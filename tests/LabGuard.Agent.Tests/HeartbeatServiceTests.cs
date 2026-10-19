using System.Text.Json;
using LabGuard.Protocol;
using LabGuard.Protocol.Dtos;
using LabGuard.Protocol.Serialization;
using Xunit;

namespace LabGuard.Agent.Tests;

/// <summary>
/// Các bài kiểm thử cho HeartbeatService và kiểm thử đồng thời cùng MetricsService (Task LG-A-004).
/// </summary>
public class HeartbeatServiceTests
{
    [Fact]
    public async Task StopAsync_IsRepeatable_AndAllowsRestart()
    {
        using var client = new AgentSocketClient();
        var collector = new WindowsMetricsCollector(new FakeNativeMetricsProvider());
        using var service = new HeartbeatService(client, collector, period: TimeSpan.FromHours(1));

        await service.StopAsync();
        service.Start();
        await service.StopAsync();
        await service.StopAsync();
        service.Start();
        await service.StopAsync();
    }

    [Fact]
    public async Task Start_WhenAlreadyStarted_RejectsDuplicateLoop()
    {
        using var client = new AgentSocketClient();
        var collector = new WindowsMetricsCollector(new FakeNativeMetricsProvider());
        using var service = new HeartbeatService(client, collector, period: TimeSpan.FromHours(1));
        using var cancellation = new CancellationTokenSource();

        service.Start(cancellation.Token);
        try
        {
            Assert.Throws<InvalidOperationException>(() => service.Start(cancellation.Token));
        }
        finally
        {
            // Also cancel the first loop if testing the unfixed implementation.
            cancellation.Cancel();
            await service.StopAsync();
        }
    }

    // =========================================================================
    // TEST 8: HeartbeatService phát bản tin HEARTBEAT hợp lệ
    // =========================================================================
    [Fact]
    public async Task Test8_HeartbeatService_ProducesCanonicalHeartbeatEnvelope()
    {
        await using var server = new FakeTcpServer();
        using var client = new AgentSocketClient();

        var connectTask = client.ConnectAsync(server.Host, server.Port);
        var serverClientTask = server.AcceptClientAsync();
        await Task.WhenAll(connectTask, serverClientTask);

        using var serverClient = await serverClientTask;

        var fake = new FakeNativeMetricsProvider { TickCountMs = 120_000UL }; // 120s
        var collector = new WindowsMetricsCollector(fake);
        using var heartbeatService = new HeartbeatService(client, collector, deviceId: "PC-LAB01-01");

        // Gửi nhịp tim đầu tiên
        var sentPayload = await heartbeatService.SendHeartbeatAsync();

        // Đọc 1 frame từ server
        var frames = await server.ReadFramesAsync(serverClient, 1);
        Assert.Single(frames);

        var (validation, envelope) = EnvelopeValidator.ValidateAndParse(frames[0]);
        Assert.True(validation.IsValid, $"Envelope nhịp tim không hợp lệ: {validation.ErrorMessage}");
        Assert.NotNull(envelope);
        Assert.Equal(MessageTypes.Heartbeat, envelope!.MessageType);
        Assert.Equal("PC-LAB01-01", envelope.DeviceId);

        var receivedPayload = JsonSerializer.Deserialize<HeartbeatPayload>(
            envelope.Payload.GetRawText(),
            ProtocolSerializer.Options);

        Assert.NotNull(receivedPayload);
        Assert.Equal(1L, receivedPayload!.SequenceNumber);
        Assert.Equal(120L, receivedPayload.UptimeSeconds);
    }

    // =========================================================================
    // TEST 9: Hai lần gửi nhịp tim liên tiếp tạo SequenceNumber tăng đơn điệu
    // =========================================================================
    [Fact]
    public async Task Test9_TwoHeartbeats_ProduceIncreasingSequenceNumber()
    {
        await using var server = new FakeTcpServer();
        using var client = new AgentSocketClient();

        var connectTask = client.ConnectAsync(server.Host, server.Port);
        var serverClientTask = server.AcceptClientAsync();
        await Task.WhenAll(connectTask, serverClientTask);

        using var serverClient = await serverClientTask;

        var fake = new FakeNativeMetricsProvider();
        var collector = new WindowsMetricsCollector(fake);
        using var heartbeatService = new HeartbeatService(client, collector);

        var hb1 = await heartbeatService.SendHeartbeatAsync();
        var hb2 = await heartbeatService.SendHeartbeatAsync();

        Assert.Equal(1L, hb1.SequenceNumber);
        Assert.Equal(2L, hb2.SequenceNumber);
        Assert.Equal(2L, heartbeatService.CurrentSequenceNumber);
    }

    // =========================================================================
    // TEST 10: MetricsService và HeartbeatService cùng đẩy đồng thời vào AgentSocketClient
    // Không bị trộn lẫn byte, xé khung hay nghẽn luồng (Single-Writer Queue A-003)
    // =========================================================================
    [Fact]
    public async Task Test10_MetricsAndHeartbeat_EnqueueConcurrently_WithoutFrameCorruption()
    {
        await using var server = new FakeTcpServer();
        using var client = new AgentSocketClient();

        var connectTask = client.ConnectAsync(server.Host, server.Port);
        var serverClientTask = server.AcceptClientAsync();
        await Task.WhenAll(connectTask, serverClientTask);

        using var serverClient = await serverClientTask;

        var fake = new FakeNativeMetricsProvider();
        var collector = new WindowsMetricsCollector(fake);

        using var metricsService = new MetricsService(client, collector, deviceId: "PC-CONCURRENT-01");
        using var heartbeatService = new HeartbeatService(client, collector, deviceId: "PC-CONCURRENT-01");

        const int iterations = 15;
        const int totalExpectedFrames = iterations * 2; // 15 Metrics + 15 Heartbeats = 30 frames

        // Khởi động 2 tác vụ đẩy tin đồng thời không có khóa (lock)
        var metricsTask = Task.Run(async () =>
        {
            for (int i = 0; i < iterations; i++)
            {
                await metricsService.SendMetricBatchAsync();
            }
        });

        var heartbeatTask = Task.Run(async () =>
        {
            for (int i = 0; i < iterations; i++)
            {
                await heartbeatService.SendHeartbeatAsync();
            }
        });

        // Server đọc đủ 30 frames
        var readFramesTask = server.ReadFramesAsync(serverClient, totalExpectedFrames);

        await Task.WhenAll(metricsTask, heartbeatTask);
        var received = await readFramesTask;

        Assert.Equal(totalExpectedFrames, received.Count);

        int metricBatchCount = 0;
        int heartbeatCount = 0;

        // Xác minh toàn bộ 30 frames đều nguyên vẹn, giải mã được envelope 6 trường
        foreach (var frameBytes in received)
        {
            var (validation, envelope) = EnvelopeValidator.ValidateAndParse(frameBytes);
            Assert.True(validation.IsValid, $"Frame bị hỏng hoặc trộn byte: {validation.ErrorMessage}");
            Assert.NotNull(envelope);

            if (envelope!.MessageType == MessageTypes.MetricBatch)
            {
                metricBatchCount++;
            }
            else if (envelope.MessageType == MessageTypes.Heartbeat)
            {
                heartbeatCount++;
            }
            else
            {
                Assert.Fail($"Loại message không xác định: {envelope.MessageType}");
            }
        }

        Assert.Equal(iterations, metricBatchCount);
        Assert.Equal(iterations, heartbeatCount);
    }

    // =========================================================================
    // TEST 11b: CancellationToken dừng vòng lặp định kỳ của HeartbeatService
    // =========================================================================
    [Fact]
    public async Task Test11_Heartbeat_Cancellation_StopsPeriodicLoopCleanly()
    {
        await using var server = new FakeTcpServer();
        using var client = new AgentSocketClient();

        var connectTask = client.ConnectAsync(server.Host, server.Port);
        var serverClientTask = server.AcceptClientAsync();
        await Task.WhenAll(connectTask, serverClientTask);

        var fake = new FakeNativeMetricsProvider();
        var collector = new WindowsMetricsCollector(fake);

        using var service = new HeartbeatService(client, collector, period: TimeSpan.FromMilliseconds(20));
        using var cts = new CancellationTokenSource();

        service.Start(cts.Token);
        await Task.Delay(80);
        cts.Cancel();

        await service.StopAsync();
    }
}
