namespace LabGuard.Agent.Tests;

/// <summary>
/// Nhà cung cấp số liệu hệ thống giả lập phục vụ kiểm thử đơn vị độc lập và có tính tất định (deterministic).
/// </summary>
internal sealed class FakeNativeMetricsProvider : ISystemMetricsNativeProvider
{
    private readonly Queue<(ulong idle, ulong kernel, ulong user)> _cpuSnapshots = new();

    public ulong TotalPhysicalMemory { get; set; } = 16_000_000_000UL; // 16 GB
    public ulong AvailablePhysicalMemory { get; set; } = 4_000_000_000UL; // 4 GB
    public ulong TickCountMs { get; set; } = 3_600_000UL; // 3600 giây (1 giờ)

    public bool FailGetSystemTimes { get; set; }
    public bool FailGlobalMemoryStatusEx { get; set; }

    public void EnqueueCpuSnapshot(ulong idle, ulong kernel, ulong user)
    {
        _cpuSnapshots.Enqueue((idle, kernel, user));
    }

    public bool GetSystemTimes(out ulong idleTime, out ulong kernelTime, out ulong userTime)
    {
        if (FailGetSystemTimes)
        {
            idleTime = 0;
            kernelTime = 0;
            userTime = 0;
            return false;
        }

        if (_cpuSnapshots.Count > 0)
        {
            var snap = _cpuSnapshots.Dequeue();
            idleTime = snap.idle;
            kernelTime = snap.kernel;
            userTime = snap.user;
            return true;
        }

        idleTime = 1000;
        kernelTime = 2000;
        userTime = 1000;
        return true;
    }

    public bool GlobalMemoryStatusEx(out ulong totalPhysical, out ulong availablePhysical)
    {
        if (FailGlobalMemoryStatusEx)
        {
            totalPhysical = 0;
            availablePhysical = 0;
            return false;
        }

        totalPhysical = TotalPhysicalMemory;
        availablePhysical = AvailablePhysicalMemory;
        return true;
    }

    public ulong GetTickCount64()
    {
        return TickCountMs;
    }
}
