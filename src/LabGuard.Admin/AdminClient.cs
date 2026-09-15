using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text.Json;
using LabGuard.Protocol;
using LabGuard.Protocol.Dtos;
using LabGuard.Protocol.Serialization;

namespace LabGuard.Admin;

/// <summary>
/// TCP client for the Admin application.
/// Connects to Central Server, performs HELLO_ADMIN and LOGIN handshakes,
/// requests initial device snapshot, and receives realtime DEVICE_UPDATE broadcasts.
/// </summary>
public class AdminClient : IAsyncDisposable
{
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private CancellationTokenSource? _cts;
    private Task? _readerTask;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly Action<string>? _logger;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<CommandResultPayload>> _pendingCommands = new();

    public bool IsConnected => _tcpClient?.Connected ?? false;
    public bool IsAuthenticated { get; private set; }
    public string? SessionToken { get; private set; }
    public string? UserFullName { get; private set; }
    public string? UserRole { get; private set; }

    public event Action<DeviceUpdatePayload>? OnDeviceUpdated;
    public event Action<List<DeviceSummaryDto>>? OnDevicePageReceived;
    public event Action<List<IncidentSummaryDto>>? OnIncidentPageReceived;
    public event Action<IncidentOpenPayload>? OnIncidentOpened;
    public event Action<IncidentUpdatePayload>? OnIncidentUpdated;
    public event Action<string>? OnConnectionStatusChanged;

    public AdminClient(Action<string>? logger = null)
    {
        _logger = logger;
    }

    private void Log(string message)
    {
        _logger?.Invoke($"[AdminClient] {message}");
    }

    public async Task<bool> ConnectAndLoginAsync(
        string host = "127.0.0.1",
        int port = 5000,
        string username = "admin",
        string password = "Admin@123",
        CancellationToken ct = default)
    {
        try
        {
            OnConnectionStatusChanged?.Invoke($"Connecting to {host}:{port}...");
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(host, port, ct).ConfigureAwait(false);
            _stream = _tcpClient.GetStream();
            OnConnectionStatusChanged?.Invoke("Connected. Greeting server...");

            // 1. Send HELLO_ADMIN
            string helloReqId = Guid.NewGuid().ToString("D");
            var helloEnvelope = WireEnvelope<HelloAdminPayload>.Create(
                MessageTypes.HelloAdmin,
                "ADMIN-CONSOLE",
                new HelloAdminPayload { ClientVersion = "1.0.0", Hostname = Environment.MachineName },
                helloReqId);

            await WriteEnvelopeAsync(helloEnvelope, ct).ConfigureAwait(false);
            var helloAckEnv = await ReadResponseEnvelopeAsync(ct).ConfigureAwait(false);
            if (helloAckEnv.MessageType != MessageTypes.HelloAck)
            {
                OnConnectionStatusChanged?.Invoke($"HELLO failed: expected HELLO_ACK, got {helloAckEnv.MessageType}");
                return false;
            }

            // 2. Send LOGIN
            OnConnectionStatusChanged?.Invoke("Authenticating admin user...");
            string loginReqId = Guid.NewGuid().ToString("D");
            var loginEnvelope = WireEnvelope<LoginPayload>.Create(
                MessageTypes.Login,
                "ADMIN-CONSOLE",
                new LoginPayload { Username = username, Password = password },
                loginReqId);

            await WriteEnvelopeAsync(loginEnvelope, ct).ConfigureAwait(false);
            var loginResultEnv = await ReadResponseEnvelopeAsync(ct).ConfigureAwait(false);
            if (loginResultEnv.MessageType != MessageTypes.LoginResult)
            {
                OnConnectionStatusChanged?.Invoke($"LOGIN failed: expected LOGIN_RESULT, got {loginResultEnv.MessageType}");
                return false;
            }

            var loginResult = ProtocolSerializer.ExtractPayload<LoginResultPayload>(loginResultEnv.Payload);
            if (loginResult == null || !loginResult.Success)
            {
                OnConnectionStatusChanged?.Invoke($"Login failed: {loginResult?.ErrorMessage ?? "Access denied"}");
                return false;
            }

            IsAuthenticated = true;
            SessionToken = loginResult.SessionToken;
            UserFullName = loginResult.FullName;
            UserRole = loginResult.Role;

            OnConnectionStatusChanged?.Invoke($"Logged in as {UserFullName} ({UserRole})");

            // 3. Start background reader loop for broadcast events
            _cts = new CancellationTokenSource();
            _readerTask = RunReaderLoopAsync(_cts.Token);

            // 4. Request initial snapshots
            await RequestDeviceListAsync().ConfigureAwait(false);
            await RequestIncidentListAsync().ConfigureAwait(false);

            return true;
        }
        catch (Exception ex)
        {
            OnConnectionStatusChanged?.Invoke($"Connection error: {ex.Message}");
            return false;
        }
    }

    public async Task RequestDeviceListAsync(CancellationToken ct = default)
    {
        if (!IsConnected || _stream == null)
        {
            return;
        }

        var req = WireEnvelope<ListDevicesPayload>.Create(
            MessageTypes.ListDevices,
            "ADMIN-CONSOLE",
            new ListDevicesPayload { PageNumber = 1, PageSize = 100 },
            Guid.NewGuid().ToString("D"));

        await WriteEnvelopeAsync(req, ct).ConfigureAwait(false);
    }

    public async Task RequestIncidentListAsync(CancellationToken ct = default)
    {
        if (!IsConnected || _stream == null)
        {
            return;
        }

        var req = WireEnvelope<ListIncidentsPayload>.Create(
            MessageTypes.ListIncidents,
            "ADMIN-CONSOLE",
            new ListIncidentsPayload { PageNumber = 1, PageSize = 100 },
            Guid.NewGuid().ToString("D"));

        await WriteEnvelopeAsync(req, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends an incident lifecycle command (ACK, ASSIGN, START, RESOLVE) and awaits correlated COMMAND_RESULT.
    /// </summary>
    public async Task<CommandResultPayload> SendIncidentCommandAsync(
        string commandType,
        long incidentId,
        string expectedRowVersion,
        Dictionary<string, object?>? parameters = null,
        CancellationToken ct = default)
    {
        if (!IsConnected || _stream == null)
        {
            return new CommandResultPayload
            {
                Success = false,
                StatusCode = 503,
                ErrorCode = "ERR_NOT_CONNECTED",
                ErrorMessage = "Not connected to Central Server."
            };
        }

        string requestId = Guid.NewGuid().ToString("D");
        string commandId = Guid.NewGuid().ToString("D");

        var payload = new IncidentCommandPayload
        {
            CommandId = commandId,
            CommandType = commandType,
            IncidentId = incidentId,
            ExpectedRowVersion = expectedRowVersion,
            CommandParams = parameters ?? []
        };

        var envelope = WireEnvelope<IncidentCommandPayload>.Create(
            commandType,
            "ADMIN-CONSOLE",
            payload,
            requestId);

        var tcs = new TaskCompletionSource<CommandResultPayload>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingCommands[requestId] = tcs;

        try
        {
            await WriteEnvelopeAsync(envelope, ct).ConfigureAwait(false);

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
            using var reg = linkedCts.Token.Register(() => tcs.TrySetCanceled());

            return await tcs.Task.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return new CommandResultPayload
            {
                CommandId = commandId,
                Success = false,
                StatusCode = 408,
                ErrorCode = "ERR_TIMEOUT",
                ErrorMessage = "Timeout waiting for server response."
            };
        }
        finally
        {
            _pendingCommands.TryRemove(requestId, out _);
        }
    }

    private async Task RunReaderLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && IsConnected && _stream != null)
            {
                byte[]? frameBytes = await FrameCodec.ReadFrameAsync(_stream, FrameCodec.MaxPayloadSize, ct).ConfigureAwait(false);
                if (frameBytes == null)
                {
                    OnConnectionStatusChanged?.Invoke("Connection closed by server.");
                    break;
                }

                var envelope = ProtocolSerializer.DeserializeEnvelope(frameBytes);
                if (envelope == null)
                {
                    continue;
                }

                if (envelope.MessageType == MessageTypes.DevicePage)
                {
                    var page = ProtocolSerializer.ExtractPayload<DevicePagePayload>(envelope.Payload);
                    if (page != null)
                    {
                        OnDevicePageReceived?.Invoke(page.Devices);
                    }
                }
                else if (envelope.MessageType == MessageTypes.DeviceUpdate)
                {
                    var update = ProtocolSerializer.ExtractPayload<DeviceUpdatePayload>(envelope.Payload);
                    if (update != null)
                    {
                        OnDeviceUpdated?.Invoke(update);
                    }
                }
                else if (envelope.MessageType == MessageTypes.IncidentPage)
                {
                    var page = ProtocolSerializer.ExtractPayload<IncidentPagePayload>(envelope.Payload);
                    if (page != null)
                    {
                        OnIncidentPageReceived?.Invoke(page.Incidents);
                    }
                }
                else if (envelope.MessageType == MessageTypes.IncidentOpen)
                {
                    var open = ProtocolSerializer.ExtractPayload<IncidentOpenPayload>(envelope.Payload);
                    if (open != null)
                    {
                        OnIncidentOpened?.Invoke(open);
                    }
                }
                else if (envelope.MessageType == MessageTypes.IncidentUpdate)
                {
                    var update = ProtocolSerializer.ExtractPayload<IncidentUpdatePayload>(envelope.Payload);
                    if (update != null)
                    {
                        OnIncidentUpdated?.Invoke(update);
                    }
                }
                else if (envelope.MessageType == MessageTypes.CommandResult)
                {
                    var result = ProtocolSerializer.ExtractPayload<CommandResultPayload>(envelope.Payload);
                    if (result != null)
                    {
                        if (_pendingCommands.TryRemove(envelope.RequestId, out var tcs))
                        {
                            tcs.TrySetResult(result);
                        }
                        else if (!string.IsNullOrWhiteSpace(result.CommandId) && _pendingCommands.TryRemove(result.CommandId, out var cmdTcs))
                        {
                            cmdTcs.TrySetResult(result);
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Clean shutdown
        }
        catch (Exception ex)
        {
            OnConnectionStatusChanged?.Invoke($"Reader disconnected: {ex.Message}");
        }
    }

    private async Task<WireEnvelope<JsonElement>> ReadResponseEnvelopeAsync(CancellationToken ct)
    {
        if (_stream == null)
        {
            throw new InvalidOperationException("Not connected.");
        }

        byte[]? bytes = await FrameCodec.ReadFrameAsync(_stream, FrameCodec.MaxPayloadSize, ct).ConfigureAwait(false);
        if (bytes == null)
        {
            throw new EndOfStreamException("Server closed connection.");
        }

        return ProtocolSerializer.DeserializeEnvelope(bytes)
            ?? throw new InvalidOperationException("Failed to deserialize envelope.");
    }

    private async Task WriteEnvelopeAsync<T>(WireEnvelope<T> envelope, CancellationToken ct)
    {
        if (_stream == null)
        {
            throw new InvalidOperationException("Not connected.");
        }

        byte[] json = ProtocolSerializer.Serialize(envelope);
        await _writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await FrameCodec.WriteFrameAsync(_stream, json, ct).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        foreach (var pending in _pendingCommands.Values)
        {
            pending.TrySetCanceled();
        }
        _pendingCommands.Clear();
        if (_readerTask != null)
        {
            try { await _readerTask.ConfigureAwait(false); } catch { }
        }
        _cts?.Dispose();
        _stream?.Dispose();
        _tcpClient?.Dispose();
        _writeLock.Dispose();
        _stream = null;
        _tcpClient = null;
    }
}
