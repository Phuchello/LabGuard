namespace LabGuard.Agent;

/// <summary>
/// Background periodic telemetry collection service using PeriodicTimer (default 5.0s).
/// Periodically collects CPU, RAM, and Uptime via MetricsCollector, constructs METRIC_BATCH,
/// and transmits it through the controlled Agent client.
/// </summary>
public class MetricsService : IAsyncDisposable
{
    private readonly AgentSocketClient _client;
    private readonly TimeSpan _interval;
    private readonly Action<string>? _logger;
    private readonly MetricsCollector _collector;
    private readonly string _bootId;
    private CancellationTokenSource? _cts;
    private Task? _timerTask;

    public bool IsRunning => _timerTask != null && !_timerTask.IsCompleted;
    public string BootId => _bootId;

    public MetricsService(
        AgentSocketClient client,
        TimeSpan? interval = null,
        Action<string>? logger = null,
        MetricsCollector? collector = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _interval = interval ?? TimeSpan.FromSeconds(5.0);
        _logger = logger;
        _collector = collector ?? new MetricsCollector(logger);
        _bootId = Guid.NewGuid().ToString("D");
    }

    public void Start()
    {
        if (IsRunning)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _timerTask = RunMetricsLoopAsync(_cts.Token);
        _logger?.Invoke($"[MetricsService] Started telemetry loop (interval={_interval.TotalSeconds}s, bootId={_bootId}).");
    }

    private async Task RunMetricsLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(_interval);
        try
        {
            while (!ct.IsCancellationRequested && await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                try
                {
                    var batch = _collector.CollectBatch(_bootId);
                    if (batch == null)
                    {
                        continue;
                    }

                    // Agent owns METRIC_BATCH until matching BATCH_ACK (ReplayBuffer capacity 50, Drop-Oldest)
                    _client.ReplayBuffer.Enqueue(batch);

                    double? cpu = batch.Samples.Find(s => s.MetricName == "CPU")?.MetricValue;
                    double? ram = batch.Samples.Find(s => s.MetricName == "RAM")?.MetricValue;

                    _logger?.Invoke($"[MetricsService] Sending METRIC_BATCH batchId={batch.BatchId} CPU={cpu}% RAM={ram}% Uptime={batch.UptimeSeconds}s");
                    var ack = await _client.SendMetricBatchAsync(batch, ct: ct).ConfigureAwait(false);

                    if (ack.Accepted)
                    {
                        _client.ReplayBuffer.Acknowledge(batch.BatchId);
                        _logger?.Invoke($"[MetricsService] BATCH_ACK received for batchId={batch.BatchId} (samples={ack.SamplesRecorded}).");
                    }
                    else
                    {
                        _logger?.Invoke($"[MetricsService] BATCH_ACK rejected for batchId={batch.BatchId}: {ack.ErrorMessage}");
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (TimeoutException tex)
                {
                    _logger?.Invoke($"[MetricsService] Telemetry send timed out: {tex.Message}");
                }
                catch (Exception ex)
                {
                    _logger?.Invoke($"[MetricsService] Telemetry send failed: {ex.Message}");
                    _client.NotifyConnectionLost(ex.Message);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on clean shutdown
        }
        catch (Exception ex)
        {
            _logger?.Invoke($"[MetricsService] Unhandled error in metrics loop: {ex.Message}");
        }
    }

    public async Task StopAsync()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            if (_timerTask != null)
            {
                try
                {
                    await _timerTask.ConfigureAwait(false);
                }
                catch
                {
                    // Ignore cancel
                }
            }

            _cts.Dispose();
            _cts = null;
            _timerTask = null;
            _logger?.Invoke("[MetricsService] Telemetry loop stopped.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }
}
