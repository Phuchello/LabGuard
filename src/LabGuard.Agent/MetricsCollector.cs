using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using LabGuard.Protocol.Dtos;

namespace LabGuard.Agent;

/// <summary>
/// Collects real Windows machine performance metrics: CPU percent, RAM percent, Uptime.
/// Strictly excludes Disk telemetry per frozen Checkpoint 1 baseline.
/// </summary>
public class MetricsCollector
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(out FILETIME lpIdleTime, out FILETIME lpKernelTime, out FILETIME lpUserTime);

    private readonly Action<string>? _logger;
    private ulong _prevIdleTime;
    private ulong _prevKernelTime;
    private ulong _prevUserTime;
    private bool _hasPreviousCpuSample;

    public MetricsCollector(Action<string>? logger = null)
    {
        _logger = logger;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (SampleCpuTimes(out _prevIdleTime, out _prevKernelTime, out _prevUserTime))
            {
                _hasPreviousCpuSample = true;
            }
        }
    }

    /// <summary>
    /// Gets current physical system RAM usage as percentage (0.00% to 100.00%).
    /// Returns null if metric collection fails.
    /// </summary>
    public virtual double? GetMemoryPercent()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var memStatus = new MEMORYSTATUSEX();
                memStatus.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
                if (GlobalMemoryStatusEx(ref memStatus))
                {
                    return Math.Round((double)memStatus.dwMemoryLoad, 2);
                }
                _logger?.Invoke("[MetricsCollector] METRIC_COLLECTION_FAILED: GlobalMemoryStatusEx returned false.");
            }
            else
            {
                _logger?.Invoke("[MetricsCollector] METRIC_COLLECTION_FAILED: Non-Windows OS platform detected.");
            }
        }
        catch (Exception ex)
        {
            _logger?.Invoke($"[MetricsCollector] METRIC_COLLECTION_FAILED: RAM collection error: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Gets current total system CPU usage as percentage (0.00% to 100.00%).
    /// Returns null if metric collection fails.
    /// </summary>
    public virtual double? GetCpuPercent()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (SampleCpuTimes(out ulong idle, out ulong kernel, out ulong user))
                {
                    if (!_hasPreviousCpuSample)
                    {
                        _prevIdleTime = idle;
                        _prevKernelTime = kernel;
                        _prevUserTime = user;
                        _hasPreviousCpuSample = true;
                        return 0.0;
                    }

                    ulong idleDelta = idle - _prevIdleTime;
                    ulong kernelDelta = kernel - _prevKernelTime;
                    ulong userDelta = user - _prevUserTime;

                    _prevIdleTime = idle;
                    _prevKernelTime = kernel;
                    _prevUserTime = user;

                    ulong totalSys = kernelDelta + userDelta;
                    if (totalSys > 0)
                    {
                        double cpu = (1.0 - ((double)idleDelta / totalSys)) * 100.0;
                        return Math.Round(Math.Clamp(cpu, 0.0, 100.0), 2);
                    }
                }
                _logger?.Invoke("[MetricsCollector] METRIC_COLLECTION_FAILED: GetSystemTimes failed or totalSys <= 0.");
            }
            else
            {
                _logger?.Invoke("[MetricsCollector] METRIC_COLLECTION_FAILED: Non-Windows OS platform detected.");
            }
        }
        catch (Exception ex)
        {
            _logger?.Invoke($"[MetricsCollector] METRIC_COLLECTION_FAILED: CPU collection error: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Gets system uptime in seconds.
    /// </summary>
    public virtual long GetUptimeSeconds()
    {
        return Environment.TickCount64 / 1000;
    }

    private static bool SampleCpuTimes(out ulong idle, out ulong kernel, out ulong user)
    {
        if (GetSystemTimes(out FILETIME idleFt, out FILETIME kernelFt, out FILETIME userFt))
        {
            idle = FileTimeToULong(idleFt);
            kernel = FileTimeToULong(kernelFt);
            user = FileTimeToULong(userFt);
            return true;
        }

        idle = 0;
        kernel = 0;
        user = 0;
        return false;
    }

    private static ulong FileTimeToULong(FILETIME ft)
    {
        return ((ulong)(uint)ft.dwHighDateTime << 32) | (uint)ft.dwLowDateTime;
    }

    /// <summary>
    /// Collects a new METRIC_BATCH payload containing real CPU, RAM, and Uptime.
    /// Returns null if metric collection failed, skipping this telemetry cycle.
    /// </summary>
    public MetricBatchPayload? CollectBatch(string bootId)
    {
        double? cpu = GetCpuPercent();
        double? ram = GetMemoryPercent();
        long uptime = GetUptimeSeconds();

        if (cpu == null || ram == null)
        {
            _logger?.Invoke($"[MetricsCollector] METRIC_COLLECTION_FAILED: Incomplete metric sample (CPU={cpu?.ToString() ?? "null"}, RAM={ram?.ToString() ?? "null"}). Telemetry cycle skipped.");
            return null;
        }

        string nowUtc = DateTime.UtcNow.ToString("O");
        return new MetricBatchPayload
        {
            BootId = bootId,
            BatchId = Guid.NewGuid().ToString("D"),
            SampleCount = 2,
            UptimeSeconds = uptime,
            IsReplay = false,
            Samples =
            [
                new MetricSampleDto
                {
                    MetricName = "CPU",
                    MetricValue = cpu.Value,
                    Unit = "%",
                    RecordedAtUtc = nowUtc
                },
                new MetricSampleDto
                {
                    MetricName = "RAM",
                    MetricValue = ram.Value,
                    Unit = "%",
                    RecordedAtUtc = nowUtc
                }
            ]
        };
    }
}
