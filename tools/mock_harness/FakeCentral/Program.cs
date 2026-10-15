using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LabGuard.Protocol;
using LabGuard.Protocol.Dtos;

namespace LabGuard.Tools.MockHarness.FakeCentral;

public class Program
{
    private static readonly CancellationTokenSource _cts = new();

    public static async Task<int> Main(string[] args)
    {
        Console.Title = "FakeCentral - Mock Server";
        Console.WriteLine("==========================================");
        Console.WriteLine("       LABGUARD - FAKE CENTRAL SERVER     ");
        Console.WriteLine("==========================================");

        int port = 6000;
        if (args.Length > 0 && int.TryParse(args[0], out int customPort))
        {
            port = customPort;
        }

        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("\n[FakeCentral] Đang dừng máy chủ...");
            _cts.Cancel();
        };

        return await RunServerAsync(port, _cts.Token);
    }

    /// <summary>
    /// Khởi chạy FakeCentral lắng nghe tại cổng chỉ định.
    /// Cho phép truyền TaskCompletionSource để nhận thông báo cổng thực tế khi dùng dynamic port (0).
    /// </summary>
    public static async Task<int> RunServerAsync(
        int port = 6000,
        CancellationToken ct = default,
        TaskCompletionSource<int>? startedTcs = null)
    {
        TcpListener listener = new(IPAddress.Loopback, port);

        try
        {
            listener.Start();
        }
        catch (SocketException ex)
        {
            Console.Error.WriteLine($"[FakeCentral] [LỖI KHỞI ĐỘNG] Không thể lắng nghe tại cổng {port}: {ex.Message} ({ex.SocketErrorCode})");
            startedTcs?.TrySetException(ex);
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[FakeCentral] [LỖI KHỞI ĐỘNG] Khởi động thất bại: {ex.Message}");
            startedTcs?.TrySetException(ex);
            return 1;
        }

        int actualPort = ((IPEndPoint)listener.LocalEndpoint).Port;
        startedTcs?.TrySetResult(actualPort);

        Console.WriteLine($"[FakeCentral] Đang lắng nghe tại 127.0.0.1:{actualPort}...");
        Console.WriteLine("[FakeCentral] Nhấn Ctrl+C để dừng.");

        ConcurrentDictionary<Task, byte> activeClientTasks = new();

        try
        {
            while (!ct.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    Console.Error.WriteLine($"[FakeCentral] Lỗi tiếp nhận client: {ex.Message}");
                    continue;
                }

                try
                {
                    Task clientTask = Task.Run(() => HandleClientAsync(client, ct), CancellationToken.None);
                    activeClientTasks.TryAdd(clientTask, 0);
                    _ = clientTask.ContinueWith(
                        t => activeClientTasks.TryRemove(t, out _),
                        CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                }
                catch
                {
                    try { client.Close(); } catch { }
                    throw;
                }
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[FakeCentral] Nhận tín hiệu dừng máy chủ.");
        }
        finally
        {
            try
            {
                listener.Stop();
            }
            catch { }

            if (!activeClientTasks.IsEmpty)
            {
                try
                {
                    await Task.WhenAny(Task.WhenAll(activeClientTasks.Keys), Task.Delay(2000)).ConfigureAwait(false);
                }
                catch { }
            }

            Console.WriteLine("[FakeCentral] Máy chủ đã đóng sạch sẽ.");
        }

        return 0;
    }

    /// <summary>
    /// Xử lý một kết nối client: đọc các gói tin qua FrameCodec, validate phong bì,
    /// phản hồi HEARTBEAT_ACK cho HEARTBEAT hợp lệ và bỏ qua/ghi log các bản tin khác.
    /// </summary>
    public static async Task HandleClientAsync(TcpClient client, CancellationToken ct = default)
    {
        EndPoint? remoteEp = null;
        try
        {
            remoteEp = client.Client.RemoteEndPoint;
            Console.WriteLine($"\n[FakeCentral] [+] Kết nối mới từ: {remoteEp}");

            using NetworkStream stream = client.GetStream();

            while (!ct.IsCancellationRequested)
            {
                WireEnvelope<JsonElement>? envelope;
                try
                {
                    // 1. Đọc và parse/validate phong bì qua FrameCodec
                    envelope = await FrameCodec.ReadEnvelopeAsync(stream, ct: ct).ConfigureAwait(false);
                }
                catch (EndOfStreamException)
                {
                    Console.WriteLine($"[FakeCentral] [-] Client {remoteEp} đã đóng kết nối an toàn.");
                    break;
                }
                catch (InvalidDataException ex)
                {
                    Console.Error.WriteLine($"[FakeCentral] [!] Gói tin không hợp lệ từ {remoteEp}: {ex.Message}");
                    break;
                }
                catch (IOException ex) when (ex.InnerException is SocketException se &&
                                            (se.SocketErrorCode == SocketError.ConnectionReset ||
                                             se.SocketErrorCode == SocketError.ConnectionAborted))
                {
                    Console.WriteLine($"[FakeCentral] [-] Client {remoteEp} đã ngắt kết nối đột ngột: {se.SocketErrorCode}");
                    break;
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"[FakeCentral] [-] Lỗi I/O từ {remoteEp}: {ex.Message}");
                    break;
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"[FakeCentral] [-] Lỗi Socket từ {remoteEp}: {ex.Message}");
                    break;
                }

                if (envelope == null)
                {
                    Console.WriteLine($"[FakeCentral] [-] Client {remoteEp} đã ngắt kết nối bình thường (EOF).");
                    break;
                }

                Console.WriteLine($"[FakeCentral NHẬN] Loại: {envelope.MessageType,-15} | Device: {envelope.DeviceId} | ReqId: {envelope.RequestId}");

                // 2. Chỉ phản hồi ACK đối với HEARTBEAT hợp lệ
                if (envelope.MessageType == MessageTypes.Heartbeat)
                {
                    // Xác thực rõ ràng sequenceNumber và uptimeSeconds tồn tại và có giá trị số hợp lệ trước khi deserialize
                    bool hasSeq = envelope.Payload.TryGetProperty("sequenceNumber", out var seqProp) ||
                                  envelope.Payload.TryGetProperty("SequenceNumber", out seqProp);
                    bool hasUptime = envelope.Payload.TryGetProperty("uptimeSeconds", out var uptimeProp) ||
                                     envelope.Payload.TryGetProperty("UptimeSeconds", out uptimeProp);

                    if (!hasSeq || seqProp.ValueKind != JsonValueKind.Number || !seqProp.TryGetInt64(out long sequenceNumber) || sequenceNumber < 0)
                    {
                        Console.Error.WriteLine($"[FakeCentral] [LỖI PAYLOAD] HEARTBEAT từ {envelope.DeviceId} thiếu hoặc sequenceNumber không hợp lệ ({seqProp}). Không gửi ACK.");
                        continue;
                    }

                    if (!hasUptime || uptimeProp.ValueKind != JsonValueKind.Number || !uptimeProp.TryGetInt64(out long uptimeSeconds) || uptimeSeconds < 0)
                    {
                        Console.Error.WriteLine($"[FakeCentral] [LỖI PAYLOAD] HEARTBEAT từ {envelope.DeviceId} thiếu hoặc uptimeSeconds không hợp lệ ({uptimeProp}). Không gửi ACK.");
                        continue;
                    }

                    // 3. Tạo bản tin phản hồi HeartbeatAckPayload với serverTimeUtc hiện tại và acknowledgedSequence
                    var ackPayload = new HeartbeatAckPayload
                    {
                        AcknowledgedSequence = sequenceNumber,
                        ServerTimeUtc = DateTime.UtcNow.ToString("O")
                    };

                    var ackEnvelope = WireEnvelope<HeartbeatAckPayload>.Create(
                        messageType: MessageTypes.HeartbeatAck,
                        deviceId: "CENTRAL-MOCK",
                        payload: ackPayload,
                        requestId: envelope.RequestId // Bảo toàn requestId để correlation
                    );

                    // 4. Đóng khung qua FrameCodec và gửi lại cho client
                    byte[] ackBytes = FrameCodec.Encode(ackEnvelope);
                    await stream.WriteAsync(ackBytes, ct).ConfigureAwait(false);
                    await stream.FlushAsync(ct).ConfigureAwait(false);
                    Console.WriteLine($"[FakeCentral GỬI] -> Đã gửi HEARTBEAT_ACK cho ReqId: {envelope.RequestId} (AckSeq: {sequenceNumber})");
                }
                else
                {
                    // Đối với các loại bản tin không hỗ trợ trong Mock, ghi log và TUYỆT ĐỐI không gửi ACK thành công giả mạo
                    Console.WriteLine($"[FakeCentral] Bản tin không được hỗ trợ trong Mock: '{envelope.MessageType}' từ {envelope.DeviceId}. Bỏ qua, không gửi ACK.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Server shutting down
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[FakeCentral] Lỗi xử lý client {remoteEp}: {ex.Message}");
        }
        finally
        {
            try
            {
                client.Close();
            }
            catch { }
            Console.WriteLine($"[FakeCentral] [-] Đã giải phóng kết nối: {remoteEp}");
        }
    }
}