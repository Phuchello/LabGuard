using System;
using System.Globalization;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LabGuard.Protocol;
using LabGuard.Protocol.Dtos;
using LabGuard.Protocol.Serialization;

namespace LabGuard.Tools.MockHarness.FakeAgent;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.Title = "FakeAgent - Mock Client";
        Console.WriteLine("==========================================");
        Console.WriteLine("       LABGUARD - FAKE AGENT CLIENT       ");
        Console.WriteLine("==========================================");

        int port = 6000;
        string host = "127.0.0.1";
        int count = 5;

        if (args.Length > 0 && int.TryParse(args[0], out int p))
        {
            port = p;
        }
        if (args.Length > 1 && !string.IsNullOrWhiteSpace(args[1]))
        {
            host = args[1];
        }
        if (args.Length > 2 && int.TryParse(args[2], out int c))
        {
            count = c;
        }

        int exitCode = await RunAsync(host, port, count, CancellationToken.None);
        return exitCode;
    }

    /// <summary>
    /// Thực hiện gửi chuỗi HEARTBEAT và xác thực các gói tin phản hồi HEARTBEAT_ACK tương ứng.
    /// Trả về 0 nếu toàn bộ quá trình thành công, khác 0 nếu gặp lỗi kết nối hoặc ACK không hợp lệ.
    /// </summary>
    public static async Task<int> RunAsync(
        string host = "127.0.0.1",
        int port = 6000,
        int count = 5,
        CancellationToken ct = default,
        string deviceId = "AGENT-DEV-01",
        TimeSpan? ackTimeout = null,
        TimeSpan? interval = null)
    {
        TimeSpan effectiveTimeout = ackTimeout ?? TimeSpan.FromSeconds(5);
        TimeSpan effectiveInterval = interval ?? TimeSpan.FromSeconds(1);

        Console.WriteLine($"[FakeAgent] Đang kết nối tới FakeCentral tại {host}:{port}...");

        using var client = new TcpClient();
        try
        {
            await client.ConnectAsync(host, port, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[FakeAgent LỖI] Không thể kết nối tới {host}:{port}: {ex.Message}");
            return 1;
        }

        Console.WriteLine("[FakeAgent] Kết nối thành công!\n");
        using var stream = client.GetStream();
        DateTime startTime = DateTime.UtcNow;

        for (int i = 1; i <= count; i++)
        {
            ct.ThrowIfCancellationRequested();

            long uptime = (long)(DateTime.UtcNow - startTime).TotalSeconds;
            var payload = new HeartbeatPayload
            {
                SequenceNumber = i,
                UptimeSeconds = Math.Max(0, uptime)
            };

            var heartbeat = WireEnvelope<HeartbeatPayload>.Create(
                messageType: MessageTypes.Heartbeat,
                deviceId: deviceId,
                payload: payload
            );

            // 1. Đóng khung 4 byte Big-Endian và gửi qua stream
            byte[] frameBytes = FrameCodec.Encode(heartbeat);
            await stream.WriteAsync(frameBytes, ct).ConfigureAwait(false);
            await stream.FlushAsync(ct).ConfigureAwait(false);
            Console.WriteLine($"[FakeAgent GỬI #{i}] Loại: {heartbeat.MessageType} | Seq: {i} | ReqId: {heartbeat.RequestId}");

            // 2. Nhận phản hồi ACK từ Server với timeout hữu hạn
            using var ackCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            ackCts.CancelAfter(effectiveTimeout);

            WireEnvelope<JsonElement>? ackEnvelope;
            try
            {
                ackEnvelope = await FrameCodec.ReadEnvelopeAsync(stream, ct: ackCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                Console.Error.WriteLine($"[FakeAgent LỖI #{i}] Timeout ({effectiveTimeout.TotalSeconds}s) chờ HEARTBEAT_ACK cho ReqId: {heartbeat.RequestId}");
                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[FakeAgent LỖI #{i}] Lỗi đọc ACK từ stream: {ex.Message}");
                return 1;
            }

            if (ackEnvelope == null)
            {
                Console.Error.WriteLine($"[FakeAgent LỖI #{i}] Server ngắt kết nối (EOF) khi đang chờ HEARTBEAT_ACK.");
                return 1;
            }

            // 3. Xác thực ACK: messageType, requestId, acknowledgedSequence, serverTimeUtc
            if (ackEnvelope.MessageType != MessageTypes.HeartbeatAck)
            {
                Console.Error.WriteLine($"[FakeAgent LỖI #{i}] Sai MessageType: nhận '{ackEnvelope.MessageType}', mong đợi '{MessageTypes.HeartbeatAck}'.");
                return 1;
            }

            if (ackEnvelope.RequestId != heartbeat.RequestId)
            {
                Console.Error.WriteLine($"[FakeAgent LỖI #{i}] Sai RequestId: nhận '{ackEnvelope.RequestId}', mong đợi '{heartbeat.RequestId}'.");
                return 1;
            }

            HeartbeatAckPayload? ackPayload;
            try
            {
                ackPayload = ProtocolSerializer.ExtractPayload<HeartbeatAckPayload>(ackEnvelope.Payload);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[FakeAgent LỖI #{i}] Lỗi deserialize HeartbeatAckPayload: {ex.Message}");
                return 1;
            }

            if (ackPayload == null)
            {
                Console.Error.WriteLine($"[FakeAgent LỖI #{i}] Payload HEARTBEAT_ACK rỗng.");
                return 1;
            }

            if (ackPayload.AcknowledgedSequence != heartbeat.Payload.SequenceNumber)
            {
                Console.Error.WriteLine($"[FakeAgent LỖI #{i}] Sai AcknowledgedSequence: nhận {ackPayload.AcknowledgedSequence}, mong đợi {heartbeat.Payload.SequenceNumber}.");
                return 1;
            }

            if (!IsValidUtcTimestamp(ackPayload.ServerTimeUtc))
            {
                Console.Error.WriteLine($"[FakeAgent LỖI #{i}] ServerTimeUtc '{ackPayload.ServerTimeUtc}' không phải mốc thời gian UTC hợp lệ ('Z' hoặc '+00:00').");
                return 1;
            }

            Console.WriteLine($"[FakeAgent NHẬN #{i}] Nhận ACK hợp lệ! AcknowledgedSeq: {ackPayload.AcknowledgedSequence} | ServerTimeUtc: {ackPayload.ServerTimeUtc}\n");

            if (i < count && effectiveInterval > TimeSpan.Zero)
            {
                await Task.Delay(effectiveInterval, ct).ConfigureAwait(false);
            }
        }

        Console.WriteLine($"[FakeAgent] Đã hoàn thành {count} lượt gửi heartbeat và xác thực ACK thành công.");
        return 0;
    }

    /// <summary>
    /// Kiểm tra chuỗi thời gian có phải định dạng ISO-8601 UTC hợp lệ với chỉ định rõ ràng ('Z' hoặc '+00:00') hay không.
    /// </summary>
    public static bool IsValidUtcTimestamp(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        string trimmed = value.Trim();
        bool hasExplicitUtc = trimmed.EndsWith('Z') || trimmed.EndsWith("+00:00", StringComparison.Ordinal);
        if (!hasExplicitUtc) return false;
        if (!DateTimeOffset.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto)) return false;
        return dto.Offset == TimeSpan.Zero;
    }
}