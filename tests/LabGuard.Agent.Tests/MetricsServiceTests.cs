using System.Text.Json;
using LabGuard.Protocol;
using LabGuard.Protocol.Dtos;
using LabGuard.Protocol.Serialization;
using Xunit;

namespace LabGuard.Agent.Tests;

/// <summary>
/// Các bài kiểm thử cho MetricsService (Task LG-A-004).
/// </summary>
public class MetricsServiceTests
{
    [Fact]
    public async Task StopAsync_IsRepeatable_AndAllowsRestart()
    {
        using var client = new AgentSocketClient();
        var collector = new WindowsMetricsCollector(new FakeNativeMetricsProvider());
        using var service = new MetricsService(client, collector, period: TimeSpan.FromHours(1));

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
        using var service = new MetricsService(client, collector, period: TimeSpan.FromHours(1));
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
    // TEST 6: Một lần thu thập tạo ra một frame METRIC_BATCH hợp lệ gửi qua AgentSocketClient thật
    // =========================================================================
    [Fact]
    public async Task Test6_OneCollectionCycle_ProducesValidMetricBatch_ThroughRealClientAndServer()
    {
        await using var server = new FakeTcpServer();
        using var client = new AgentSocketClient();

        var connectTask = client.ConnectAsync(server.Host, server.Port);
        var serverClientTask = server.AcceptClientAsync();
        await Task.WhenAll(connectTask, serverClientTask);

        using var serverClient = await serverClientTask;

        var fake = new FakeNativeMetricsProvider
        {
            TotalPhysicalMemory = 16_000_000_000UL,
            AvailablePhysicalMemory = 4_000_000_000UL,
            TickCountMs = 3_600_000UL
        };
        fake.EnqueueCpuSnapshot(1000, 2000, 1000);
        fake.EnqueueCpuSnapshot(1200, 2500, 1500);

        var collector = new WindowsMetricsCollector(fake);
        using var service = new MetricsService(client, collector, deviceId: "PC-LAB01-01");

        // Gửi 1 mẻ số liệu qua hàng đợi
        var payloadSent = await service.SendMetricBatchAsync();

        // Server đọc 1 frame
        var frames = await server.ReadFramesAsync(serverClient, 1);
        Assert.Single(frames);

        // 1. Kiểm tra envelope thô qua EnvelopeValidator
        var (validationResult, envelope) = EnvelopeValidator.ValidateAndParse(frames[0]);
        Assert.True(validationResult.IsValid, $"Envelope không hợp lệ: {validationResult.ErrorMessage}");
        Assert.NotNull(envelope);
        Assert.Equal(MessageTypes.MetricBatch, envelope!.MessageType);
        Assert.Equal("PC-LAB01-01", envelope.DeviceId);

        // 2. Giải mã JSON payload
        var payloadReceived = JsonSerializer.Deserialize<MetricBatchPayload>(
            envelope.Payload.GetRawText(),
            ProtocolSerializer.Options);
        Assert.NotNull(payloadReceived);

        // 3. Kiểm chứng các trường theo yêu cầu A-004
        Assert.True(EnvelopeValidator.IsCanonicalUuidV4(payloadReceived!.BootId), "BootId phải là UUID v4 chuẩn.");
        Assert.True(EnvelopeValidator.IsCanonicalUuidV4(payloadReceived.BatchId), "BatchId phải là UUID v4 chuẩn.");
        Assert.Equal(2, payloadReceived.SampleCount);
        Assert.False(payloadReceived.IsReplay, "A-004 không có ReplayBuffer, isReplay phải là false.");
        Assert.Equal(3600L, payloadReceived.UptimeSeconds);

        // 4. Kiểm chứng các mẫu đo CPU và RAM
        Assert.Equal(2, payloadReceived.Samples.Count);

        var cpuSample = payloadReceived.Samples.FirstOrDefault(s => s.MetricName == "CPU");
        Assert.NotNull(cpuSample);
        Assert.Equal(80.0, cpuSample!.MetricValue);
        Assert.Equal("%", cpuSample.Unit);

        var ramSample = payloadReceived.Samples.FirstOrDefault(s => s.MetricName == "RAM");
        Assert.NotNull(ramSample);
        Assert.Equal(75.0, ramSample!.MetricValue);
        Assert.Equal("%", ramSample.Unit);
    }

    // =========================================================================
    // TEST 7: Hai mẻ số liệu liên tiếp: cùng BootId nhưng khác BatchId
    // =========================================================================
    [Fact]
    public async Task Test7_TwoMetricBatches_HaveSameBootId_AndDifferentBatchId()
    {
        await using var server = new FakeTcpServer();
        using var client = new AgentSocketClient();

        var connectTask = client.ConnectAsync(server.Host, server.Port);
        var serverClientTask = server.AcceptClientAsync();
        await Task.WhenAll(connectTask, serverClientTask);

        using var serverClient = await serverClientTask;

        var fake = new FakeNativeMetricsProvider();
        var collector = new WindowsMetricsCollector(fake);
        using var service = new MetricsService(client, collector, deviceId: "PC-LAB01-01");

        // Gửi mẻ 1
        var batch1 = await service.SendMetricBatchAsync();

        // Gửi mẻ 2
        var batch2 = await service.SendMetricBatchAsync();

        // Kiểm tra tính nhất quán của BootId và tính duy nhất của BatchId
        Assert.Equal(batch1.BootId, batch2.BootId);
        Assert.NotEqual(batch1.BatchId, batch2.BatchId);
        Assert.True(EnvelopeValidator.IsCanonicalUuidV4(batch1.BatchId));
        Assert.True(EnvelopeValidator.IsCanonicalUuidV4(batch2.BatchId));
    }

    // =========================================================================
    // TEST 11: CancellationToken dừng vòng lặp định kỳ một cách êm đềm
    // =========================================================================
    [Fact]
    public async Task Test11_Cancellation_StopsPeriodicLoopCleanly()
    {
        await using var server = new FakeTcpServer();
        using var client = new AgentSocketClient();

        var connectTask = client.ConnectAsync(server.Host, server.Port);
        var serverClientTask = server.AcceptClientAsync();
        await Task.WhenAll(connectTask, serverClientTask);

        var fake = new FakeNativeMetricsProvider();
        var collector = new WindowsMetricsCollector(fake);

        // Khởi tạo service với chu kỳ rất ngắn (20ms) để thử nghiệm
        using var service = new MetricsService(client, collector, period: TimeSpan.FromMilliseconds(20));
        using var cts = new CancellationTokenSource();

        service.Start(cts.Token);

        // Cho vòng lặp chạy một chút rồi dừng
        await Task.Delay(80);
        cts.Cancel();

        await service.StopAsync();
        // Không ném ngoại lệ chưa xử lý, kết thúc sạch sẽ
    }

    // =========================================================================
    // TEST 12: Thu thập chỉ số KHÔNG tự động kết nối lại hay lưu ReplayBuffer
    // =========================================================================
    [Fact]
    public async Task Test12_MetricsService_DoesNotImplementReconnectOrReplay()
    {
        // Client chưa kết nối tới bất kỳ server nào
        using var client = new AgentSocketClient();
        var fake = new FakeNativeMetricsProvider();
        var collector = new WindowsMetricsCollector(fake);
        using var service = new MetricsService(client, collector);

        // Khi chưa kết nối, SendMetricBatchAsync phải ném InvalidOperationException ngay lập tức
        // KHÔNG được tự ý thử reconnect (việc của A-005) và KHÔNG được lưu đệm ngoại tuyến (việc của A-006)
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await service.SendMetricBatchAsync();
        });
    }
}
