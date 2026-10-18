using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using LabGuard.Protocol;
using LabGuard.Protocol.Dtos;

namespace LabGuard.Agent;

internal static class AgentConsoleApplication
{
    internal const int Success = 0, InvalidConfiguration = 2, RuntimeFailure = 3, ConnectionFailure = 4, SessionUnavailable = 5, Cancelled = 130;
    private const string Help = """
        LabGuard.Agent — Console startup / kiểm tra trước tích hợp
          --init-config <file> --central-host <host> [--central-port <port>]
          --config <file> --validate-config
          --config <file> --collect-once
          --config <file> --probe
          --help
        Không chỉ định chế độ: monitoring bị chặn vì TLS/session/AUTH chưa hiện thực.
        Probe chỉ dùng trên môi trường phát triển được phép, không gửi token hoặc telemetry.
        Không tự reconnect, lưu SQL, xử lý BATCH_ACK hoặc replay trong launcher này.
        """;

    internal static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error, CancellationToken ct)
    {
        Options options;
        AgentSettings settings;
        try
        {
            options = Options.Parse(args);
            if (options.Mode == "help") { output.WriteLine(Help); return Success; }
            if (options.Mode == "init")
            {
                settings = AgentSettings.Create(options.ConfigPath, options.Host!, options.Port);
                output.WriteLine(JsonSerializer.Serialize(new { eventName = "ConfigurationCreated", settings.DeviceId, message = "Giữ cấu hình này cho đúng một máy; tạo riêng cho máy tiếp theo." }));
                return Success;
            }
            settings = AgentSettings.Load(options.ConfigPath);
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException or JsonException)
        {
            error.WriteLine(ex is ArgumentException ? $"ConfigurationError: {ex.Message}" : $"ConfigurationError: {ex.GetType().Name}; kiểm tra đường dẫn, quyền hoặc JSON. Dùng --help để xem cách tạo cấu hình.");
            return InvalidConfiguration;
        }

        AgentLog? log = null;
        try
        {
            log = new AgentLog(settings, options.ConfigPath, output);
            log.Write("ConfigurationValidated", "Cấu hình hợp lệ; chưa thiết lập phiên giám sát.", new { settings.DeviceId, settings.CentralHost, settings.CentralPort });
            ct.ThrowIfCancellationRequested();
            if (options.Mode == "validate") return Success;
            if (options.Mode == "collect")
            {
                if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
                var collector = new WindowsMetricsCollector();
                // Constructor primes the CPU delta. Allow a local sample window, never busy-wait.
                await Task.Delay(200, ct);
                var cpu = collector.ReadCpuPercent();
                var memory = collector.ReadMemoryBytes();
                log.Write("LocalMetricsCollected", "Chỉ đọc số liệu tại máy; không gửi mạng, không đánh giá sự cố.",
                    new { cpuPercent = cpu, memory.UsedPhysicalBytes, memory.TotalPhysicalBytes, uptimeSeconds = collector.ReadUptimeSeconds(), localOnly = true, sampleWindowMilliseconds = 200 });
                return Success;
            }
            if (options.Mode != "probe")
            {
                log.Write("MonitoringNotStarted", "Chưa có TLS/HELLO/AUTH và reader phiên. Central hiện có listener nhưng chưa có pipeline xác thực/lưu SQL. Không gửi heartbeat hoặc metric.", level: "Warning");
                return SessionUnavailable;
            }

            log.Write("DevelopmentTransportProbe", "Probe TCP thường, không có bảo đảm TLS/AUTH. Chỉ dùng với endpoint phát triển được phép.", level: "Warning");
            await using var client = new AgentSocketClient();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(settings.ConnectTimeoutSeconds));
            try { await client.ConnectAsync(settings.CentralHost!, settings.CentralPort, timeout.Token); }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                log.Write("ConnectionTimeout", "Hết thời gian kết nối; không tự thử lại.", level: "Error");
                return ConnectionFailure;
            }
            log.Write("TcpConnected", "TCP đã kết nối; không suy ra authenticated, telemetry received, SQL commit hoặc ACK.", new { authenticated = false, monitoringStarted = false });

            var nic = NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .OrderBy(n => n.Id, StringComparer.Ordinal).FirstOrDefault(n => n.GetPhysicalAddress().GetAddressBytes().Length > 0);
            var hello = new HelloAgentPayload
            {
                AgentVersion = typeof(AgentConsoleApplication).Assembly.GetName().Version?.ToString() ?? "unknown",
                OsVersion = Environment.OSVersion.ToString(), Hostname = Environment.MachineName,
                // Observed adapter metadata only, not proof of identity or of the route to Central.
                MacAddress = nic?.GetPhysicalAddress().ToString() ?? string.Empty
            };
            await client.SendEnvelopeAsync(WireEnvelope<HelloAgentPayload>.Create(MessageTypes.HelloAgent, settings.DeviceId!, hello), ct);
            log.Write("HelloQueued", "HELLO đã vào hàng đợi gửi; không phải HELLO_ACK hoặc xác nhận đã nhận từ Central.");
            await Task.Delay(TimeSpan.FromSeconds(settings.ProbeDurationSeconds), ct);
            if (!client.IsConnected)
            {
                log.Write("TransportWriterFailed", "Writer báo mất kết nối; probe thất bại, không tự reconnect.", level: "Error");
                return ConnectionFailure;
            }
            log.Write("ProbeFinished", "Kết thúc probe; chưa có reader phát hiện peer đóng im lặng, chưa AUTH/SQL/ACK/reconnect.", new { queuedFrames = client.QueuedCount, writerReportsConnected = client.IsConnected });
            // Existing client cancellation does not promise delivery/draining of queued frames.
            await client.DisconnectAsync();
            log.Write("TransportClosed", "Đã đóng TCP probe; không khởi chạy monitoring.");
            return Success;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            log?.Write("Cancelled", "Đã yêu cầu dừng; transport được giải phóng, không xác nhận lưu dữ liệu.");
            return Cancelled;
        }
        catch (SocketException ex)
        {
            log?.Write("ConnectionFailed", "Lỗi TCP; không tự reconnect.", new { socketError = ex.SocketErrorCode.ToString() }, "Error");
            return ConnectionFailure;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or PlatformNotSupportedException or System.ComponentModel.Win32Exception)
        {
            error.WriteLine($"RuntimeError: {ex.GetType().Name}; chưa xác minh monitoring hoặc lưu SQL.");
            return RuntimeFailure;
        }
        finally { log?.Dispose(); }
    }

    private sealed record Options(string Mode, string ConfigPath, string? Host, int Port)
    {
        internal static Options Parse(string[] args)
        {
            string? mode = null, path = null, host = null; var port = 5000; var portSet = false;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < args.Length; i++)
            {
                var option = args[i];
                if (!seen.Add(option)) throw new ArgumentException("Tham số lệnh bị lặp.");
                string Value() => ++i < args.Length && !args[i].StartsWith("--", StringComparison.Ordinal) ? args[i] : throw new ArgumentException("Thiếu giá trị tham số.");
                void Mode(string value) { if (mode is not null) throw new ArgumentException("Chỉ chọn một chế độ chạy."); mode = value; }
                switch (option)
                {
                    case "--help": case "-h": Mode("help"); break;
                    case "--init-config": if (path is not null) throw new ArgumentException("Chỉ dùng một đường dẫn cấu hình."); Mode("init"); path = Value(); break;
                    case "--config": if (path is not null) throw new ArgumentException("Chỉ dùng một đường dẫn cấu hình."); path = Value(); break;
                    case "--central-host": host = Value(); break;
                    case "--central-port": if (!int.TryParse(Value(), out port)) throw new ArgumentException("Cổng phải là số nguyên."); portSet = true; break;
                    case "--validate-config": Mode("validate"); break;
                    case "--collect-once": Mode("collect"); break;
                    case "--probe": Mode("probe"); break;
                    default: throw new ArgumentException("Tham số không được hỗ trợ; dùng --help.");
                }
            }
            if (mode == "init" && host is null) throw new ArgumentException("Tạo cấu hình cần --central-host.");
            if (mode != "init" && (host is not null || portSet)) throw new ArgumentException("Đổi endpoint trong file cấu hình, không ghi đè lúc chạy.");
            return new Options(mode ?? "monitor", path ?? Path.Combine(AppContext.BaseDirectory, "agentsettings.json"), host, port);
        }
    }
}
