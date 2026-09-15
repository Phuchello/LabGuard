using System.Diagnostics;

namespace LabGuard.Agent;

/// <summary>
/// Background heartbeat loop service for LabGuard Agent.
/// Periodically emits HEARTBEAT wire envelopes and receives HEARTBEAT_ACK.
/// If connection drops, loop terminates safely without retrying old heartbeats.
/// </summary>
public class HeartbeatService : IAsyncDisposable
{
    private readonly AgentSocketClient _client;
    private readonly TimeSpan _interval;
    private readonly Action<string>? _logger;
    private readonly Stopwatch _stopwatch = new();
    private CancellationTokenSource? _cts;
    private Task? _heartbeatTask;
    private long _sequence;

    public bool IsRunning => _heartbeatTask != null && !_heartbeatTask.IsCompleted;
    public long CurrentSequence => _sequence;
    public TimeSpan Interval => _interval;

    public HeartbeatService(
        AgentSocketClient client,
        TimeSpan? interval = null,
        Action<string>? logger = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _interval = interval ?? TimeSpan.FromSeconds(5.0);
        _logger = logger;
    }

    public void Start()
    {
        if (IsRunning)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _stopwatch.Restart();
        _heartbeatTask = RunHeartbeatLoopAsync(_cts.Token);
    }

    private async Task RunHeartbeatLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(_interval);
        try
        {
            while (!ct.IsCancellationRequested && await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                if (!_client.IsConnected || !_client.IsAuthenticated)
                {
                    _client.NotifyConnectionLost("Client not connected or authenticated.");
                    break;
                }

                _sequence++;
                long uptime = (long)_stopwatch.Elapsed.TotalSeconds;

                try
                {
                    await _client.SendHeartbeatAsync(_sequence, uptime, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger?.Invoke($"[HeartbeatService] Heartbeat stopped due to error/disconnect: {ex.Message}");
                    _client.NotifyConnectionLost(ex.Message);
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on clean shutdown
        }
        catch (Exception ex)
        {
            _logger?.Invoke($"[HeartbeatService] Heartbeat loop encountered error: {ex.Message}");
        }
    }

    public async Task StopAsync()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            if (_heartbeatTask != null)
            {
                try
                {
                    await _heartbeatTask.ConfigureAwait(false);
                }
                catch
                {
                    // Ignore cancel exceptions
                }
            }

            _cts.Dispose();
            _cts = null;
            _heartbeatTask = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }
}
