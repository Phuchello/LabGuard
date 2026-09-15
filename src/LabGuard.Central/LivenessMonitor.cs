using System.Diagnostics;

namespace LabGuard.Central;

/// <summary>
/// Background liveness monitor running a controlled PeriodicTimer (default 2.5s).
/// Evaluates monotonic time elapsed since LastSeen for every registered agent device.
/// Triggers the ONLINE -> OFFLINE transition exactly once when elapsed > offlineThreshold (default 20.0s).
/// </summary>
public class LivenessMonitor : IAsyncDisposable
{
    private readonly MonotonicLivenessRegistry _registry;
    private readonly TimeSpan _checkInterval;
    private readonly TimeSpan _offlineThreshold;
    private readonly Action<string>? _logger;
    private CancellationTokenSource? _cts;
    private Task? _monitorTask;

    public bool IsRunning => _monitorTask != null && !_monitorTask.IsCompleted;
    public TimeSpan CheckInterval => _checkInterval;
    public TimeSpan OfflineThreshold => _offlineThreshold;

    public LivenessMonitor(
        MonotonicLivenessRegistry registry,
        TimeSpan? checkInterval = null,
        TimeSpan? offlineThreshold = null,
        Action<string>? logger = null)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _checkInterval = checkInterval ?? TimeSpan.FromSeconds(2.5);
        _offlineThreshold = offlineThreshold ?? TimeSpan.FromSeconds(20.0);
        _logger = logger;
    }

    public void Start()
    {
        if (IsRunning)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _monitorTask = RunScanLoopAsync(_cts.Token);
        _logger?.Invoke($"[LivenessMonitor] Started liveness scanner (interval={_checkInterval.TotalSeconds}s, threshold={_offlineThreshold.TotalSeconds}s).");
    }

    private async Task RunScanLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(_checkInterval);
        try
        {
            while (!ct.IsCancellationRequested && await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                ScanDevices();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on clean shutdown
        }
        catch (Exception ex)
        {
            _logger?.Invoke($"[LivenessMonitor] Scanner encountered unhandled error: {ex.Message}");
        }
    }

    /// <summary>
    /// Performs one atomic scan pass over all registered devices.
    /// Marks any ONLINE device as OFFLINE if monotonic elapsed time > threshold.
    /// </summary>
    public void ScanDevices()
    {
        long now = Stopwatch.GetTimestamp();

        foreach (var record in _registry.GetAllRecords())
        {
            if (record.CurrentStatus == DeviceLivenessStatus.Online)
            {
                double elapsedSeconds = record.GetElapsedSeconds(now);
                if (elapsedSeconds > _offlineThreshold.TotalSeconds)
                {
                    record.CurrentStatus = DeviceLivenessStatus.Offline;
                    _logger?.Invoke($"[Liveness] Device '{record.DeviceId}' transitioned ONLINE -> OFFLINE (elapsed: {elapsedSeconds:F2}s > {_offlineThreshold.TotalSeconds:F2}s, Gen {record.ConnectionGeneration}).");
                    _registry.NotifyStatusChanged(record.DeviceId, DeviceLivenessStatus.Online, DeviceLivenessStatus.Offline);
                }
            }
        }
    }

    public async Task StopAsync()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            if (_monitorTask != null)
            {
                try
                {
                    await _monitorTask.ConfigureAwait(false);
                }
                catch
                {
                    // Ignore cancel exceptions
                }
            }

            _cts.Dispose();
            _cts = null;
            _monitorTask = null;
            _logger?.Invoke("[LivenessMonitor] Stopped liveness scanner.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }
}
