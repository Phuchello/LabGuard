using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using LabGuard.Protocol;
using LabGuard.Protocol.Dtos;
using Xunit;

namespace LabGuard.Agent.Tests;

/// <summary>
/// Bộ kiểm thử tự động cho nhiệm vụ A-003:
/// Agent TCP Connection &amp; Bounded Single-Writer Queue.
/// Kiểm chứng toàn bộ kịch bản quy định trong WBS và tài liệu kiến trúc CP2-001.
/// </summary>
public class AgentSocketClientTests
{
    // =========================================================================
    // SCENARIO 1: Kết nối thành công tới Loopback Fake Server
    // =========================================================================
    [Fact]
    public async Task Scenario1_Agent_CanConnect_ToLoopbackFakeServer()
    {
        await using var server = new FakeTcpServer();
        using var client = new AgentSocketClient();

        Assert.False(client.IsConnected);

        var connectTask = client.ConnectAsync(server.Host, server.Port);
        var acceptTask = server.AcceptClientAsync();

        await Task.WhenAll(connectTask, acceptTask);

        Assert.True(client.IsConnected);
    }

    // =========================================================================
    // SCENARIO 2: Gửi 1 frame vào hàng đợi và server nhận trọn vẹn
    // =========================================================================
    [Fact]
    public async Task Scenario2_OneQueuedFrame_ReachesServerCorrectly()
    {
        await using var server = new FakeTcpServer();
        using var client = new AgentSocketClient();

        var connectTask = client.ConnectAsync(server.Host, server.Port);
        var serverClientTask = server.AcceptClientAsync();
        await Task.WhenAll(connectTask, serverClientTask);

        using var serverClient = await serverClientTask;

        // Tạo frame mẫu
        byte[] payload = Encoding.UTF8.GetBytes("{\"test\":\"single_queued_frame\"}");
        byte[] framed = FrameCodec.Encode(payload);

        // Gửi frame qua hàng đợi
        await client.EnqueueFrameAsync(framed);

        // Server đọc frame bằng FrameCodec (A-002)
        var received = await server.ReadFramesAsync(serverClient, 1);

        Assert.Single(received);
        Assert.Equal(payload, received[0]);
    }

    // =========================================================================
    // SCENARIO 3: Gửi nhiều frame tuần tự và nhận đúng thứ tự FIFO tuyệt đối
    // =========================================================================
    [Fact]
    public async Task Scenario3_MultipleSequentialFrames_ArriveInExactOrder()
    {
        await using var server = new FakeTcpServer();
        using var client = new AgentSocketClient();

        var connectTask = client.ConnectAsync(server.Host, server.Port);
        var serverClientTask = server.AcceptClientAsync();
        await Task.WhenAll(connectTask, serverClientTask);

        using var serverClient = await serverClientTask;

        const int frameCount = 10;
        var sentPayloads = new List<byte[]>();

        for (int i = 0; i < frameCount; i++)
        {
            var envelope = new WireEnvelope<HeartbeatPayload>
            {
                Version = 1,
                MessageType = MessageTypes.Heartbeat,
                RequestId = Guid.NewGuid().ToString(),
                DeviceId = "PC-TEST-01",
                SentAtUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Payload = new HeartbeatPayload
                {
                    SequenceNumber = i,
                    UptimeSeconds = 100 + i
                }
            };

            sentPayloads.Add(Protocol.Serialization.ProtocolSerializer.Serialize(envelope));
            await client.SendEnvelopeAsync(envelope);
        }

        // Đọc toàn bộ 10 frames tại server
        var receivedFrames = await server.ReadFramesAsync(serverClient, frameCount);

        Assert.Equal(frameCount, receivedFrames.Count);
        for (int i = 0; i < frameCount; i++)
        {
            // Kiểm tra thứ tự và nội dung từng frame
            Assert.Equal(sentPayloads[i], receivedFrames[i]);
        }
    }

    // =========================================================================
    // SCENARIO 4: Nhiều producer Task đẩy đồng thời, byte không bị trộn lẫn
    // =========================================================================
    [Fact]
    public async Task Scenario4_MultipleProducerTasks_EnqueueConcurrently_FrameBytesNotCorrupted()
    {
        await using var server = new FakeTcpServer();
        // Dùng constructor production (không tham số) để xác nhận capacity = 256
        using var client = new AgentSocketClient();

        var connectTask = client.ConnectAsync(server.Host, server.Port);
        var serverClientTask = server.AcceptClientAsync();
        await Task.WhenAll(connectTask, serverClientTask);

        using var serverClient = await serverClientTask;

        const int producersCount = 10;
        const int framesPerProducer = 20;
        const int totalFrames = producersCount * framesPerProducer;

        // Bắt đầu 10 producer tasks đẩy đồng thời (mô phỏng Heartbeat, Metrics, Service)
        var producerTasks = new List<Task>();
        for (int p = 0; p < producersCount; p++)
        {
            int producerId = p;
            producerTasks.Add(Task.Run(async () =>
            {
                for (int f = 0; f < framesPerProducer; f++)
                {
                    var envelope = new WireEnvelope<JsonElement>
                    {
                        Version = 1,
                        MessageType = MessageTypes.Heartbeat,
                        RequestId = Guid.NewGuid().ToString(),
                        DeviceId = $"PC-PRODUCER-{producerId}",
                        SentAtUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        Payload = JsonSerializer.Deserialize<JsonElement>($"{{\"producer\":{producerId},\"seq\":{f}}}")
                    };

                    await client.SendEnvelopeAsync(envelope);
                }
            }));
        }

        // Đọc 200 frames từ server song song khi các producer đang đẩy
        var readServerTask = server.ReadFramesAsync(serverClient, totalFrames);

        await Task.WhenAll(producerTasks);
        var received = await readServerTask;

        Assert.Equal(totalFrames, received.Count);

        // Xác minh 100% frame nhận được đều giải mã JSON hợp lệ (không bị xé khung / đan xen byte)
        foreach (var frameBytes in received)
        {
            var validation = EnvelopeValidator.ValidateRawJson(frameBytes);
            Assert.True(validation.IsValid, $"Frame bị hỏng hoặc trộn byte: {validation.ErrorMessage}");
        }
    }

    // =========================================================================
    // SCENARIO 5: Chỉ có đúng một luồng ghi duy nhất (Single Writer Path)
    // =========================================================================
    [Fact]
    public async Task Scenario5_SingleWriterModel_AllFramesDeliveredThroughQueue()
    {
        await using var server = new FakeTcpServer();
        using var client = new AgentSocketClient();

        var connectTask = client.ConnectAsync(server.Host, server.Port);
        var serverClientTask = server.AcceptClientAsync();
        await Task.WhenAll(connectTask, serverClientTask);

        using var serverClient = await serverClientTask;

        // Gửi 50 frames siêu nhanh từ nhiều luồng để tạo áp lực ghi
        var sendTasks = Enumerable.Range(0, 50).Select(i =>
        {
            var envelope = new WireEnvelope<JsonElement>
            {
                Version = 1,
                MessageType = MessageTypes.Heartbeat,
                RequestId = Guid.NewGuid().ToString(),
                DeviceId = "PC-SINGLE-WRITER",
                SentAtUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Payload = JsonSerializer.Deserialize<JsonElement>($"{{\"i\":{i}}}")
            };
            return client.SendEnvelopeAsync(envelope).AsTask();
        }).ToArray();

        await Task.WhenAll(sendTasks);

        var received = await server.ReadFramesAsync(serverClient, 50);
        Assert.Equal(50, received.Count);

        // Toàn bộ frames đều có header 4 byte Big-Endian và envelope hợp lệ
        foreach (var frame in received)
        {
            var validation = EnvelopeValidator.ValidateRawJson(frame);
            Assert.True(validation.IsValid);
        }
    }

    // =========================================================================
    // SCENARIO 6: Áp lực ngược (Backpressure) — kiểm chứng trên ĐÚNG factory production
    //
    // Gọi trực tiếp AgentSocketClient.CreateOutbox(capacity) — cùng factory mà ConnectAsync dùng.
    // Điều này chứng minh cấu hình channel trong production là Bounded + FullMode.Wait,
    // chứ không phải chỉ kiểm tra hành vi của Channel<T> nói chung.
    //
    // Không cần writer loop thực sự hay TCP stack: ta kiểm chứng tại tầng channel.
    // =========================================================================
    [Fact]
    public async Task Scenario6_Backpressure_WhenQueueFull_ProducerBlocks_UntilSpaceFreed()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        // Gọi đúng factory nội bộ mà ConnectAsync dùng trong production.
        // capacity=1 để dễ điền đầy và kiểm chứng backpressure nhanh.
        var ch = AgentSocketClient.CreateOutbox(1);

        // --- Bước 1: Điền đầy channel ---
        // Item đầu tiên phải vào ngay (channel trống, chỗ cho 1 item).
        await ch.Writer.WriteAsync(new byte[] { 0xAA }, cts.Token);
        Assert.Equal(1, ch.Reader.Count);  // channel đầy (1/1)

        // --- Bước 2: Ghi item thứ hai phải bị block ---
        // QUAN TRỌNG: ValueTask chỉ được consume một lần — chuyển sang Task ngay lập tức.
        Task blockedTask = ch.Writer.WriteAsync(new byte[] { 0xBB }, cts.Token).AsTask();
        await Task.Delay(50); // Đủ thời gian để task được lên lịch

        // Task chưa hoàn thành → producer đang bị chặn (backpressure hoạt động)
        Assert.False(blockedTask.IsCompleted,
            "WriteAsync phải block khi channel đầy (FullMode=Wait — không drop frame)");

        // --- Bước 3: Giải phóng chỗ → item bị block phải hoàn thành ---
        bool dequeued = ch.Reader.TryRead(out byte[]? item1);
        Assert.True(dequeued);
        Assert.Equal(new byte[] { 0xAA }, item1); // Đúng item đã ghi, đúng thứ tự FIFO

        // Sau khi có chỗ trống, task bị block phải hoàn thành trong thời gian ngắn
        await blockedTask.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.True(blockedTask.IsCompleted,
            "WriteAsync phải hoàn thành sau khi có chỗ trống trong channel");

        // Xác nhận item thứ hai đã vào channel
        Assert.Equal(1, ch.Reader.Count);
        bool dequeued2 = ch.Reader.TryRead(out byte[]? item2);
        Assert.True(dequeued2);
        Assert.Equal(new byte[] { 0xBB }, item2);
    }

    // =========================================================================
    // SCENARIO 7: Dừng êm đềm (Cancellation) dừng Writer Loop an toàn
    // =========================================================================
    [Fact]
    public async Task Scenario7_Cancellation_StopsWriterCleanly()
    {
        await using var server = new FakeTcpServer();
        using var client = new AgentSocketClient();

        var connectTask = client.ConnectAsync(server.Host, server.Port);
        var serverClientTask = server.AcceptClientAsync();
        await Task.WhenAll(connectTask, serverClientTask);

        // Ngắt kết nối kèm token hủy
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await client.DisconnectAsync(cts.Token);

        Assert.False(client.IsConnected);
    }

    // =========================================================================
    // SCENARIO 8a: Dispose() đồng bộ giải phóng kết nối và writer an toàn
    // =========================================================================
    [Fact]
    public async Task Scenario8a_Dispose_ClosesTcpConnectionAndBackgroundWriterCleanly()
    {
        await using var server = new FakeTcpServer();
        var client = new AgentSocketClient();

        var connectTask = client.ConnectAsync(server.Host, server.Port);
        var serverClientTask = server.AcceptClientAsync();
        await Task.WhenAll(connectTask, serverClientTask);

        Assert.True(client.IsConnected);

        // Gọi Dispose đồng bộ
        client.Dispose();

        Assert.False(client.IsConnected);

        // Dispose nhiều lần không gây crash
        client.Dispose();
        await client.DisposeAsync();

        // Gửi tin sau khi đã Dispose phải ném ObjectDisposedException
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            client.EnqueueFrameAsync([1, 2, 3, 4]).AsTask());
    }

    // =========================================================================
    // SCENARIO 8b: DisposeAsync trực tiếp (KHÔNG gọi Dispose trước)
    // Xác nhận: server quan sát clean EOF (ReadFrameAsync trả null)
    // =========================================================================
    [Fact]
    public async Task Scenario8b_DisposeAsync_DirectCall_ServerObservesCleanEof()
    {
        await using var server = new FakeTcpServer();
        // Dùng var thay vì using để kiểm soát thời điểm dispose chính xác
        var client = new AgentSocketClient();

        var connectTask = client.ConnectAsync(server.Host, server.Port);
        var serverClientTask = server.AcceptClientAsync();
        await Task.WhenAll(connectTask, serverClientTask);

        using var serverConn = await serverClientTask;
        Assert.True(client.IsConnected);

        // Gửi 1 frame để xác nhận kết nối hoạt động trước khi dispose
        byte[] payload = System.Text.Encoding.UTF8.GetBytes("{\"ping\":1}");
        byte[] frame = FrameCodec.Encode(payload);
        await client.EnqueueFrameAsync(frame);

        // Đọc frame đó từ phía server
        var framesBeforeDispose = await server.ReadFramesAsync(serverConn, 1);
        Assert.Single(framesBeforeDispose);

        // Gọi DisposeAsync TRỰC TIẾP — KHÔNG gọi Dispose() trước
        await client.DisposeAsync();

        Assert.False(client.IsConnected);

        // Phía server: ReadFrameAsync tiếp theo phải trả null (clean EOF, không phải exception)
        // vì client đã đóng kết nối bình thường
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        byte[]? eofFrame = await FrameCodec.ReadFrameAsync(serverConn.GetStream(), ct: cts.Token);
        Assert.Null(eofFrame); // Clean EOF — server nhận tín hiệu kết nối đóng

        // Gọi DisposeAsync lần nữa phải không gây crash (idempotent)
        await client.DisposeAsync();
    }

    // =========================================================================
    // SCENARIO 9: Lỗi kết nối ban đầu KHÔNG tự động kết nối lại (No Reconnect)
    // =========================================================================
    [Fact]
    public async Task Scenario9_ConnectionFailure_DoesNotAutomaticallyReconnect()
    {
        using var client = new AgentSocketClient();

        // Chọn một cổng không có dịch vụ nào lắng nghe
        int unusedPort;
        using (var temp = new TcpListener(IPAddress.Loopback, 0))
        {
            temp.Start();
            unusedPort = ((IPEndPoint)temp.LocalEndpoint).Port;
            temp.Stop(); // Cổng đã đóng
        }

        // Kết nối vào cổng đóng phải ném SocketException ngay lập tức
        await Assert.ThrowsAnyAsync<SocketException>(() =>
            client.ConnectAsync("127.0.0.1", unusedPort));

        // Xác nhận trạng thái là ngắt kết nối
        Assert.False(client.IsConnected);

        // Chờ 300ms để kiểm chứng chắc chắn không có task ngầm nào tự động thử lại
        await Task.Delay(300);
        Assert.False(client.IsConnected);
    }

    // =========================================================================
    // SCENARIO 10: Xử lý lỗi ghi — Server đóng kết nối đột ngột
    // Client phải chuyển ra khỏi trạng thái "connected" và không bị treo
    // =========================================================================
    [Fact]
    public async Task Scenario10_WriterFailure_WhenServerClosesConnection_ClientBecomesDisconnected()
    {
        await using var server = new FakeTcpServer();
        using var client = new AgentSocketClient();

        var connectTask = client.ConnectAsync(server.Host, server.Port);
        var serverClientTask = server.AcceptClientAsync();
        await Task.WhenAll(connectTask, serverClientTask);

        using var serverConn = await serverClientTask;
        Assert.True(client.IsConnected);

        // Server đóng kết nối đột ngột (mô phỏng crash server / network drop)
        serverConn.Close();

        // Gửi ít nhất 1 frame để kích hoạt lỗi ghi trong writer loop.
        // Writer loop sẽ nhận IOException tại stream.WriteAsync.
        byte[] payload = System.Text.Encoding.UTF8.GetBytes("{\"trigger\":\"write_failure\"}");
        byte[] frame = FrameCodec.Encode(payload);

        // Thử gửi nhiều lần để chắc chắn writer loop đã có cơ hội phát hiện lỗi
        // (có thể frame đầu tiên nằm trong kernel buffer và chưa thực sự fail)
        for (int i = 0; i < 5; i++)
        {
            try { await client.EnqueueFrameAsync(frame); } catch { break; }
        }

        // Chờ writer loop xử lý lỗi và cập nhật _writerFaulted
        // (writer loop chạy background — cần thời gian ngắn để phát hiện IOException)
        using var waitCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (client.IsConnected && !waitCts.IsCancellationRequested)
        {
            await Task.Delay(20);
        }

        // Sau khi writer faulted: IsConnected phải là false
        Assert.False(client.IsConnected, "IsConnected phải false sau khi writer loop gặp lỗi mạng");

        // Quan trọng: EnqueueFrameAsync tiếp theo KHÔNG được treo mãi mãi
        // Nó phải throw (InvalidOperationException hoặc ChannelClosedException) nhanh chóng
        using var throwCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        await Assert.ThrowsAnyAsync<Exception>(() =>
            client.EnqueueFrameAsync(frame, throwCts.Token).AsTask());
    }

    // =========================================================================
    // Edge Cases: Ràng buộc đối số và trạng thái
    // =========================================================================
    [Fact]
    public async Task EnqueueFrame_WhenNotConnected_ThrowsInvalidOperationException()
    {
        using var client = new AgentSocketClient();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.EnqueueFrameAsync([1, 2, 3, 4]).AsTask());
    }

    [Fact]
    public async Task EnqueueFrame_EmptyFrame_ThrowsArgumentException()
    {
        await using var server = new FakeTcpServer();
        using var client = new AgentSocketClient();

        var connectTask = client.ConnectAsync(server.Host, server.Port);
        var serverClientTask = server.AcceptClientAsync();
        await Task.WhenAll(connectTask, serverClientTask);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.EnqueueFrameAsync(Array.Empty<byte>()).AsTask());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_InvalidCapacity_ThrowsArgumentOutOfRangeException(int capacity)
    {
        // Dùng internal constructor (test seam) để kiểm tra validation
        Assert.Throws<ArgumentOutOfRangeException>(() => new AgentSocketClient(capacity));
    }

    [Fact]
    public void PublicConstructor_AlwaysUses256Capacity()
    {
        // Đảm bảo production constructor luôn dùng đúng 256 theo CP2-001
        using var client = new AgentSocketClient();
        Assert.Equal(AgentSocketClient.DefaultOutboxCapacity, client.OutboxCapacity);
        Assert.Equal(256, client.OutboxCapacity);
    }
}
