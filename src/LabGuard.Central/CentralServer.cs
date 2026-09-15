using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using LabGuard.Protocol;

namespace LabGuard.Central;

/// <summary>
/// Minimal TCP Central Server spike for Gate 3.
/// Listens on loopback or configured endpoint, accepts clients, and manages active ClientConnections
/// with full task lifecycle tracking and deterministic shutdown.
/// </summary>
public class CentralServer : IAsyncDisposable
{
    private readonly IPAddress _ipAddress;
    private readonly int _configuredPort;
    private readonly string _expectedToken;
    private readonly Action<string>? _logger;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _acceptTask;
    private readonly ConcurrentDictionary<string, ConnectionRuntime> _activeConnections = new();

    public int Port { get; private set; }
    public bool IsRunning => _listener != null;
    public int ActiveConnectionCount => _activeConnections.Count;
    public MonotonicLivenessRegistry LivenessRegistry { get; }
    public LivenessMonitor LivenessMonitor { get; }
    public DatabaseRepository? Database { get; }
    public RuleEngine RuleEngine { get; }
    private readonly ConcurrentDictionary<string, ClientConnection> _adminSubscribers = new();

    internal sealed class ConnectionRuntime
    {
        public required ClientConnection Connection { get; init; }
        public Task? ProcessTask { get; set; }
    }

    public CentralServer(
        string expectedToken,
        IPAddress? ipAddress = null,
        int port = 0,
        Action<string>? logger = null,
        TimeSpan? livenessCheckInterval = null,
        TimeSpan? offlineThreshold = null,
        DatabaseRepository? database = null,
        TimeSpan? incidentCooldown = null)
    {
        _expectedToken = expectedToken ?? throw new ArgumentNullException(nameof(expectedToken));
        _ipAddress = ipAddress ?? IPAddress.Loopback;
        _configuredPort = port;
        _logger = logger;
        Database = database;

        RuleEngine = new RuleEngine(logger, incidentCooldown);
        RuleEngine.OnIncidentTriggered += async (evt) =>
        {
            if (Database != null)
            {
                var incident = await Database.OpenOrUpdateIncidentAsync(evt.DeviceId, evt.RuleType, evt.Severity, evt.TriggerValue).ConfigureAwait(false);
                if (incident != null)
                {
                    await BroadcastIncidentOpenAsync(new LabGuard.Protocol.Dtos.IncidentOpenPayload
                    {
                        IncidentId = incident.IncidentId,
                        DeviceId = incident.DeviceId,
                        RuleName = incident.RuleName,
                        ConditionState = incident.ConditionState,
                        WorkflowState = incident.WorkflowState,
                        OpenedAtUtc = incident.OpenedAtUtc,
                        RowVersionHex = incident.RowVersionHex
                    }).ConfigureAwait(false);
                }
            }
        };

        RuleEngine.OnIncidentRecovered += async (evt) =>
        {
            if (Database != null)
            {
                var incident = await Database.RecoverIncidentAsync(evt.DeviceId, evt.RuleType).ConfigureAwait(false);
                if (incident != null)
                {
                    await BroadcastIncidentUpdateAsync(new LabGuard.Protocol.Dtos.IncidentUpdatePayload
                    {
                        IncidentId = incident.IncidentId,
                        DeviceId = incident.DeviceId,
                        ConditionState = incident.ConditionState,
                        WorkflowState = incident.WorkflowState,
                        UpdatedAtUtc = DateTime.UtcNow.ToString("O"),
                        RowVersionHex = incident.RowVersionHex
                    }).ConfigureAwait(false);
                }
            }
        };

        LivenessRegistry = new MonotonicLivenessRegistry(logger);
        LivenessMonitor = new LivenessMonitor(
            LivenessRegistry,
            livenessCheckInterval ?? TimeSpan.FromSeconds(2.5),
            offlineThreshold ?? TimeSpan.FromSeconds(20.0),
            logger);

        LivenessRegistry.OnStatusChanged += (deviceId, oldStatus, newStatus) =>
        {
            string statusStr = newStatus.ToString().ToUpperInvariant();
            if (Database != null)
            {
                _ = Database.UpdateDeviceStatusAsync(deviceId, statusStr);
            }
            _ = BroadcastDeviceUpdateAsync(new LabGuard.Protocol.Dtos.DeviceUpdatePayload
            {
                DeviceId = deviceId,
                Status = statusStr,
                UpdatedAtUtc = DateTime.UtcNow.ToString("O")
            });

            // Evaluate DEVICE_OFFLINE rule
            RuleEngine.ProcessLivenessStatus(deviceId, newStatus);
        };
    }

    public void RegisterAdminSubscriber(ClientConnection conn)
    {
        _adminSubscribers[conn.ConnectionId] = conn;
    }

    public void UnregisterAdminSubscriber(string connectionId)
    {
        _adminSubscribers.TryRemove(connectionId, out _);
    }

    public async Task BroadcastDeviceUpdateAsync(LabGuard.Protocol.Dtos.DeviceUpdatePayload update)
    {
        foreach (var admin in _adminSubscribers.Values)
        {
            try
            {
                await admin.SendEnvelopeAsync(MessageTypes.DeviceUpdate, Guid.NewGuid().ToString("D"), update).ConfigureAwait(false);
            }
            catch
            {
                // Ignore disconnected admin
            }
        }
    }

    public async Task BroadcastIncidentOpenAsync(LabGuard.Protocol.Dtos.IncidentOpenPayload payload)
    {
        foreach (var admin in _adminSubscribers.Values)
        {
            try
            {
                await admin.SendEnvelopeAsync(MessageTypes.IncidentOpen, Guid.NewGuid().ToString("D"), payload).ConfigureAwait(false);
            }
            catch
            {
                // Ignore disconnected admin
            }
        }
    }

    public async Task BroadcastIncidentUpdateAsync(LabGuard.Protocol.Dtos.IncidentUpdatePayload payload)
    {
        foreach (var admin in _adminSubscribers.Values)
        {
            try
            {
                await admin.SendEnvelopeAsync(MessageTypes.IncidentUpdate, Guid.NewGuid().ToString("D"), payload).ConfigureAwait(false);
            }
            catch
            {
                // Ignore disconnected admin
            }
        }
    }

    private void Log(string message)
    {
        string timestamp = DateTime.UtcNow.ToString("O");
        _logger?.Invoke($"[{timestamp}] [CentralServer:{Port}] {message}");
    }

    public void Start()
    {
        if (IsRunning)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _listener = new TcpListener(_ipAddress, _configuredPort);
        _listener.Start();

        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        Log($"Central Server listening on {_ipAddress}:{Port}.");

        // Start liveness scanner
        LivenessMonitor.Start();

        // Accept loop is async I/O and starts directly without extra Task.Run
        _acceptTask = AcceptLoopAsync(_cts.Token);
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && _listener != null)
            {
                TcpClient tcpClient = await _listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);
                var connection = new ClientConnection(tcpClient, _expectedToken, LivenessRegistry, Database, this, _logger);
                var runtime = new ConnectionRuntime { Connection = connection };

                // Connection is registered in active dictionary BEFORE processing begins to prevent race
                _activeConnections.TryAdd(connection.ConnectionId, runtime);
                runtime.ProcessTask = ProcessConnectionAsync(connection, ct);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on server shutdown
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted || _listener == null)
        {
            // Expected when listener is stopped
        }
        catch (Exception ex)
        {
            Log($"Unhandled error in accept loop: {ex}");
        }
    }

    private async Task ProcessConnectionAsync(ClientConnection connection, CancellationToken ct)
    {
        try
        {
            await connection.ProcessAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log($"Unhandled exception in client connection {connection.ConnectionId}: {ex.Message}");
        }
        finally
        {
            UnregisterAdminSubscriber(connection.ConnectionId);
            _activeConnections.TryRemove(connection.ConnectionId, out _);
            await connection.DisposeAsync().ConfigureAwait(false);
        }
    }

    public async Task StopAsync()
    {
        if (!IsRunning)
        {
            return;
        }

        Log("Stopping Central Server...");

        // 0. Stop liveness monitor scanner
        await LivenessMonitor.StopAsync().ConfigureAwait(false);

        // 1. Cancel server CTS
        _cts?.Cancel();

        // 2. Stop listener so no new connections are accepted
        try
        {
            _listener?.Stop();
        }
        catch
        {
            // Ignore socket stop errors
        }
        _listener = null;

        // 3. Await accept loop termination
        if (_acceptTask != null)
        {
            try
            {
                await _acceptTask.ConfigureAwait(false);
            }
            catch
            {
                // Ignore cancel exceptions
            }
        }

        // 4. Snapshot runtimes and close active sockets/connections to unblock read loops
        var currentRuntimes = _activeConnections.Values.ToArray();
        foreach (var runtime in currentRuntimes)
        {
            await runtime.Connection.DisposeAsync().ConfigureAwait(false);
        }

        // 5. Await all tracked connection tasks
        var connectionTasks = currentRuntimes.Select(r => r.ProcessTask).Where(t => t != null).Cast<Task>().ToArray();
        if (connectionTasks.Length > 0)
        {
            try
            {
                await Task.WhenAll(connectionTasks).ConfigureAwait(false);
            }
            catch
            {
                // Exceptions in individual connections are handled in ProcessConnectionAsync
            }
        }

        // 6. Clear dictionary
        _activeConnections.Clear();

        // 7. Dispose resources
        _cts?.Dispose();
        _cts = null;

        Log("Central Server stopped.");
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }
}
