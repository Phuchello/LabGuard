using LabGuard.Protocol.Dtos;

namespace LabGuard.Agent;

/// <summary>
/// Thread-safe in-memory FIFO ReplayBuffer for unacknowledged telemetry batches (CP2-003).
/// Holds up to 50 batches. When full, drops the oldest batch and logs TELEMETRY_DROPPED.
/// Only stores METRIC_BATCH. Never stores HEARTBEAT or SERVICE_STATUS.
/// Preserves original BootId and BatchId on retry, marking IsReplay = true.
/// </summary>
public class ReplayBuffer
{
    public const int MaxCapacity = 50;

    private readonly object _lock = new();
    private readonly LinkedList<MetricBatchPayload> _buffer = new();
    private readonly Action<string>? _logger;

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _buffer.Count;
            }
        }
    }

    public ReplayBuffer(Action<string>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Enqueues a new unacknowledged metric batch.
    /// If capacity (50) is exceeded, drops the oldest batch (FIFO) and logs TELEMETRY_DROPPED.
    /// </summary>
    public void Enqueue(MetricBatchPayload batch)
    {
        ArgumentNullException.ThrowIfNull(batch);

        lock (_lock)
        {
            if (_buffer.Count >= MaxCapacity)
            {
                var oldest = _buffer.First!.Value;
                _buffer.RemoveFirst();
                _logger?.Invoke($"[ReplayBuffer] TELEMETRY_DROPPED: buffer capacity {MaxCapacity} exceeded. Dropped oldest batch {oldest.BatchId}.");
            }

            _buffer.AddLast(batch);
        }
    }

    /// <summary>
    /// Removes a batch upon receiving matching BATCH_ACK from Central Server.
    /// </summary>
    public bool Acknowledge(string batchId)
    {
        if (string.IsNullOrEmpty(batchId))
        {
            return false;
        }

        lock (_lock)
        {
            var current = _buffer.First;
            while (current != null)
            {
                if (string.Equals(current.Value.BatchId, batchId, StringComparison.OrdinalIgnoreCase))
                {
                    _buffer.Remove(current);
                    return true;
                }
                current = current.Next;
            }
        }

        return false;
    }

    /// <summary>
    /// Retrieves all pending unacknowledged batches marked with IsReplay = true.
    /// Preserves original BootId, BatchId, Uptime, and Samples.
    /// </summary>
    public List<MetricBatchPayload> GetPendingReplays()
    {
        lock (_lock)
        {
            var replays = new List<MetricBatchPayload>(_buffer.Count);
            foreach (var batch in _buffer)
            {
                // Clone batch reference but ensure IsReplay is set to true
                replays.Add(new MetricBatchPayload
                {
                    BootId = batch.BootId,
                    BatchId = batch.BatchId,
                    SampleCount = batch.SampleCount,
                    UptimeSeconds = batch.UptimeSeconds,
                    IsReplay = true,
                    Samples = [.. batch.Samples]
                });
            }
            return replays;
        }
    }

    /// <summary>
    /// Clears all buffered items.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _buffer.Clear();
        }
    }
}
