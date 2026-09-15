using System.Collections.Concurrent;
using System.Diagnostics;

namespace LabGuard.Central;

public enum DeviceLivenessStatus
{
    Offline,
    Online
}

public class DeviceLivenessRecord
{
    public string DeviceId { get; }
    public long ConnectionGeneration { get; set; }
    public long LastSeenMonotonicTimestamp { get; set; }
    public DeviceLivenessStatus CurrentStatus { get; set; }

    public DeviceLivenessRecord(string deviceId, long connectionGeneration, DeviceLivenessStatus initialStatus = DeviceLivenessStatus.Offline)
    {
        DeviceId = deviceId;
        ConnectionGeneration = connectionGeneration;
        CurrentStatus = initialStatus;
        LastSeenMonotonicTimestamp = 0;
    }

    public double GetElapsedSeconds(long currentTimestamp)
    {
        if (LastSeenMonotonicTimestamp == 0)
        {
            return double.PositiveInfinity;
        }
        return (currentTimestamp - LastSeenMonotonicTimestamp) / (double)Stopwatch.Frequency;
    }
}

/// <summary>
/// Maintains in-memory device liveness records using Monotonic Clock (Stopwatch.GetTimestamp).
/// Tracks per-device ConnectionGeneration to filter out stale socket frames.
/// </summary>
public class MonotonicLivenessRegistry
{
    private readonly ConcurrentDictionary<string, DeviceLivenessRecord> _records = new(StringComparer.OrdinalIgnoreCase);
    private readonly Action<string>? _logger;

    public event Action<string, DeviceLivenessStatus, DeviceLivenessStatus>? OnStatusChanged;

    public MonotonicLivenessRegistry(Action<string>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Registers or updates device registration upon successful AUTH.
    /// Increments ConnectionGeneration. Does NOT mark device ONLINE until first authenticated frame arrives.
    /// </summary>
    public long RegisterOrUpdateDevice(string deviceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);

        var record = _records.AddOrUpdate(
            deviceId,
            id => new DeviceLivenessRecord(id, 1, DeviceLivenessStatus.Offline),
            (id, existing) =>
            {
                existing.ConnectionGeneration++;
                return existing;
            });

        _logger?.Invoke($"[LivenessRegistry] Registered device '{deviceId}' (Gen {record.ConnectionGeneration}, Status {record.CurrentStatus}).");
        return record.ConnectionGeneration;
    }

    /// <summary>
    /// Records activity from an authenticated frame.
    /// Checks ConnectionGeneration: rejects stale frames.
    /// Updates LastSeenMonotonicTimestamp and marks device ONLINE if not already online.
    /// </summary>
    public bool RecordActivity(string deviceId, long connectionGeneration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);

        if (!_records.TryGetValue(deviceId, out var record))
        {
            return false;
        }

        // Stale generation check
        if (connectionGeneration < record.ConnectionGeneration)
        {
            _logger?.Invoke($"[LivenessRegistry] Stale frame rejected for device '{deviceId}': incoming gen {connectionGeneration} < current gen {record.ConnectionGeneration}.");
            return false;
        }

        long now = Stopwatch.GetTimestamp();
        record.LastSeenMonotonicTimestamp = now;

        if (record.CurrentStatus != DeviceLivenessStatus.Online)
        {
            var oldStatus = record.CurrentStatus;
            record.CurrentStatus = DeviceLivenessStatus.Online;
            _logger?.Invoke($"[Liveness] Device '{deviceId}' transitioned {oldStatus} -> ONLINE (Gen {connectionGeneration}).");
            OnStatusChanged?.Invoke(deviceId, oldStatus, DeviceLivenessStatus.Online);
        }

        return true;
    }

    public DeviceLivenessRecord? GetRecord(string deviceId)
    {
        _records.TryGetValue(deviceId, out var record);
        return record;
    }

    public DeviceLivenessStatus GetStatus(string deviceId)
    {
        return _records.TryGetValue(deviceId, out var record)
            ? record.CurrentStatus
            : DeviceLivenessStatus.Offline;
    }

    public long GetLatestGeneration(string deviceId)
    {
        return _records.TryGetValue(deviceId, out var record)
            ? record.ConnectionGeneration
            : 0;
    }

    public IReadOnlyCollection<DeviceLivenessRecord> GetAllRecords()
    {
        return _records.Values.ToArray();
    }

    internal void NotifyStatusChanged(string deviceId, DeviceLivenessStatus oldStatus, DeviceLivenessStatus newStatus)
    {
        OnStatusChanged?.Invoke(deviceId, oldStatus, newStatus);
    }
}
