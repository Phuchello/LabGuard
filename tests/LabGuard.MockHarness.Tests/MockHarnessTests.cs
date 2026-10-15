using System;
using System.Buffers.Binary;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LabGuard.Protocol;
using LabGuard.Protocol.Dtos;
using LabGuard.Protocol.Serialization;
using Xunit;

namespace LabGuard.MockHarness.Tests;

/// <summary>
/// Bộ kiểm thử tự động toàn diện cho công cụ Mock Harness (FakeCentral và FakeAgent)
/// theo yêu cầu kỹ thuật LG-D (NT106 - Lập trình mạng).
/// </summary>
public class MockHarnessTests
{
    private static async Task<(int Port, Task<int> ServerTask, CancellationTokenSource Cts)> StartFakeCentralAsync()
    {
        var cts = new CancellationTokenSource();
        var tcs = new TaskCompletionSource<int>();
        var serverTask = Task.Run(() => Tools.MockHarness.FakeCentral.Program.RunServerAsync(0, cts.Token, tcs));
        int port = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        return (port, serverTask, cts);
    }

    // -------------------------------------------------------------------------
    // SCENARIO 1: Valid HEARTBEAT -> correct HEARTBEAT_ACK
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Scenario1_ValidHeartbeat_ReceivesCorrectHeartbeatAck()
    {
        var (port, serverTask, cts) = await StartFakeCentralAsync();

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync("127.0.0.1", port);
            using var stream = client.GetStream();

            var reqId = Guid.NewGuid().ToString("D");
            var heartbeat = WireEnvelope<HeartbeatPayload>.Create(
                messageType: MessageTypes.Heartbeat,
                deviceId: "TEST-DEV-01",
                payload: new HeartbeatPayload { SequenceNumber = 42, UptimeSeconds = 1234 },
                requestId: reqId
            );

            byte[] frame = FrameCodec.Encode(heartbeat);
            await stream.WriteAsync(frame);
            await stream.FlushAsync();

            using var ackCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var ack = await FrameCodec.ReadEnvelopeAsync(stream, ct: ackCts.Token);

            Assert.NotNull(ack);
            Assert.Equal(MessageTypes.HeartbeatAck, ack.MessageType);
            Assert.Equal(reqId, ack.RequestId);
            Assert.Equal("CENTRAL-MOCK", ack.DeviceId);

            var ackPayload = ProtocolSerializer.ExtractPayload<HeartbeatAckPayload>(ack.Payload);
            Assert.NotNull(ackPayload);
            Assert.Equal(42, ackPayload.AcknowledgedSequence);
            Assert.True(Tools.MockHarness.FakeAgent.Program.IsValidUtcTimestamp(ackPayload.ServerTimeUtc));
        }
        finally
        {
            cts.Cancel();
            await serverTask;
        }
    }

    // -------------------------------------------------------------------------
    // SCENARIO 2: Five sequential heartbeat requests -> five matching acknowledgments
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Scenario2_FiveSequentialHeartbeats_AllAcknowledgedByFakeAgent()
    {
        var (port, serverTask, cts) = await StartFakeCentralAsync();

        try
        {
            int exitCode = await Tools.MockHarness.FakeAgent.Program.RunAsync(
                host: "127.0.0.1",
                port: port,
                count: 5,
                interval: TimeSpan.FromMilliseconds(10),
                ackTimeout: TimeSpan.FromSeconds(3)
            );

            Assert.Equal(0, exitCode);
        }
        finally
        {
            cts.Cancel();
            await serverTask;
        }
    }

    // -------------------------------------------------------------------------
    // SCENARIO 3: Two simultaneous TCP clients
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Scenario3_TwoSimultaneousClients_HandledConcurrently()
    {
        var (port, serverTask, cts) = await StartFakeCentralAsync();

        try
        {
            using var clientA = new TcpClient();
            await clientA.ConnectAsync("127.0.0.1", port);
            var streamA = clientA.GetStream();

            // Client A gửi Heartbeat 1 và giữ kết nối mở
            var reqA1 = Guid.NewGuid().ToString("D");
            var hbA1 = WireEnvelope<HeartbeatPayload>.Create(
                MessageTypes.Heartbeat, "CLIENT-A", new HeartbeatPayload { SequenceNumber = 1, UptimeSeconds = 10 }, reqA1);
            await streamA.WriteAsync(FrameCodec.Encode(hbA1));
            await streamA.FlushAsync();
            var ackA1 = await FrameCodec.ReadEnvelopeAsync(streamA);
            Assert.NotNull(ackA1);
            Assert.Equal(reqA1, ackA1.RequestId);

            // Client B kết nối và giao tiếp đồng thời trong khi Client A vẫn đang duy trì socket
            using var clientB = new TcpClient();
            await clientB.ConnectAsync("127.0.0.1", port);
            var streamB = clientB.GetStream();

            var reqB1 = Guid.NewGuid().ToString("D");
            var hbB1 = WireEnvelope<HeartbeatPayload>.Create(
                MessageTypes.Heartbeat, "CLIENT-B", new HeartbeatPayload { SequenceNumber = 99, UptimeSeconds = 50 }, reqB1);
            await streamB.WriteAsync(FrameCodec.Encode(hbB1));
            await streamB.FlushAsync();
            var ackB1 = await FrameCodec.ReadEnvelopeAsync(streamB);
            Assert.NotNull(ackB1);
            Assert.Equal(reqB1, ackB1.RequestId);
            clientB.Close();

            // Client A tiếp tục gửi Heartbeat 2
            var reqA2 = Guid.NewGuid().ToString("D");
            var hbA2 = WireEnvelope<HeartbeatPayload>.Create(
                MessageTypes.Heartbeat, "CLIENT-A", new HeartbeatPayload { SequenceNumber = 2, UptimeSeconds = 15 }, reqA2);
            await streamA.WriteAsync(FrameCodec.Encode(hbA2));
            await streamA.FlushAsync();
            var ackA2 = await FrameCodec.ReadEnvelopeAsync(streamA);
            Assert.NotNull(ackA2);
            Assert.Equal(reqA2, ackA2.RequestId);
        }
        finally
        {
            cts.Cancel();
            await serverTask;
        }
    }

    // -------------------------------------------------------------------------
    // SCENARIO 4: Multiple length-prefixed frames sent consecutively on one connection
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Scenario4_MultipleFramesCoalescedOnStream_AllParsedAndAcked()
    {
        var (port, serverTask, cts) = await StartFakeCentralAsync();

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync("127.0.0.1", port);
            var stream = client.GetStream();

            var req1 = Guid.NewGuid().ToString("D");
            var req2 = Guid.NewGuid().ToString("D");
            var req3 = Guid.NewGuid().ToString("D");

            byte[] f1 = FrameCodec.Encode(WireEnvelope<HeartbeatPayload>.Create(MessageTypes.Heartbeat, "DEV-1", new HeartbeatPayload { SequenceNumber = 1, UptimeSeconds = 1 }, req1));
            byte[] f2 = FrameCodec.Encode(WireEnvelope<HeartbeatPayload>.Create(MessageTypes.Heartbeat, "DEV-1", new HeartbeatPayload { SequenceNumber = 2, UptimeSeconds = 2 }, req2));
            byte[] f3 = FrameCodec.Encode(WireEnvelope<HeartbeatPayload>.Create(MessageTypes.Heartbeat, "DEV-1", new HeartbeatPayload { SequenceNumber = 3, UptimeSeconds = 3 }, req3));

            // Ghép 3 frames liên tiếp vào 1 mảng byte duy nhất (mô phỏng TCP stream coalescing)
            byte[] coalesced = new byte[f1.Length + f2.Length + f3.Length];
            Buffer.BlockCopy(f1, 0, coalesced, 0, f1.Length);
            Buffer.BlockCopy(f2, 0, coalesced, f1.Length, f2.Length);
            Buffer.BlockCopy(f3, 0, coalesced, f1.Length + f2.Length, f3.Length);

            await stream.WriteAsync(coalesced);
            await stream.FlushAsync();

            // Đọc 3 ACK phản hồi tuần tự
            var ack1 = await FrameCodec.ReadEnvelopeAsync(stream);
            var ack2 = await FrameCodec.ReadEnvelopeAsync(stream);
            var ack3 = await FrameCodec.ReadEnvelopeAsync(stream);

            Assert.NotNull(ack1);
            Assert.NotNull(ack2);
            Assert.NotNull(ack3);

            Assert.Equal(req1, ack1.RequestId);
            Assert.Equal(req2, ack2.RequestId);
            Assert.Equal(req3, ack3.RequestId);
        }
        finally
        {
            cts.Cancel();
            await serverTask;
        }
    }

    // -------------------------------------------------------------------------
    // SCENARIO 5: Fragmented TCP header and payload
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Scenario5_FragmentedHeaderAndPayload_ReconstructedCorrectly()
    {
        var (port, serverTask, cts) = await StartFakeCentralAsync();

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync("127.0.0.1", port);
            var stream = client.GetStream();

            var reqId = Guid.NewGuid().ToString("D");
            var hb = WireEnvelope<HeartbeatPayload>.Create(
                MessageTypes.Heartbeat, "DEV-FRAG", new HeartbeatPayload { SequenceNumber = 77, UptimeSeconds = 999 }, reqId);
            byte[] frame = FrameCodec.Encode(hb);

            // Phân mảnh: gửi 1 byte header -> delay -> 3 byte header còn lại -> delay -> 10 byte payload -> payload còn lại
            await stream.WriteAsync(frame.AsMemory(0, 1));
            await stream.FlushAsync();
            await Task.Delay(20);

            await stream.WriteAsync(frame.AsMemory(1, 3));
            await stream.FlushAsync();
            await Task.Delay(20);

            await stream.WriteAsync(frame.AsMemory(4, 10));
            await stream.FlushAsync();
            await Task.Delay(20);

            await stream.WriteAsync(frame.AsMemory(14));
            await stream.FlushAsync();

            var ack = await FrameCodec.ReadEnvelopeAsync(stream);
            Assert.NotNull(ack);
            Assert.Equal(reqId, ack.RequestId);

            var payload = ProtocolSerializer.ExtractPayload<HeartbeatAckPayload>(ack.Payload);
            Assert.NotNull(payload);
            Assert.Equal(77, payload.AcknowledgedSequence);
        }
        finally
        {
            cts.Cancel();
            await serverTask;
        }
    }

    // -------------------------------------------------------------------------
    // SCENARIO 6: Invalid heartbeat payload -> no false success ACK
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Scenario6_InvalidPayloadOrUnsupportedMessage_NoFalseSuccessAck()
    {
        var (port, serverTask, cts) = await StartFakeCentralAsync();

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync("127.0.0.1", port);
            var stream = client.GetStream();

            // 6a. Gửi HEARTBEAT nhưng payload thiếu sequenceNumber (payload cũ lỗi thời: {"status": "ACTIVE"})
            using var doc1 = JsonDocument.Parse("{\"status\": \"ACTIVE\"}");
            var badHb1 = WireEnvelope<JsonElement>.Create(MessageTypes.Heartbeat, "DEV-BAD", doc1.RootElement.Clone());
            await stream.WriteAsync(FrameCodec.Encode(badHb1));
            await stream.FlushAsync();

            // 6b. Gửi HEARTBEAT với sequenceNumber dạng chuỗi thay vì số
            using var doc2 = JsonDocument.Parse("{\"sequenceNumber\": \"not-a-number\", \"uptimeSeconds\": 10}");
            var badHb2 = WireEnvelope<JsonElement>.Create(MessageTypes.Heartbeat, "DEV-BAD", doc2.RootElement.Clone());
            await stream.WriteAsync(FrameCodec.Encode(badHb2));
            await stream.FlushAsync();

            // 6c. Gửi bản tin khác không hỗ trợ trong Mock (ví dụ: PING)
            using var doc3 = JsonDocument.Parse("{\"text\": \"ping-test\"}");
            var pingMsg = WireEnvelope<JsonElement>.Create(MessageTypes.Ping, "DEV-BAD", doc3.RootElement.Clone());
            await stream.WriteAsync(FrameCodec.Encode(pingMsg));
            await stream.FlushAsync();

            // Xác nhận rằng KHÔNG có ACK thành công giả mạo nào được gửi về
            using var readCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            byte[] buf = new byte[1024];
            int read = 0;
            try
            {
                read = await stream.ReadAsync(buf, readCts.Token);
            }
            catch (OperationCanceledException)
            {
                read = 0; // Mong đợi timeout: không có ACK nào được gửi
            }

            Assert.Equal(0, read);
        }
        finally
        {
            cts.Cancel();
            await serverTask;
        }
    }

    // -------------------------------------------------------------------------
    // SCENARIO 7: Wrong ACK requestId / acknowledgedSequence detected by FakeAgent
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Scenario7_MismatchedAckOrInvalidUtc_DetectedAndRejectedByFakeAgent()
    {
        // 7a. Mismatched RequestId
        var listenerA = new TcpListener(IPAddress.Loopback, 0);
        listenerA.Start();
        int portA = ((IPEndPoint)listenerA.LocalEndpoint).Port;

        _ = Task.Run(async () =>
        {
            using var c = await listenerA.AcceptTcpClientAsync();
            using var s = c.GetStream();
            var env = await FrameCodec.ReadEnvelopeAsync(s);
            if (env != null)
            {
                // Trả về RequestId SAI (không khớp với request)
                var wrongAck = WireEnvelope<HeartbeatAckPayload>.Create(
                    MessageTypes.HeartbeatAck, "CENTRAL-MOCK",
                    new HeartbeatAckPayload { AcknowledgedSequence = 1, ServerTimeUtc = DateTime.UtcNow.ToString("O") },
                    requestId: Guid.NewGuid().ToString("D") // Khác với env.RequestId!
                );
                await s.WriteAsync(FrameCodec.Encode(wrongAck));
                await s.FlushAsync();
            }
            listenerA.Stop();
        });

        int exitA = await Tools.MockHarness.FakeAgent.Program.RunAsync(port: portA, count: 1, ackTimeout: TimeSpan.FromSeconds(2));
        Assert.Equal(1, exitA); // Bị từ chối!

        // 7b. Mismatched AcknowledgedSequence
        var listenerB = new TcpListener(IPAddress.Loopback, 0);
        listenerB.Start();
        int portB = ((IPEndPoint)listenerB.LocalEndpoint).Port;

        _ = Task.Run(async () =>
        {
            using var c = await listenerB.AcceptTcpClientAsync();
            using var s = c.GetStream();
            var env = await FrameCodec.ReadEnvelopeAsync(s);
            if (env != null)
            {
                // Trả về sequenceNumber SAI (client gửi seq 1, server trả 999)
                var wrongAck = WireEnvelope<HeartbeatAckPayload>.Create(
                    MessageTypes.HeartbeatAck, "CENTRAL-MOCK",
                    new HeartbeatAckPayload { AcknowledgedSequence = 999, ServerTimeUtc = DateTime.UtcNow.ToString("O") },
                    requestId: env.RequestId
                );
                await s.WriteAsync(FrameCodec.Encode(wrongAck));
                await s.FlushAsync();
            }
            listenerB.Stop();
        });

        int exitB = await Tools.MockHarness.FakeAgent.Program.RunAsync(port: portB, count: 1, ackTimeout: TimeSpan.FromSeconds(2));
        Assert.Equal(1, exitB); // Bị từ chối!

        // 7c. Invalid ServerTimeUtc (không có chỉ chỉ múi giờ 'Z' hoặc '+00:00')
        var listenerC = new TcpListener(IPAddress.Loopback, 0);
        listenerC.Start();
        int portC = ((IPEndPoint)listenerC.LocalEndpoint).Port;

        _ = Task.Run(async () =>
        {
            using var c = await listenerC.AcceptTcpClientAsync();
            using var s = c.GetStream();
            var env = await FrameCodec.ReadEnvelopeAsync(s);
            if (env != null)
            {
                var wrongAck = WireEnvelope<HeartbeatAckPayload>.Create(
                    MessageTypes.HeartbeatAck, "CENTRAL-MOCK",
                    new HeartbeatAckPayload { AcknowledgedSequence = 1, ServerTimeUtc = "2026-10-15 12:00:00" }, // Không phải ISO UTC!
                    requestId: env.RequestId
                );
                await s.WriteAsync(FrameCodec.Encode(wrongAck));
                await s.FlushAsync();
            }
            listenerC.Stop();
        });

        int exitC = await Tools.MockHarness.FakeAgent.Program.RunAsync(port: portC, count: 1, ackTimeout: TimeSpan.FromSeconds(2));
        Assert.Equal(1, exitC); // Bị từ chối!
    }

    // -------------------------------------------------------------------------
    // SCENARIO 8: Normal and abrupt disconnection
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Scenario8_NormalAndAbruptDisconnection_HandledSafely()
    {
        var (port, serverTask, cts) = await StartFakeCentralAsync();

        try
        {
            // 8a. Normal Disconnect (Shutdown Both / EOF)
            using (var clientNormal = new TcpClient())
            {
                await clientNormal.ConnectAsync("127.0.0.1", port);
                clientNormal.Client.Shutdown(SocketShutdown.Both);
                clientNormal.Close();
            }

            // 8b. Abrupt Disconnect (TCP RST via LingerOption(true, 0))
            using (var clientRst = new TcpClient())
            {
                await clientRst.ConnectAsync("127.0.0.1", port);
                clientRst.Client.LingerState = new LingerOption(true, 0);
                clientRst.Close();
            }

            // 8c. Server remains healthy and accepts subsequent connection
            using var clientAlive = new TcpClient();
            await clientAlive.ConnectAsync("127.0.0.1", port);
            var stream = clientAlive.GetStream();

            var req = Guid.NewGuid().ToString("D");
            var hb = WireEnvelope<HeartbeatPayload>.Create(
                MessageTypes.Heartbeat, "DEV-ALIVE", new HeartbeatPayload { SequenceNumber = 100, UptimeSeconds = 500 }, req);
            await stream.WriteAsync(FrameCodec.Encode(hb));
            await stream.FlushAsync();

            var ack = await FrameCodec.ReadEnvelopeAsync(stream);
            Assert.NotNull(ack);
            Assert.Equal(req, ack.RequestId);
        }
        finally
        {
            cts.Cancel();
            await serverTask;
        }
    }

    // -------------------------------------------------------------------------
    // SCENARIO 9: Clean tool shutdown
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Scenario9_ToolShutdown_StopsCleanlyAndReleasesPort()
    {
        var (port, serverTask, cts) = await StartFakeCentralAsync();

        // Server đang chạy
        using (var probe = new TcpClient())
        {
            await probe.ConnectAsync("127.0.0.1", port);
            Assert.True(probe.Connected);
        }

        // Gửi tín hiệu dừng
        cts.Cancel();
        int exitCode = await serverTask.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal(0, exitCode);

        // Xác nhận cổng đã được giải phóng (thử bind lại cổng đó thành công)
        var verifyListener = new TcpListener(IPAddress.Loopback, port);
        verifyListener.Start();
        verifyListener.Stop();
    }
}
