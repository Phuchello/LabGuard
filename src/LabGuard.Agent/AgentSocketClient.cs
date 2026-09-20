using System.Net.Sockets;
using System.Text.Json;
using LabGuard.Protocol;
using LabGuard.Protocol.Dtos;
using LabGuard.Protocol.Serialization;

namespace LabGuard.Agent;

/// <summary>
/// Minimal TCP Agent client spike for Gate 3.
/// Connects via real TCP socket to Central Server and executes the HELLO -> AUTH handshake.
/// </summary>
public class AgentSocketClient : IAsyncDisposable
{
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private string? _deviceId;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly SemaphoreSlim _duplexLock = new(1, 1);
    private readonly Action<string>? _logger;
    private HeartbeatService? _heartbeatService;
    private MetricsService? _metricsService;
    private ServiceMonitor? _serviceMonitor;
    private TaskCompletionSource _disconnectTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public bool IsConnected => _tcpClient?.Connected ?? false;
    public string? SessionId { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(SessionId);
    public ReplayBuffer ReplayBuffer { get; }

    public AgentSocketClient(Action<string>? logger = null, ReplayBuffer? replayBuffer = null)
    {
        _logger = logger;
        ReplayBuffer = replayBuffer ?? new ReplayBuffer(_logger);
    }

    public void NotifyConnectionLost(string reason)
    {
        Log($"Connection lost notice: {reason}");
        _disconnectTcs.TrySetResult();
    }

    public Task WaitForDisconnectAsync(CancellationToken ct = default)
    {
        return _disconnectTcs.Task.WaitAsync(ct);
    }

    private void Log(string message)
    {
        string timestamp = DateTime.UtcNow.ToString("O");
        _logger?.Invoke($"[{timestamp}] [AgentSocketClient] {message}");
    }

    public async Task ConnectAsync(string host, int port, CancellationToken ct = default)
    {
        _disconnectTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Log($"Connecting to {host}:{port}...");
        _tcpClient = new TcpClient();
        await _tcpClient.ConnectAsync(host, port, ct).ConfigureAwait(false);
        _stream = _tcpClient.GetStream();
        Log($"Connected to {host}:{port}.");
    }

    /// <summary>
    /// Sends HELLO_AGENT and awaits correlated HELLO_ACK response.
    /// </summary>
    public async Task<HelloAckPayload> SendHelloAsync(string deviceId, CancellationToken ct = default)
    {
        EnsureConnected();

        _deviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
        string requestId = Guid.NewGuid().ToString("D");
        var helloPayload = new HelloAgentPayload
        {
            AgentVersion = "1.0.0",
            Hostname = Environment.MachineName,
            OsVersion = Environment.OSVersion.ToString(),
            MacAddress = "00:1A:2B:3C:4D:5E"
        };

        var envelope = WireEnvelope<HelloAgentPayload>.Create(
            MessageTypes.HelloAgent,
            _deviceId,
            helloPayload,
            requestId);

        Log($"Sending HELLO_AGENT requestId={requestId} deviceId={_deviceId}");
        await WriteEnvelopeAsync(envelope, ct).ConfigureAwait(false);

        // Await response frame
        var responseEnvelope = await ReadResponseEnvelopeAsync(ct).ConfigureAwait(false);

        if (responseEnvelope.MessageType != MessageTypes.HelloAck)
        {
            throw new InvalidOperationException($"Expected HELLO_ACK but received '{responseEnvelope.MessageType}'.");
        }

        if (responseEnvelope.RequestId != requestId)
        {
            throw new InvalidOperationException($"Correlation mismatch: expected requestId '{requestId}' but got '{responseEnvelope.RequestId}'.");
        }

        var ack = ProtocolSerializer.ExtractPayload<HelloAckPayload>(responseEnvelope.Payload)
            ?? throw new InvalidOperationException("Failed to deserialize HELLO_ACK payload.");

        Log($"Received HELLO_ACK from serverVersion={ack.ServerVersion} serverTimeUtc={ack.ServerTimeUtc}");
        return ack;
    }

    /// <summary>
    /// Sends AUTH_AGENT and awaits correlated AUTH_RESULT response.
    /// </summary>
    public async Task<AuthResultPayload> SendAuthAsync(string deviceId, string preSharedToken, CancellationToken ct = default)
    {
        EnsureConnected();

        string requestId = Guid.NewGuid().ToString("D");
        var authPayload = new AuthAgentPayload
        {
            PreSharedToken = preSharedToken,
            ClientNonce = Guid.NewGuid().ToString("N")
        };

        var envelope = WireEnvelope<AuthAgentPayload>.Create(
            MessageTypes.AuthAgent,
            deviceId,
            authPayload,
            requestId);

        Log($"Sending AUTH_AGENT requestId={requestId} deviceId={deviceId}");
        await WriteEnvelopeAsync(envelope, ct).ConfigureAwait(false);

        // Await response frame
        var responseEnvelope = await ReadResponseEnvelopeAsync(ct).ConfigureAwait(false);

        if (responseEnvelope.MessageType != MessageTypes.AuthResult)
        {
            throw new InvalidOperationException($"Expected AUTH_RESULT but received '{responseEnvelope.MessageType}'.");
        }

        if (responseEnvelope.RequestId != requestId)
        {
            throw new InvalidOperationException($"Correlation mismatch: expected requestId '{requestId}' but got '{responseEnvelope.RequestId}'.");
        }

        var result = ProtocolSerializer.ExtractPayload<AuthResultPayload>(responseEnvelope.Payload)
            ?? throw new InvalidOperationException("Failed to deserialize AUTH_RESULT payload.");

        if (result.Success)
        {
            SessionId = result.SessionId;
            Log($"Received AUTH_RESULT: SUCCESS, sessionId={SessionId}");
        }
        else
        {
            Log($"Received AUTH_RESULT: FAILED, error={result.ErrorMessage}");
        }

        return result;
    }

    /// <summary>
    /// Sends HEARTBEAT and awaits correlated HEARTBEAT_ACK response.
    /// </summary>
    public async Task<HeartbeatAckPayload> SendHeartbeatAsync(long sequenceNumber, long uptimeSeconds, CancellationToken ct = default)
    {
        EnsureConnected();
        if (!IsAuthenticated || string.IsNullOrEmpty(_deviceId))
        {
            throw new InvalidOperationException("Agent client must be authenticated before sending heartbeats.");
        }

        string requestId = Guid.NewGuid().ToString("D");
        var heartbeatPayload = new HeartbeatPayload
        {
            SequenceNumber = sequenceNumber,
            UptimeSeconds = uptimeSeconds
        };

        var envelope = WireEnvelope<HeartbeatPayload>.Create(
            MessageTypes.Heartbeat,
            _deviceId,
            heartbeatPayload,
            requestId);

        await _duplexLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await WriteEnvelopeAsync(envelope, ct).ConfigureAwait(false);

            // Await correlated HEARTBEAT_ACK
            var responseEnvelope = await ReadResponseEnvelopeAsync(ct).ConfigureAwait(false);

            if (responseEnvelope.MessageType != MessageTypes.HeartbeatAck)
            {
                throw new InvalidOperationException($"Expected HEARTBEAT_ACK but received '{responseEnvelope.MessageType}'.");
            }

            if (responseEnvelope.RequestId != requestId)
            {
                throw new InvalidOperationException($"Correlation mismatch: expected requestId '{requestId}' but got '{responseEnvelope.RequestId}'.");
            }

            var ack = ProtocolSerializer.ExtractPayload<HeartbeatAckPayload>(responseEnvelope.Payload)
                ?? throw new InvalidOperationException("Failed to deserialize HEARTBEAT_ACK payload.");

            return ack;
        }
        finally
        {
            _duplexLock.Release();
        }
    }

    /// <summary>
    /// Sends METRIC_BATCH and awaits correlated BATCH_ACK response with bounded timeout (default 5.0s).
    /// Releases the duplex lock on timeout to ensure Heartbeat can proceed unimpeded.
    /// </summary>
    public async Task<BatchAckPayload> SendMetricBatchAsync(MetricBatchPayload batch, TimeSpan? timeout = null, CancellationToken ct = default)
    {
        EnsureConnected();
        if (!IsAuthenticated || string.IsNullOrEmpty(_deviceId))
        {
            throw new InvalidOperationException("Agent client must be authenticated before sending telemetry batches.");
        }

        string requestId = Guid.NewGuid().ToString("D");
        var envelope = WireEnvelope<MetricBatchPayload>.Create(
            MessageTypes.MetricBatch,
            _deviceId,
            batch,
            requestId);

        TimeSpan effectiveTimeout = timeout ?? TimeSpan.FromSeconds(5.0);

        await _duplexLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await WriteEnvelopeAsync(envelope, ct).ConfigureAwait(false);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(effectiveTimeout);

            WireEnvelope<JsonElement> responseEnvelope;
            try
            {
                responseEnvelope = await ReadResponseEnvelopeAsync(timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                Log($"BATCH_ACK_TIMEOUT for batchId={batch.BatchId} after {effectiveTimeout.TotalSeconds:F1}s. Releasing duplex lock.");
                throw new TimeoutException($"BATCH_ACK_TIMEOUT: server did not return BATCH_ACK within {effectiveTimeout.TotalSeconds:F1}s.");
            }

            if (responseEnvelope.MessageType != MessageTypes.BatchAck)
            {
                throw new InvalidOperationException($"Expected BATCH_ACK but received '{responseEnvelope.MessageType}'.");
            }

            if (responseEnvelope.RequestId != requestId)
            {
                throw new InvalidOperationException($"Correlation mismatch: expected requestId '{requestId}' but got '{responseEnvelope.RequestId}'.");
            }

            var ack = ProtocolSerializer.ExtractPayload<BatchAckPayload>(responseEnvelope.Payload)
                ?? throw new InvalidOperationException("Failed to deserialize BATCH_ACK payload.");

            return ack;
        }
        finally
        {
            _duplexLock.Release();
        }
    }

    /// <summary>
    /// Sends latest-state SERVICE_STATUS message to Central Server.
    /// Unidirectional telemetry, does not require ACK.
    /// </summary>
    public async Task SendServiceStatusAsync(ServiceStatusPayload payload, CancellationToken ct = default)
    {
        EnsureConnected();
        if (!IsAuthenticated || string.IsNullOrEmpty(_deviceId))
        {
            throw new InvalidOperationException("Agent client must be authenticated before sending service status.");
        }

        string requestId = Guid.NewGuid().ToString("D");
        var envelope = WireEnvelope<ServiceStatusPayload>.Create(
            MessageTypes.ServiceStatus,
            _deviceId,
            payload,
            requestId);

        await WriteEnvelopeAsync(envelope, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Flushes unacknowledged telemetry batches stored in ReplayBuffer.
    /// Retries batches marked with IsReplay = true, preserving original BootId and BatchId.
    /// </summary>
    public async Task<int> ReplayPendingBatchesAsync(CancellationToken ct = default)
    {
        var pending = ReplayBuffer.GetPendingReplays();
        if (pending.Count == 0)
        {
            return 0;
        }

        Log($"Flushing {pending.Count} buffered telemetry batches...");
        int replayedCount = 0;
        foreach (var batch in pending)
        {
            if (ct.IsCancellationRequested || !IsConnected || !IsAuthenticated)
            {
                break;
            }

            try
            {
                var ack = await SendMetricBatchAsync(batch, TimeSpan.FromSeconds(5.0), ct).ConfigureAwait(false);
                if (ack.Accepted)
                {
                    ReplayBuffer.Acknowledge(batch.BatchId);
                    replayedCount++;
                }
            }
            catch (Exception ex)
            {
                Log($"Failed to replay batchId={batch.BatchId}: {ex.Message}");
                break; // Stop replaying if connection dropped
            }
        }

        Log($"Replayed {replayedCount} batches successfully.");
        return replayedCount;
    }

    /// <summary>
    /// Starts background periodic heartbeat loop using PeriodicTimer.
    /// </summary>
    public void StartHeartbeat(TimeSpan? interval = null)
    {
        _heartbeatService?.DisposeAsync();
        _heartbeatService = new HeartbeatService(this, interval, _logger);
        _heartbeatService.Start();
    }

    /// <summary>
    /// Stops background periodic heartbeat loop.
    /// </summary>
    public async Task StopHeartbeatAsync()
    {
        if (_heartbeatService != null)
        {
            await _heartbeatService.StopAsync().ConfigureAwait(false);
            _heartbeatService = null;
        }
    }

    /// <summary>
    /// Starts background periodic telemetry collection loop using PeriodicTimer.
    /// </summary>
    public void StartMetrics(TimeSpan? interval = null, MetricsCollector? collector = null)
    {
        _metricsService?.DisposeAsync();
        _metricsService = new MetricsService(this, interval, _logger, collector);
        _metricsService.Start();
    }

    /// <summary>
    /// Stops background periodic telemetry collection loop.
    /// </summary>
    public async Task StopMetricsAsync()
    {
        if (_metricsService != null)
        {
            await _metricsService.StopAsync().ConfigureAwait(false);
            _metricsService = null;
        }
    }

    /// <summary>
    /// Starts background TCP service monitoring loop.
    /// </summary>
    public void StartServiceMonitoring(IEnumerable<ServiceTarget>? targets = null, TimeSpan? interval = null, TimeSpan? probeTimeout = null)
    {
        _serviceMonitor?.DisposeAsync();
        _serviceMonitor = new ServiceMonitor(this, targets, interval, probeTimeout, _logger);
        _serviceMonitor.Start();
    }

    /// <summary>
    /// Stops background TCP service monitoring loop.
    /// </summary>
    public async Task StopServiceMonitoringAsync()
    {
        if (_serviceMonitor != null)
        {
            await _serviceMonitor.StopAsync().ConfigureAwait(false);
            _serviceMonitor = null;
        }
    }

    /// <summary>
    /// Computes exponential backoff delay with uniform jitter:
    /// Delays: 1s, 2s, 4s, 8s, 16s, max 30s.
    /// Jitter: +-10% (multiplier in range [0.90, 1.10]).
    /// </summary>
    public static double CalculateBackoffDelaySeconds(int attempt, double? randomFraction = null)
    {
        int k = Math.Max(0, attempt);
        double baseSeconds = Math.Min(30.0, Math.Pow(2, k));
        double fraction = randomFraction.HasValue ? Math.Clamp(randomFraction.Value, 0.0, 1.0) : Random.Shared.NextDouble();
        double jitterMultiplier = 0.90 + (0.20 * fraction); // [0.90, 1.10]
        return baseSeconds * jitterMultiplier;
    }

    /// <summary>
    /// Reads a single response envelope from the wire.
    /// </summary>
    public async Task<WireEnvelope<JsonElement>> ReadResponseEnvelopeAsync(CancellationToken ct = default)
    {
        EnsureConnected();

        byte[]? frameBytes;
        try
        {
            frameBytes = await FrameCodec.ReadFrameAsync(_stream!, FrameCodec.MaxPayloadSize, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            NotifyConnectionLost(ex.Message);
            throw;
        }

        if (frameBytes == null)
        {
            NotifyConnectionLost("Server closed connection cleanly.");
            throw new EndOfStreamException("Server closed connection while waiting for response frame.");
        }

        var (validation, envelope) = EnvelopeValidator.ValidateAndParse(frameBytes);
        if (!validation.IsValid || envelope == null)
        {
            throw new InvalidDataException($"Received invalid envelope from server: [{validation.ErrorCode}] {validation.ErrorMessage}");
        }

        if (envelope.MessageType != MessageTypes.HeartbeatAck)
        {
            Log($"Read response messageType={envelope.MessageType} requestId={envelope.RequestId} size={frameBytes.Length}b");
        }
        return envelope;
    }

    /// <summary>
    /// Writes an envelope through Single Writer lock.
    /// </summary>
    public async Task WriteEnvelopeAsync<T>(WireEnvelope<T> envelope, CancellationToken ct = default)
    {
        EnsureConnected();

        byte[] jsonBytes = ProtocolSerializer.Serialize(envelope);

        await _writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await FrameCodec.WriteFrameAsync(_stream!, jsonBytes, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            NotifyConnectionLost(ex.Message);
            throw;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private void EnsureConnected()
    {
        if (_tcpClient == null || !_tcpClient.Connected || _stream == null)
        {
            NotifyConnectionLost("Agent client is not connected to a server.");
            throw new InvalidOperationException("Agent client is not connected to a server.");
        }
    }

    /// <summary>
    /// Gracefully disconnects by sending AGENT_GOODBYE and awaiting GOODBYE_ACK before socket closure.
    /// </summary>
    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        await StopServiceMonitoringAsync().ConfigureAwait(false);
        await StopMetricsAsync().ConfigureAwait(false);
        await StopHeartbeatAsync().ConfigureAwait(false);

        if (IsConnected && _stream != null)
        {
            try
            {
                string requestId = Guid.NewGuid().ToString("D");
                var goodbye = WireEnvelope<AgentGoodbyePayload>.Create(
                    MessageTypes.AgentGoodbye,
                    _deviceId ?? "AGENT",
                    new AgentGoodbyePayload { Reason = "CLIENT_DISCONNECT", UptimeSeconds = 0 },
                    requestId);

                await WriteEnvelopeAsync(goodbye, ct).ConfigureAwait(false);

                // Await correlated GOODBYE_ACK
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(2));

                var ackEnvelope = await ReadResponseEnvelopeAsync(timeoutCts.Token).ConfigureAwait(false);
                if (ackEnvelope.MessageType == MessageTypes.GoodbyeAck && ackEnvelope.RequestId == requestId)
                {
                    Log($"Received correlated GOODBYE_ACK requestId={ackEnvelope.RequestId}");
                }
                else
                {
                    Log($"GOODBYE_ACK correlation failure: expected type '{MessageTypes.GoodbyeAck}' with requestId '{requestId}', got type '{ackEnvelope.MessageType}' with requestId '{ackEnvelope.RequestId}'.");
                }
            }
            catch (Exception ex)
            {
                Log($"Graceful disconnect notice: {ex.Message}");
            }
        }

        await DisposeAsync().ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        await StopServiceMonitoringAsync().ConfigureAwait(false);
        await StopMetricsAsync().ConfigureAwait(false);
        await StopHeartbeatAsync().ConfigureAwait(false);
        _stream?.Dispose();
        _tcpClient?.Dispose();
        _writeLock.Dispose();
        _duplexLock.Dispose();
        _stream = null;
        _tcpClient = null;
        await Task.CompletedTask;
    }
}
