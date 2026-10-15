using System.Runtime.InteropServices;
using Xunit;

namespace LabGuard.Agent.Tests;

/// <summary>
/// Các bài kiểm thử đơn vị cho WindowsMetricsCollector (Task LG-A-004).
/// </summary>
public class MetricsCollectorTests
{
    // =========================================================================
    // TEST 1: Công thức tính % CPU với các mẫu giả lập tất định (Deterministic)
    // =========================================================================
    [Fact]
    public void Test1_CpuFormula_WithDeterministicSnapshots()
    {
        var fake = new FakeNativeMetricsProvider();
        // Mẫu 1 (Prime khởi tạo trong constructor):
        // idle = 1000, kernel = 2000, user = 1000
        fake.EnqueueCpuSnapshot(idle: 1000, kernel: 2000, user: 1000);

        var collector = new WindowsMetricsCollector(fake);

        // Mẫu 2 (Lần đọc thứ hai):
        // idle = 1200, kernel = 2500, user = 1500
        // kernelDelta = 500, userDelta = 500 -> totalDelta = 1000
        // idleDelta = 200
        // busyDelta = totalDelta - idleDelta = 1000 - 200 = 800
        // cpuPercent = (800 / 1000) * 100 = 80.0%
        fake.EnqueueCpuSnapshot(idle: 1200, kernel: 2500, user: 1500);

        double cpu = collector.ReadCpuPercent();

        Assert.Equal(80.0, cpu, precision: 2);
    }

    // =========================================================================
    // TEST 2: Kết quả CPU luôn được giới hạn chặt chẽ trong khoảng [0, 100]
    // =========================================================================
    [Theory]
    [InlineData(1000, 2000, 1000, 1000, 2000, 1000, 0.0)]     // totalDelta = 0 -> 0%
    [InlineData(1000, 2000, 1000, 1500, 2100, 1000, 0.0)]     // idleDelta (500) > totalDelta (100) do glitch -> clamped 0%
    [InlineData(1000, 2000, 1000, 1000, 3000, 2000, 100.0)]   // 100% bận (idleDelta = 0) -> 100%
    public void Test2_CpuResult_IsClamped_BetweenZeroAndOneHundred(
        ulong idle1, ulong kernel1, ulong user1,
        ulong idle2, ulong kernel2, ulong user2,
        double expectedCpu)
    {
        var fake = new FakeNativeMetricsProvider();
        fake.EnqueueCpuSnapshot(idle1, kernel1, user1);

        var collector = new WindowsMetricsCollector(fake);
        fake.EnqueueCpuSnapshot(idle2, kernel2, user2);

        double cpu = collector.ReadCpuPercent();

        Assert.InRange(cpu, 0.0, 100.0);
        Assert.Equal(expectedCpu, cpu, precision: 2);
    }

    // =========================================================================
    // TEST 3: Tính toán RAM: Used = Total - Available và tỷ lệ % chính xác
    // =========================================================================
    [Fact]
    public void Test3_RamCalculation_UsedEqualsTotalMinusAvailable()
    {
        var fake = new FakeNativeMetricsProvider
        {
            TotalPhysicalMemory = 16_000_000_000UL,   // 16 GB
            AvailablePhysicalMemory = 4_000_000_000UL // 4 GB
        };

        var collector = new WindowsMetricsCollector(fake);
        var snapshot = collector.ReadMemoryBytes();

        Assert.Equal(16_000_000_000UL, snapshot.TotalPhysicalBytes);
        Assert.Equal(4_000_000_000UL, snapshot.AvailablePhysicalBytes);
        Assert.Equal(12_000_000_000UL, snapshot.UsedPhysicalBytes);

        // Kiểm tra tính tỷ lệ % RAM: (12 / 16) * 100 = 75.0%
        double ramPercent = ((double)snapshot.UsedPhysicalBytes / snapshot.TotalPhysicalBytes) * 100.0;
        Assert.Equal(75.0, ramPercent, precision: 2);
    }

    // =========================================================================
    // TEST 4: Chuyển đổi Uptime từ mili-giây sang giây chính xác và đơn điệu
    // =========================================================================
    [Fact]
    public void Test4_UptimeConversion_IsCorrectAndMonotonic()
    {
        var fake = new FakeNativeMetricsProvider
        {
            TickCountMs = 125_400UL // 125.4 giây
        };

        var collector = new WindowsMetricsCollector(fake);
        long uptime1 = collector.ReadUptimeSeconds();
        Assert.Equal(125L, uptime1);

        // Tiến thời gian hệ thống lên 250 giây
        fake.TickCountMs = 250_999UL;
        long uptime2 = collector.ReadUptimeSeconds();
        Assert.Equal(250L, uptime2);
        Assert.True(uptime2 >= uptime1, "Uptime phải tăng đơn điệu.");
    }

    // =========================================================================
    // TEST 5: Smoke test trên Windows thật (Real Win32 P/Invoke Calls)
    // =========================================================================
    [Fact]
    public void Test5_RealWindowsCollector_SmokeTest()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Bỏ qua kiểm thử native nếu không chạy trên Windows thật
            return;
        }

        var collector = new WindowsMetricsCollector();

        // 1. Kiểm tra CPU
        double cpu = collector.ReadCpuPercent();
        Assert.InRange(cpu, 0.0, 100.0);

        // 2. Kiểm tra RAM
        var ram = collector.ReadMemoryBytes();
        Assert.True(ram.TotalPhysicalBytes > 0, "Tổng RAM vật lý phải > 0.");
        Assert.True(ram.UsedPhysicalBytes <= ram.TotalPhysicalBytes, "RAM sử dụng phải <= Tổng RAM.");

        // 3. Kiểm tra Uptime
        long uptime = collector.ReadUptimeSeconds();
        Assert.True(uptime > 0, "Uptime của máy trạm phải > 0 giây.");
    }

    // =========================================================================
    // Xử lý lỗi khi Win32 API trả về thất bại
    // =========================================================================
    [Fact]
    public void NativeFailure_GetSystemTimes_ThrowsInvalidOperationException()
    {
        var fake = new FakeNativeMetricsProvider { FailGetSystemTimes = true };
        var collector = new WindowsMetricsCollector(fake);

        Assert.Throws<InvalidOperationException>(() => collector.ReadCpuPercent());
    }

    [Fact]
    public void NativeFailure_GlobalMemoryStatusEx_ThrowsInvalidOperationException()
    {
        var fake = new FakeNativeMetricsProvider { FailGlobalMemoryStatusEx = true };
        var collector = new WindowsMetricsCollector(fake);

        Assert.Throws<InvalidOperationException>(() => collector.ReadMemoryBytes());
    }

    // =========================================================================
    // Tiêu chí hiệu năng WBS: Thời gian thu thập mỗi chu kỳ < 100ms
    // =========================================================================
    [Fact]
    public void PerformanceTest_CollectionLatency_IsUnder100ms()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        var collector = new WindowsMetricsCollector();
        // Chờ ngắn để có delta CPU
        Thread.Sleep(50);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 10; i++)
        {
            collector.ReadCpuPercent();
            collector.ReadMemoryBytes();
            collector.ReadUptimeSeconds();
        }
        sw.Stop();

        double avgLatencyMs = sw.Elapsed.TotalMilliseconds / 10.0;
        Assert.True(avgLatencyMs < 100.0, $"Thời gian thu thập trung bình ({avgLatencyMs:F3}ms) phải nhỏ hơn ngưỡng 100ms.");
    }
}
