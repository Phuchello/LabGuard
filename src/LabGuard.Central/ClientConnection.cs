using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LabGuard.Protocol;
using LabGuard.Protocol.Dtos;
using LabGuard.Protocol.Serialization;

namespace LabGuard.Central;

public enum ConnectionState
{
    AwaitingHello,
    AwaitingAuth,
    Authenticated,
    Closed
}

/// <summary>
/// Manages a single active TCP client connection to the Central Server.
/// Handles length-prefixed frame decoding, strict envelope validation,
/// handshake state machine transitions (HELLO -> AUTH), identity binding,
/// and Single Writer discipline for outgoing socket stream writes.
/// </summary>
public class ClientConnection : IAsyncDisposable
{
    private readonly TcpClient _tcpClient;
    private readonly NetworkStream _stream;
    private readonly string _expectedToken;
    private readonly MonotonicLivenessRegistry _livenessRegistry;
    private readonly DatabaseRepository? _database;
    private readonly CentralServer? _server;
    private readonly Action<string>? _logger;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public string ConnectionId { get; } = Guid.NewGuid().ToString("D");
    public string? SessionId { get; private set; }
    public string? DeviceId { get; private set; }
    public int? UserId { get; private set; }
    public long ConnectionGeneration { get; set; }
    public ConnectionState State { get; private set; } = ConnectionState.AwaitingHello;
    public bool IsAdmin { get; private set; }
    public string RemoteEndpoint { get; }

    public ClientConnection(
        TcpClient tcpClient,
        string expectedToken,
        MonotonicLivenessRegistry? livenessRegistry = null,
        DatabaseRepository? database = null,
        CentralServer? server = null,
        Action<string>? logger = null)
    {
        _tcpClient = tcpClient ?? throw new ArgumentNullException(nameof(tcpClient));
        _stream = _tcpClient.GetStream();
        _expectedToken = expectedToken ?? throw new ArgumentNullException(nameof(expectedToken));
        _livenessRegistry = livenessRegistry ?? new MonotonicLivenessRegistry(logger);
        _database = database;
        _server = server;
        _logger = logger;
        RemoteEndpoint = _tcpClient.Client.RemoteEndPoint?.ToString() ?? "unknown";
    }

    public ClientConnection(
        TcpClient tcpClient,
        string expectedToken,
        MonotonicLivenessRegistry? livenessRegistry,
        Action<string>? logger)
        : this(tcpClient, expectedToken, livenessRegistry, null, null, logger)
    {
    }

    public ClientConnection(TcpClient tcpClient, string expectedToken, Action<string>? logger)
        : this(tcpClient, expectedToken, null, null, null, logger)
    {
    }

    private void Log(string message)
    {
        string timestamp = DateTime.UtcNow.ToString("O");
        _logger?.Invoke($"[{timestamp}] [Conn:{ConnectionId}] [{RemoteEndpoint}] {message}");
    }

    /// <summary>
    /// Processes incoming frames over the real TCP connection until closed or disconnected.
    /// </summary>
    public async Task ProcessAsync(CancellationToken ct = default)
    {
        Log("Client connected.");

        try
        {
            while (!ct.IsCancellationRequested && State != ConnectionState.Closed)
            {
                byte[]? frameBytes;
                try
                {
                    frameBytes = await FrameCodec.ReadFrameAsync(_stream, FrameCodec.MaxPayloadSize, ct).ConfigureAwait(false);
                }
                catch (InvalidDataException ex)
                {
                    Log($"Frame validation failed: {ex.Message}");
                    string errorCode = ex.Message.Contains("exceeds maximum allowed size", StringComparison.OrdinalIgnoreCase)
                        ? ErrorCodes.FrameTooLarge
                        : ErrorCodes.FrameZeroLength;

                    await SendErrorAsync(Guid.NewGuid().ToString("D"), errorCode, ex.Message, ct).ConfigureAwait(false);
                    break;
                }
                catch (EndOfStreamException)
                {
                    Log("Stream closed prematurely mid-frame by client.");
                    break;
                }

                if (frameBytes == null)
                {
                    Log("Client disconnected cleanly at frame boundary.");
                    break;
                }

                await HandleFrameAsync(frameBytes, ct).ConfigureAwait(false);
            }
        }
        catch (IOException ex)
        {
            Log($"Socket I/O termination: {ex.Message}");
        }
        catch (OperationCanceledException)
        {
            Log("Processing canceled by server shutdown.");
        }
        catch (Exception ex)
        {
            Log($"Unhandled error in connection: {ex}");
        }
        finally
        {
            State = ConnectionState.Closed;
            Log("Connection closed.");
        }
    }

    private async Task HandleFrameAsync(byte[] frameBytes, CancellationToken ct)
    {
        // Strict raw-JSON validation BEFORE normal dispatch/deserialization
        var (validation, envelope) = EnvelopeValidator.ValidateAndParse(frameBytes);
        if (!validation.IsValid)
        {
            Log($"Envelope validation failed: {validation.ErrorMessage}");
            string corrId = (validation.RequestId != null && EnvelopeValidator.IsCanonicalUuidV4(validation.RequestId))
                ? validation.RequestId
                : (envelope != null && EnvelopeValidator.IsCanonicalUuidV4(envelope.RequestId))
                    ? envelope.RequestId
                    : Guid.NewGuid().ToString("D");

            string? msgType = validation.MessageType ?? envelope?.MessageType;

            // Handshake special case: if awaiting auth and client sent AUTH_AGENT with invalid payload
            if (State == ConnectionState.AwaitingAuth &&
                msgType == MessageTypes.AuthAgent &&
                validation.ErrorCode == ErrorCodes.InvalidPayload &&
                validation.ErrorMessage?.Contains("payload must be a JSON object", StringComparison.OrdinalIgnoreCase) == true)
            {
                var invalidFormatResult = new AuthResultPayload
                {
                    Success = false,
                    ErrorMessage = "AUTH_AGENT payload must be a JSON object."
                };
                await SendEnvelopeAsync(MessageTypes.AuthResult, corrId, invalidFormatResult, ct).ConfigureAwait(false);
                State = ConnectionState.Closed;
                return;
            }

            string errorCode = validation.ErrorCode ?? ErrorCodes.InvalidPayload;

            await SendErrorAsync(corrId, errorCode, validation.ErrorMessage ?? "Invalid envelope.", ct).ConfigureAwait(false);
            State = ConnectionState.Closed;
            return;
        }

        if (envelope!.MessageType != MessageTypes.Heartbeat)
        {
            Log($"Received messageType={envelope.MessageType} requestId={envelope.RequestId} payloadSize={frameBytes.Length}b");
        }

        // Handshake state machine
        switch (State)
        {
            case ConnectionState.AwaitingHello:
                if (envelope.MessageType == MessageTypes.HelloAgent)
                {
                    await HandleHelloAgentAsync(envelope, ct).ConfigureAwait(false);
                }
                else if (envelope.MessageType == MessageTypes.HelloAdmin)
                {
                    IsAdmin = true;
                    DeviceId = envelope.DeviceId;
                    var helloAck = new HelloAckPayload
                    {
                        ServerVersion = "1.0.0",
                        Capabilities = ["TLS", "JSON_FRAMING", "OCC"],
                        ServerTimeUtc = DateTime.UtcNow.ToString("O"),
                        HeartbeatIntervalSeconds = 5
                    };
                    Log($"HELLO_ADMIN accepted from {DeviceId}. Sending HELLO_ACK.");
                    await SendEnvelopeAsync(MessageTypes.HelloAck, envelope.RequestId, helloAck, ct).ConfigureAwait(false);
                    State = ConnectionState.AwaitingAuth;
                }
                else
                {
                    Log($"State violation: received {envelope.MessageType} while awaiting HELLO.");
                    await SendErrorAsync(envelope.RequestId, ErrorCodes.Unauthenticated, "Handshake sequence violation: HELLO must be first.", ct).ConfigureAwait(false);
                    State = ConnectionState.Closed;
                }
                break;

            case ConnectionState.AwaitingAuth:
                if (envelope.MessageType == MessageTypes.AuthAgent)
                {
                    await HandleAuthAgentAsync(envelope, ct).ConfigureAwait(false);
                }
                else if (envelope.MessageType == MessageTypes.Login)
                {
                    await HandleAdminLoginAsync(envelope, ct).ConfigureAwait(false);
                }
                else
                {
                    Log($"State violation: received {envelope.MessageType} while awaiting AUTH.");
                    await SendErrorAsync(envelope.RequestId, ErrorCodes.Unauthenticated, "Handshake sequence violation: AUTH expected.", ct).ConfigureAwait(false);
                    State = ConnectionState.Closed;
                }
                break;

            case ConnectionState.Authenticated:
                if (envelope.MessageType == MessageTypes.Heartbeat)
                {
                    await HandleHeartbeatAsync(envelope, ct).ConfigureAwait(false);
                }
                else if (envelope.MessageType == MessageTypes.MetricBatch)
                {
                    await HandleMetricBatchAsync(envelope, ct).ConfigureAwait(false);
                }
                else if (envelope.MessageType == MessageTypes.ServiceStatus)
                {
                    await HandleServiceStatusAsync(envelope, ct).ConfigureAwait(false);
                }
                else if (envelope.MessageType == MessageTypes.ListDevices || envelope.MessageType == MessageTypes.SubscribeDevices)
                {
                    await HandleListDevicesAsync(envelope, ct).ConfigureAwait(false);
                }
                else if (envelope.MessageType == MessageTypes.ListIncidents || envelope.MessageType == MessageTypes.SubscribeIncidents)
                {
                    await HandleListIncidentsAsync(envelope, ct).ConfigureAwait(false);
                }
                else if (envelope.MessageType == MessageTypes.IncidentAck ||
                         envelope.MessageType == MessageTypes.IncidentAssign ||
                         envelope.MessageType == MessageTypes.IncidentStart ||
                         envelope.MessageType == MessageTypes.IncidentResolve)
                {
                    await HandleIncidentCommandAsync(envelope, ct).ConfigureAwait(false);
                }
                else if (envelope.MessageType == MessageTypes.AgentGoodbye)
                {
                    Log("Received AGENT_GOODBYE. Sending GOODBYE_ACK and closing.");
                    await SendEnvelopeAsync(MessageTypes.GoodbyeAck, envelope.RequestId, new GoodbyeAckPayload { Success = true }, ct).ConfigureAwait(false);
                    State = ConnectionState.Closed;
                }
                else
                {
                    _livenessRegistry.RecordActivity(DeviceId!, ConnectionGeneration);
                    Log($"Received message '{envelope.MessageType}' in Authenticated state.");
                }
                break;
        }
    }

    private async Task HandleHelloAgentAsync(WireEnvelope<JsonElement> envelope, CancellationToken ct)
    {
        // Validate HelloAgentPayload
        if (envelope.Payload.ValueKind != JsonValueKind.Object)
        {
            Log("HELLO_AGENT payload is not a valid JSON object.");
            await SendErrorAsync(envelope.RequestId, ErrorCodes.InvalidPayload, "HELLO_AGENT payload must be a JSON object.", ct).ConfigureAwait(false);
            State = ConnectionState.Closed;
            return;
        }

        HelloAgentPayload? helloDto;
        try
        {
            helloDto = ProtocolSerializer.ExtractPayload<HelloAgentPayload>(envelope.Payload);
        }
        catch (JsonException ex)
        {
            Log($"Failed to deserialize HelloAgentPayload: {ex.Message}");
            await SendErrorAsync(envelope.RequestId, ErrorCodes.InvalidPayload, "Invalid HelloAgentPayload format.", ct).ConfigureAwait(false);
            State = ConnectionState.Closed;
            return;
        }

        if (helloDto == null || string.IsNullOrWhiteSpace(helloDto.AgentVersion) || string.IsNullOrWhiteSpace(helloDto.Hostname))
        {
            Log("HELLO_AGENT payload missing mandatory AgentVersion or Hostname.");
            await SendErrorAsync(envelope.RequestId, ErrorCodes.InvalidPayload, "HelloAgentPayload missing required AgentVersion or Hostname.", ct).ConfigureAwait(false);
            State = ConnectionState.Closed;
            return;
        }

        DeviceId = envelope.DeviceId;
        State = ConnectionState.AwaitingAuth;

        var ackPayload = new HelloAckPayload
        {
            ServerVersion = "1.0.0",
            Capabilities = ["TLS", "JSON_FRAMING", "OCC"],
            ServerTimeUtc = DateTime.UtcNow.ToString("O"),
            HeartbeatIntervalSeconds = 5
        };

        Log($"HELLO_AGENT accepted for deviceId={DeviceId}. Sending HELLO_ACK.");
        await SendEnvelopeAsync(MessageTypes.HelloAck, envelope.RequestId, ackPayload, ct).ConfigureAwait(false);
    }

    private async Task HandleAuthAgentAsync(WireEnvelope<JsonElement> envelope, CancellationToken ct)
    {
        // 1. Identity binding: DeviceId in AUTH must match DeviceId in HELLO
        if (!string.Equals(envelope.DeviceId, DeviceId, StringComparison.Ordinal))
        {
            Log($"AUTH_AGENT rejected: envelope deviceId '{envelope.DeviceId}' does not match HELLO deviceId '{DeviceId}'.");
            var mismatchResult = new AuthResultPayload
            {
                Success = false,
                ErrorMessage = "DeviceId mismatch with HELLO handshake."
            };
            await SendEnvelopeAsync(MessageTypes.AuthResult, envelope.RequestId, mismatchResult, ct).ConfigureAwait(false);
            State = ConnectionState.Closed;
            return;
        }

        // 2. Validate payload format
        if (envelope.Payload.ValueKind != JsonValueKind.Object)
        {
            Log("AUTH_AGENT payload is not a valid JSON object.");
            var invalidFormatResult = new AuthResultPayload
            {
                Success = false,
                ErrorMessage = "AUTH_AGENT payload must be a JSON object."
            };
            await SendEnvelopeAsync(MessageTypes.AuthResult, envelope.RequestId, invalidFormatResult, ct).ConfigureAwait(false);
            State = ConnectionState.Closed;
            return;
        }

        AuthAgentPayload? authDto;
        try
        {
            authDto = ProtocolSerializer.ExtractPayload<AuthAgentPayload>(envelope.Payload);
        }
        catch (JsonException ex)
        {
            Log($"Failed to deserialize AuthAgentPayload: {ex.Message}");
            var parseErrorResult = new AuthResultPayload
            {
                Success = false,
                ErrorMessage = "Invalid AuthAgentPayload format."
            };
            await SendEnvelopeAsync(MessageTypes.AuthResult, envelope.RequestId, parseErrorResult, ct).ConfigureAwait(false);
            State = ConnectionState.Closed;
            return;
        }

        string clientToken = authDto?.PreSharedToken ?? string.Empty;
        if (string.IsNullOrWhiteSpace(clientToken))
        {
            Log("AUTH_AGENT missing or empty pre-shared token.");
            var missingTokenResult = new AuthResultPayload
            {
                Success = false,
                ErrorMessage = "Missing or empty pre-shared token."
            };
            await SendEnvelopeAsync(MessageTypes.AuthResult, envelope.RequestId, missingTokenResult, ct).ConfigureAwait(false);
            State = ConnectionState.Closed;
            return;
        }

        // Constant-time token verification to prevent timing attacks
        byte[] expectedBytes = Encoding.UTF8.GetBytes(_expectedToken);
        byte[] providedBytes = Encoding.UTF8.GetBytes(clientToken);
        bool isValid = CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);

        if (isValid)
        {
            SessionId = Guid.NewGuid().ToString("D");
            State = ConnectionState.Authenticated;
            ConnectionGeneration = _livenessRegistry.RegisterOrUpdateDevice(DeviceId!);
            _server?.RuleEngine.ProcessLivenessStatus(DeviceId!, DeviceLivenessStatus.Online);

            var resultPayload = new AuthResultPayload
            {
                Success = true,
                SessionId = SessionId,
                AssignedDeviceId = DeviceId,
                ErrorMessage = null
            };

            Log($"AUTH_AGENT successful for deviceId={DeviceId}. SessionId={SessionId}. ConnectionGeneration={ConnectionGeneration}. Sending AUTH_RESULT(success=true).");
            await SendEnvelopeAsync(MessageTypes.AuthResult, envelope.RequestId, resultPayload, ct).ConfigureAwait(false);
        }
        else
        {
            Log($"AUTH_AGENT failed for deviceId={DeviceId}: invalid pre-shared token.");
            var resultPayload = new AuthResultPayload
            {
                Success = false,
                SessionId = null,
                AssignedDeviceId = null,
                ErrorMessage = "Invalid pre-shared token."
            };

            await SendEnvelopeAsync(MessageTypes.AuthResult, envelope.RequestId, resultPayload, ct).ConfigureAwait(false);
            State = ConnectionState.Closed;
        }
    }

    private async Task HandleHeartbeatAsync(WireEnvelope<JsonElement> envelope, CancellationToken ct)
    {
        if (envelope.Payload.ValueKind != JsonValueKind.Object)
        {
            Log("HEARTBEAT payload is not a valid JSON object.");
            await SendErrorAsync(envelope.RequestId, ErrorCodes.InvalidPayload, "HEARTBEAT payload must be a JSON object.", ct).ConfigureAwait(false);
            return;
        }

        HeartbeatPayload? heartbeat;
        try
        {
            heartbeat = ProtocolSerializer.ExtractPayload<HeartbeatPayload>(envelope.Payload);
        }
        catch (JsonException ex)
        {
            Log($"Failed to deserialize HeartbeatPayload: {ex.Message}");
            await SendErrorAsync(envelope.RequestId, ErrorCodes.InvalidPayload, "Invalid HeartbeatPayload format.", ct).ConfigureAwait(false);
            return;
        }

        if (heartbeat == null)
        {
            await SendErrorAsync(envelope.RequestId, ErrorCodes.InvalidPayload, "Heartbeat payload is null.", ct).ConfigureAwait(false);
            return;
        }

        bool accepted = _livenessRegistry.RecordActivity(DeviceId!, ConnectionGeneration);
        if (!accepted)
        {
            Log($"Stale heartbeat rejected: device '{DeviceId}' ConnectionGeneration {ConnectionGeneration} is superseded by a newer connection.");
            await SendErrorAsync(envelope.RequestId, ErrorCodes.Unauthorized, "Connection superseded by a newer session for this device.", ct).ConfigureAwait(false);
            State = ConnectionState.Closed;
            try { _tcpClient.Close(); } catch { }
            return;
        }

        var ackPayload = new HeartbeatAckPayload
        {
            AcknowledgedSequence = heartbeat.SequenceNumber,
            ServerTimeUtc = DateTime.UtcNow.ToString("O")
        };

        await SendEnvelopeAsync(MessageTypes.HeartbeatAck, envelope.RequestId, ackPayload, ct).ConfigureAwait(false);
    }

    private async Task HandleMetricBatchAsync(WireEnvelope<JsonElement> envelope, CancellationToken ct)
    {
        if (envelope.Payload.ValueKind != JsonValueKind.Object)
        {
            Log("METRIC_BATCH payload is not a valid JSON object.");
            await SendErrorAsync(envelope.RequestId, ErrorCodes.InvalidPayload, "METRIC_BATCH payload must be a JSON object.", ct).ConfigureAwait(false);
            return;
        }

        MetricBatchPayload? batch;
        try
        {
            batch = ProtocolSerializer.ExtractPayload<MetricBatchPayload>(envelope.Payload);
        }
        catch (JsonException ex)
        {
            Log($"Failed to deserialize MetricBatchPayload: {ex.Message}");
            await SendErrorAsync(envelope.RequestId, ErrorCodes.InvalidPayload, "Invalid MetricBatchPayload format.", ct).ConfigureAwait(false);
            return;
        }

        if (batch == null)
        {
            await SendErrorAsync(envelope.RequestId, ErrorCodes.InvalidPayload, "Metric batch payload is null.", ct).ConfigureAwait(false);
            return;
        }

        // Liveness activity check
        bool acceptedLiveness = _livenessRegistry.RecordActivity(DeviceId!, ConnectionGeneration);
        if (!acceptedLiveness)
        {
            Log($"Stale metric batch rejected: device '{DeviceId}' ConnectionGeneration {ConnectionGeneration} is superseded.");
            await SendErrorAsync(envelope.RequestId, ErrorCodes.Unauthorized, "Connection superseded by a newer session for this device.", ct).ConfigureAwait(false);
            State = ConnectionState.Closed;
            try { _tcpClient.Close(); } catch { }
            return;
        }

        bool dbSuccess = false;
        if (_database != null)
        {
            string clientIp = RemoteEndpoint.Split(':')[0];
            var result = await _database.ProcessMetricBatchAsync(DeviceId!, batch, clientIp).ConfigureAwait(false);
            dbSuccess = result.Success;
        }
        else
        {
            // In-memory fallback
            dbSuccess = true;
        }

        if (!dbSuccess)
        {
            // Per Section 9: BATCH_ACK only after successful DB commit. If SQL fails: do NOT tell Agent the batch was saved.
            Log($"Database persistence failed for batch {batch.BatchId} from device '{DeviceId}'. Suppressing BATCH_ACK.");
            return;
        }

        var ackPayload = new BatchAckPayload
        {
            BootId = batch.BootId,
            BatchId = batch.BatchId,
            Accepted = true,
            SamplesRecorded = batch.SampleCount,
            ErrorMessage = null
        };

        await SendEnvelopeAsync(MessageTypes.BatchAck, envelope.RequestId, ackPayload, ct).ConfigureAwait(false);

        // Broadcast device metrics update to connected Admin clients
        if (_server != null)
        {
            double? cpu = batch.Samples.Find(s => string.Equals(s.MetricName, "CPU", StringComparison.OrdinalIgnoreCase))?.MetricValue;
            double? ram = batch.Samples.Find(s => string.Equals(s.MetricName, "RAM", StringComparison.OrdinalIgnoreCase))?.MetricValue;

            var update = new DeviceUpdatePayload
            {
                DeviceId = DeviceId!,
                Status = "ONLINE",
                CpuUsage = cpu,
                RamUsage = ram,
                UptimeSeconds = batch.UptimeSeconds,
                UpdatedAtUtc = DateTime.UtcNow.ToString("O")
            };
            _ = _server.BroadcastDeviceUpdateAsync(update);

            if (cpu.HasValue || ram.HasValue)
            {
                _server.RuleEngine.ProcessMetricBatch(DeviceId!, cpu ?? 0, ram ?? 0);
            }
        }
    }

    private async Task HandleAdminLoginAsync(WireEnvelope<JsonElement> envelope, CancellationToken ct)
    {
        if (envelope.Payload.ValueKind != JsonValueKind.Object)
        {
            await SendErrorAsync(envelope.RequestId, ErrorCodes.InvalidPayload, "LOGIN payload must be a JSON object.", ct).ConfigureAwait(false);
            return;
        }

        LoginPayload? login;
        try
        {
            login = ProtocolSerializer.ExtractPayload<LoginPayload>(envelope.Payload);
        }
        catch (JsonException)
        {
            await SendErrorAsync(envelope.RequestId, ErrorCodes.InvalidPayload, "Invalid LoginPayload format.", ct).ConfigureAwait(false);
            return;
        }

        if (login == null)
        {
            await SendErrorAsync(envelope.RequestId, ErrorCodes.InvalidPayload, "Login payload is null.", ct).ConfigureAwait(false);
            return;
        }

        bool success = false;
        string? fullName = null;
        string? role = null;
        int? userId = null;

        if (_database != null)
        {
            var auth = await _database.AuthenticateUserAsync(login.Username, login.Password).ConfigureAwait(false);
            success = auth.Success;
            fullName = auth.FullName;
            role = auth.Role;
            userId = auth.UserId;
        }
        else
        {
            // Fallback for tests/dev without DB
            if (login.Username.Equals("admin", StringComparison.OrdinalIgnoreCase) && login.Password == "Admin@123")
            {
                success = true;
                fullName = "Quản Trị Viên Hệ Thống";
                role = "Admin";
                userId = 1;
            }
        }

        if (success)
        {
            SessionId = Guid.NewGuid().ToString("D");
            State = ConnectionState.Authenticated;
            IsAdmin = true;
            UserId = userId;
            _server?.RegisterAdminSubscriber(this);

            var resultPayload = new LoginResultPayload
            {
                Success = true,
                SessionToken = SessionId,
                FullName = fullName,
                Role = role,
                UserId = userId,
                ErrorMessage = null
            };
            Log($"Admin login successful for user '{login.Username}'. Role={role}. SessionToken={SessionId}");
            await SendEnvelopeAsync(MessageTypes.LoginResult, envelope.RequestId, resultPayload, ct).ConfigureAwait(false);
        }
        else
        {
            var resultPayload = new LoginResultPayload
            {
                Success = false,
                ErrorMessage = "Invalid username or password."
            };
            Log($"Admin login failed for user '{login.Username}'.");
            await SendEnvelopeAsync(MessageTypes.LoginResult, envelope.RequestId, resultPayload, ct).ConfigureAwait(false);
            State = ConnectionState.Closed;
        }
    }

    private async Task HandleListDevicesAsync(WireEnvelope<JsonElement> envelope, CancellationToken ct)
    {
        _server?.RegisterAdminSubscriber(this);

        List<DeviceSummaryDto> devices = [];
        if (_database != null)
        {
            devices = await _database.GetDeviceSummariesAsync().ConfigureAwait(false);
        }

        // Overlay in-memory live status
        var allLiveness = _livenessRegistry.GetAllRecords();
        foreach (var live in allLiveness)
        {
            var existing = devices.Find(d => string.Equals(d.DeviceId, live.DeviceId, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.Status = live.CurrentStatus.ToString().ToUpperInvariant();
            }
            else
            {
                devices.Add(new DeviceSummaryDto
                {
                    DeviceId = live.DeviceId,
                    Hostname = live.DeviceId,
                    IpAddress = "127.0.0.1",
                    Status = live.CurrentStatus.ToString().ToUpperInvariant(),
                    LastSeenUtc = DateTime.UtcNow.ToString("O")
                });
            }
        }

        var pagePayload = new DevicePagePayload
        {
            PageNumber = 1,
            PageSize = devices.Count,
            TotalCount = devices.Count,
            TotalPages = 1,
            Devices = devices
        };

        await SendEnvelopeAsync(MessageTypes.DevicePage, envelope.RequestId, pagePayload, ct).ConfigureAwait(false);
    }

    private async Task HandleServiceStatusAsync(WireEnvelope<JsonElement> envelope, CancellationToken ct)
    {
        if (envelope.Payload.ValueKind != JsonValueKind.Object)
        {
            Log("SERVICE_STATUS payload is not a valid JSON object.");
            return;
        }

        ServiceStatusPayload? payload;
        try
        {
            payload = ProtocolSerializer.ExtractPayload<ServiceStatusPayload>(envelope.Payload);
        }
        catch (JsonException ex)
        {
            Log($"Failed to deserialize ServiceStatusPayload: {ex.Message}");
            return;
        }

        if (payload == null) return;

        _livenessRegistry.RecordActivity(DeviceId!, ConnectionGeneration);

        if (_database != null)
        {
            await _database.UpsertServiceStatusAsync(DeviceId!, payload.Services).ConfigureAwait(false);
        }

        if (_server != null)
        {
            _server.RuleEngine.ProcessServiceStatus(DeviceId!, payload.Services);
        }
    }

    private async Task HandleListIncidentsAsync(WireEnvelope<JsonElement> envelope, CancellationToken ct)
    {
        _server?.RegisterAdminSubscriber(this);

        List<IncidentSummaryDto> incidents = [];
        if (_database != null)
        {
            incidents = await _database.GetActiveIncidentsAsync().ConfigureAwait(false);
        }

        var pagePayload = new IncidentPagePayload
        {
            PageNumber = 1,
            PageSize = incidents.Count,
            TotalCount = incidents.Count,
            TotalPages = 1,
            Incidents = incidents
        };

        await SendEnvelopeAsync(MessageTypes.IncidentPage, envelope.RequestId, pagePayload, ct).ConfigureAwait(false);
    }

    private async Task HandleIncidentCommandAsync(WireEnvelope<JsonElement> envelope, CancellationToken ct)
    {
        if (!IsAdmin)
        {
            await SendErrorAsync(envelope.RequestId, ErrorCodes.Unauthorized, "Only authenticated administrators can execute incident commands.", ct).ConfigureAwait(false);
            return;
        }

        if (envelope.Payload.ValueKind != JsonValueKind.Object)
        {
            await SendErrorAsync(envelope.RequestId, ErrorCodes.InvalidPayload, "Incident command payload must be a JSON object.", ct).ConfigureAwait(false);
            return;
        }

        IncidentCommandPayload? cmd;
        try
        {
            cmd = ProtocolSerializer.ExtractPayload<IncidentCommandPayload>(envelope.Payload);
        }
        catch (JsonException ex)
        {
            Log($"Failed to deserialize IncidentCommandPayload: {ex.Message}");
            await SendErrorAsync(envelope.RequestId, ErrorCodes.InvalidPayload, "Invalid IncidentCommandPayload format.", ct).ConfigureAwait(false);
            return;
        }

        if (cmd == null)
        {
            await SendErrorAsync(envelope.RequestId, ErrorCodes.InvalidPayload, "IncidentCommandPayload is null.", ct).ConfigureAwait(false);
            return;
        }

        // Bind CommandType to envelope message type
        cmd.CommandType = envelope.MessageType;
        if (string.IsNullOrWhiteSpace(cmd.CommandId))
        {
            cmd.CommandId = envelope.RequestId;
        }

        CommandResultPayload result;
        if (_database != null)
        {
            int actingUserId = UserId ?? 1;
            result = await _database.ExecuteIncidentCommandAsync(actingUserId, cmd).ConfigureAwait(false);

            if (result.Success && _server != null)
            {
                var updatedInc = await _database.GetIncidentByIdAsync(cmd.IncidentId).ConfigureAwait(false);
                if (updatedInc != null)
                {
                    await _server.BroadcastIncidentUpdateAsync(new IncidentUpdatePayload
                    {
                        IncidentId = updatedInc.IncidentId,
                        DeviceId = updatedInc.DeviceId,
                        ConditionState = updatedInc.ConditionState,
                        WorkflowState = updatedInc.WorkflowState,
                        UpdatedAtUtc = DateTime.UtcNow.ToString("O"),
                        RowVersionHex = result.UpdatedRowVersionHex ?? updatedInc.RowVersionHex
                    }).ConfigureAwait(false);
                }
            }
        }
        else
        {
            result = new CommandResultPayload
            {
                CommandId = cmd.CommandId,
                Success = false,
                StatusCode = 500,
                ErrorCode = "ERR_NO_DATABASE",
                ErrorMessage = "Database is not available."
            };
        }

        await SendEnvelopeAsync(MessageTypes.CommandResult, envelope.RequestId, result, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Single Writer discipline: all outgoing messages are serialized and written through this method guarded by a semaphore lock.
    /// </summary>
    public async Task SendEnvelopeAsync<T>(string messageType, string correlationRequestId, T payload, CancellationToken ct = default)
    {
        var envelope = WireEnvelope<T>.Create(messageType, "CENTRAL-SERVER", payload, correlationRequestId);
        byte[] jsonBytes = ProtocolSerializer.Serialize(envelope);

        await _writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await FrameCodec.WriteFrameAsync(_stream, jsonBytes, ct).ConfigureAwait(false);
            if (messageType != MessageTypes.HeartbeatAck)
            {
                Log($"Sent messageType={messageType} requestId={envelope.RequestId} payloadSize={jsonBytes.Length}b");
            }
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private async Task SendErrorAsync(string correlationRequestId, string errorCode, string errorMessage, CancellationToken ct)
    {
        var errorPayload = new ErrorPayload
        {
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };

        await SendEnvelopeAsync(MessageTypes.Error, correlationRequestId, errorPayload, ct).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        _stream.Dispose();
        _tcpClient.Dispose();
        _writeLock.Dispose();
        await Task.CompletedTask;
    }
}
