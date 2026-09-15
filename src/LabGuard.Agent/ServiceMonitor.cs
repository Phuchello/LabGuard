using System.Diagnostics;
using System.Net.Sockets;
using LabGuard.Protocol.Dtos;

namespace LabGuard.Agent;

public record ServiceTarget(string ServiceName, string Host, int Port);

/// <summary>
/// Monitors a small configured list of local/remote TCP services via TCP handshake probes.
/// Probe timeout: 2.0 seconds.
/// Generates canonical SERVICE_STATUS messages.
/// Latest-state telemetry only: never enqueued in ReplayBuffer, no application retries.
/// </summary>
public class ServiceMonitor : IAsyncDisposable
{
    private readonly AgentSocketClient _client;
    private readonly List<ServiceTarget> _targets = [];
    private readonly TimeSpan _interval;
    private readonly TimeSpan _probeTimeout;
    private readonly Action<string>? _logger;
    private CancellationTokenSource? _cts;
    private Task? _monitorTask;

    public bool IsRunning => _monitorTask != null && !_monitorTask.IsCompleted;

    public ServiceMonitor(
        AgentSocketClient client,
        IEnumerable<ServiceTarget>? targets = null,
        TimeSpan? interval = null,
        TimeSpan? probeTimeout = null,
        Action<string>? logger = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _interval = interval ?? TimeSpan.FromSeconds(5.0);
        _probeTimeout = probeTimeout ?? TimeSpan.FromSeconds(2.0);
        _logger = logger;

        if (targets != null)
        {
            _targets.AddRange(targets);
        }
        else
        {
            // Default configured targets for monitoring
            _targets.Add(new ServiceTarget("HTTP", "127.0.0.1", 80));
            _targets.Add(new ServiceTarget("APP", "127.0.0.1", 8080));
        }
    }

    public void AddTarget(string name, string host, int port)
    {
        _targets.Add(new ServiceTarget(name, host, port));
    }

    public void Start()
    {
        if (IsRunning)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _monitorTask = RunMonitorLoopAsync(_cts.Token);
        _logger?.Invoke($"[ServiceMonitor] Started service monitoring loop ({_targets.Count} targets, interval={_interval.TotalSeconds}s, probeTimeout={_probeTimeout.TotalSeconds}s).");
    }

    private async Task RunMonitorLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(_interval);
        try
        {
            while (!ct.IsCancellationRequested && await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                if (!_client.IsConnected || !_client.IsAuthenticated)
                {
                    continue;
                }

                var items = new List<ServiceStatusItemDto>();
                foreach (var target in _targets)
                {
                    var item = await ProbeTargetAsync(target, ct).ConfigureAwait(false);
                    items.Add(item);
                }

                var payload = new ServiceStatusPayload
                {
                    IsReplay = false,
                    Services = items
                };

                try
                {
                    await _client.SendServiceStatusAsync(payload, ct).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger?.Invoke($"[ServiceMonitor] Failed to send SERVICE_STATUS: {ex.Message}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Clean exit
        }
    }

    public async Task<ServiceStatusItemDto> ProbeTargetAsync(ServiceTarget target, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        bool isListening = false;

        try
        {
            using var probeCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            probeCts.CancelAfter(_probeTimeout);

            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(target.Host, target.Port, probeCts.Token).ConfigureAwait(false);
            isListening = tcpClient.Connected;
        }
        catch
        {
            isListening = false;
        }
        finally
        {
            sw.Stop();
        }

        return new ServiceStatusItemDto
        {
            ServiceName = target.ServiceName,
            Port = target.Port,
            IsListening = isListening,
            ResponseTimeMs = (int)sw.ElapsedMilliseconds,
            LastCheckedUtc = DateTime.UtcNow.ToString("O")
        };
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
                    // Ignore cancel
                }
            }
            _cts.Dispose();
            _cts = null;
            _monitorTask = null;
            _logger?.Invoke("[ServiceMonitor] Service monitoring stopped.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }
}
