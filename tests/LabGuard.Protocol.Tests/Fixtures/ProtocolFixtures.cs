using System.Text;
using LabGuard.Protocol.Dtos;

namespace LabGuard.Protocol.Tests.Fixtures;

/// <summary>
/// Canonical protocol fixtures for LabGuard wire contract testing (LG-A-001).
/// All 16 canonical fixtures:
/// - Follow the exact 6-field outer envelope: version, messageType, requestId, deviceId, sentAtUtc, payload
/// - Are deterministic and minified (no spaces between tokens, no trailing newline)
/// - UTF-8 encoded without BOM
/// - Strictly conform to MAX_JSON_BYTES &lt;= 262,144 bytes
/// - Validate through EnvelopeValidator
/// - Deserialize into their intended DTOs
/// </summary>
public static class ProtocolFixtures
{
    // =========================================================================
    // 1. HEARTBEAT & HEARTBEAT_ACK
    // =========================================================================
    public const string HeartbeatJson =
        "{\"version\":1,\"messageType\":\"HEARTBEAT\",\"requestId\":\"c0000000-0000-4000-8000-000000000001\",\"deviceId\":\"PC-LAB1-01\",\"sentAtUtc\":\"2026-09-20T10:00:00.0000000Z\",\"payload\":{\"sequenceNumber\":1,\"uptimeSeconds\":3600}}";

    public const string HeartbeatAckJson =
        "{\"version\":1,\"messageType\":\"HEARTBEAT_ACK\",\"requestId\":\"c0000000-0000-4000-8000-000000000009\",\"deviceId\":\"CENTRAL-SERVER\",\"sentAtUtc\":\"2026-09-20T10:00:00.0000000Z\",\"payload\":{\"serverTimeUtc\":\"2026-09-20T10:00:00.0000000Z\",\"acknowledgedSequence\":1}}";

    // =========================================================================
    // 2. METRIC_BATCH & BATCH_ACK
    // =========================================================================
    public const string MetricBatchJson =
        "{\"version\":1,\"messageType\":\"METRIC_BATCH\",\"requestId\":\"c0000000-0000-4000-8000-000000000002\",\"deviceId\":\"PC-LAB1-01\",\"sentAtUtc\":\"2026-09-20T10:00:00.0000000Z\",\"payload\":{\"bootId\":\"b0000000-0000-4000-8000-000000000001\",\"batchId\":\"b0000000-0000-4000-8000-000000000002\",\"sampleCount\":2,\"uptimeSeconds\":3600,\"isReplay\":false,\"samples\":[{\"metricName\":\"CPU\",\"metricValue\":45.5,\"unit\":\"%\",\"recordedAtUtc\":\"2026-09-20T10:00:00.0000000Z\"},{\"metricName\":\"RAM\",\"metricValue\":62.0,\"unit\":\"%\",\"recordedAtUtc\":\"2026-09-20T10:00:00.0000000Z\"}]}}";

    public const string BatchAckJson =
        "{\"version\":1,\"messageType\":\"BATCH_ACK\",\"requestId\":\"c0000000-0000-4000-8000-000000000010\",\"deviceId\":\"CENTRAL-SERVER\",\"sentAtUtc\":\"2026-09-20T10:00:00.0000000Z\",\"payload\":{\"bootId\":\"b0000000-0000-4000-8000-000000000001\",\"batchId\":\"b0000000-0000-4000-8000-000000000002\",\"accepted\":true,\"samplesRecorded\":2,\"errorMessage\":null}}";

    // =========================================================================
    // 3. SERVICE_STATUS
    // =========================================================================
    public const string ServiceStatusJson =
        "{\"version\":1,\"messageType\":\"SERVICE_STATUS\",\"requestId\":\"c0000000-0000-4000-8000-000000000011\",\"deviceId\":\"PC-LAB1-01\",\"sentAtUtc\":\"2026-09-20T10:00:00.0000000Z\",\"payload\":{\"isReplay\":false,\"services\":[{\"serviceName\":\"HTTP\",\"port\":80,\"isListening\":true,\"responseTimeMs\":4,\"lastCheckedUtc\":\"2026-09-20T10:00:00.0000000Z\"},{\"serviceName\":\"SSH\",\"port\":22,\"isListening\":true,\"responseTimeMs\":12,\"lastCheckedUtc\":\"2026-09-20T10:00:00.0000000Z\"}]}}";

    // =========================================================================
    // 4. HELLO_AGENT & HELLO_ACK
    // =========================================================================
    public const string HelloAgentJson =
        "{\"version\":1,\"messageType\":\"HELLO_AGENT\",\"requestId\":\"c0000000-0000-4000-8000-000000000005\",\"deviceId\":\"PC-LAB1-01\",\"sentAtUtc\":\"2026-09-20T10:00:00.0000000Z\",\"payload\":{\"agentVersion\":\"1.0.0\",\"osVersion\":\"Microsoft Windows 11 Pro\",\"hostname\":\"LAB01-PC01\",\"macAddress\":\"00:1A:2B:3C:4D:5E\"}}";

    public const string HelloAckJson =
        "{\"version\":1,\"messageType\":\"HELLO_ACK\",\"requestId\":\"c0000000-0000-4000-8000-000000000006\",\"deviceId\":\"CENTRAL-SERVER\",\"sentAtUtc\":\"2026-09-20T10:00:00.0000000Z\",\"payload\":{\"serverVersion\":\"1.0.0\",\"capabilities\":[\"TLS\",\"JSON_FRAMING\",\"OCC\"],\"serverTimeUtc\":\"2026-09-20T10:00:00.0000000Z\",\"heartbeatIntervalSeconds\":5}}";

    // =========================================================================
    // 5. AUTH_AGENT & AUTH_RESULT
    // =========================================================================
    public const string AuthAgentJson =
        "{\"version\":1,\"messageType\":\"AUTH_AGENT\",\"requestId\":\"c0000000-0000-4000-8000-000000000007\",\"deviceId\":\"PC-LAB1-01\",\"sentAtUtc\":\"2026-09-20T10:00:00.0000000Z\",\"payload\":{\"preSharedToken\":\"LABGUARD_TOKEN_2026\",\"clientNonce\":\"a0000000000000000000000000000001\"}}";

    public const string AuthResultJson =
        "{\"version\":1,\"messageType\":\"AUTH_RESULT\",\"requestId\":\"c0000000-0000-4000-8000-000000000008\",\"deviceId\":\"CENTRAL-SERVER\",\"sentAtUtc\":\"2026-09-20T10:00:00.0000000Z\",\"payload\":{\"success\":true,\"sessionId\":\"s0000000-0000-4000-8000-000000000001\",\"assignedDeviceId\":\"PC-LAB1-01\",\"errorMessage\":null}}";

    // =========================================================================
    // 6. AGENT_GOODBYE & GOODBYE_ACK
    // =========================================================================
    public const string AgentGoodbyeJson =
        "{\"version\":1,\"messageType\":\"AGENT_GOODBYE\",\"requestId\":\"c0000000-0000-4000-8000-000000000012\",\"deviceId\":\"PC-LAB1-01\",\"sentAtUtc\":\"2026-09-20T10:00:00.0000000Z\",\"payload\":{\"reason\":\"NORMAL_SHUTDOWN\",\"uptimeSeconds\":7200}}";

    public const string GoodbyeAckJson =
        "{\"version\":1,\"messageType\":\"GOODBYE_ACK\",\"requestId\":\"c0000000-0000-4000-8000-000000000013\",\"deviceId\":\"CENTRAL-SERVER\",\"sentAtUtc\":\"2026-09-20T10:00:00.0000000Z\",\"payload\":{\"success\":true}}";

    // =========================================================================
    // 7. LOGIN & LOGIN_RESULT
    // =========================================================================
    public const string LoginJson =
        "{\"version\":1,\"messageType\":\"LOGIN\",\"requestId\":\"c0000000-0000-4000-8000-000000000003\",\"deviceId\":\"ADMIN-CONSOLE\",\"sentAtUtc\":\"2026-09-20T10:00:00.0000000Z\",\"payload\":{\"username\":\"admin\",\"password\":\"AdminPassword123!\"}}";

    public const string LoginResultJson =
        "{\"version\":1,\"messageType\":\"LOGIN_RESULT\",\"requestId\":\"c0000000-0000-4000-8000-000000000014\",\"deviceId\":\"CENTRAL-SERVER\",\"sentAtUtc\":\"2026-09-20T10:00:00.0000000Z\",\"payload\":{\"success\":true,\"sessionToken\":\"t0000000-0000-4000-8000-000000000001\",\"userId\":1,\"role\":\"Admin\",\"fullName\":\"Administrator\",\"errorMessage\":null}}";

    // =========================================================================
    // 8. INCIDENT_ACK & COMMAND_RESULT
    // =========================================================================
    public const string IncidentAckJson =
        "{\"version\":1,\"messageType\":\"INCIDENT_ACK\",\"requestId\":\"c0000000-0000-4000-8000-000000000004\",\"deviceId\":\"ADMIN-CONSOLE\",\"sentAtUtc\":\"2026-09-20T10:00:00.0000000Z\",\"payload\":{\"commandId\":\"c0000000-0000-4000-8000-000000000005\",\"commandType\":\"INCIDENT_ACK\",\"incidentId\":1,\"expectedRowVersion\":\"0x0000000000000001\",\"commandParams\":{}}}";

    public const string CommandResultJson =
        "{\"version\":1,\"messageType\":\"COMMAND_RESULT\",\"requestId\":\"c0000000-0000-4000-8000-000000000015\",\"deviceId\":\"CENTRAL-SERVER\",\"sentAtUtc\":\"2026-09-20T10:00:00.0000000Z\",\"payload\":{\"commandId\":\"c0000000-0000-4000-8000-000000000005\",\"success\":true,\"statusCode\":200,\"errorCode\":null,\"errorMessage\":null,\"updatedRowVersionHex\":\"0x0000000000000002\"}}";

    // =========================================================================
    // 9. ERROR
    // =========================================================================
    public const string ErrorJson =
        "{\"version\":1,\"messageType\":\"ERROR\",\"requestId\":\"c0000000-0000-4000-8000-000000000016\",\"deviceId\":\"CENTRAL-SERVER\",\"sentAtUtc\":\"2026-09-20T10:00:00.0000000Z\",\"payload\":{\"errorCode\":\"ERR_INVALID_PAYLOAD\",\"errorMessage\":\"Invalid envelope payload.\",\"details\":null}}";

    // =========================================================================
    // BYTE & FRAMED ACCESSORS
    // =========================================================================
    public static byte[] HeartbeatBytes => Encoding.UTF8.GetBytes(HeartbeatJson);
    public static byte[] HeartbeatAckBytes => Encoding.UTF8.GetBytes(HeartbeatAckJson);
    public static byte[] MetricBatchBytes => Encoding.UTF8.GetBytes(MetricBatchJson);
    public static byte[] BatchAckBytes => Encoding.UTF8.GetBytes(BatchAckJson);
    public static byte[] ServiceStatusBytes => Encoding.UTF8.GetBytes(ServiceStatusJson);
    public static byte[] HelloAgentBytes => Encoding.UTF8.GetBytes(HelloAgentJson);
    public static byte[] HelloAckBytes => Encoding.UTF8.GetBytes(HelloAckJson);
    public static byte[] AuthAgentBytes => Encoding.UTF8.GetBytes(AuthAgentJson);
    public static byte[] AuthResultBytes => Encoding.UTF8.GetBytes(AuthResultJson);
    public static byte[] AgentGoodbyeBytes => Encoding.UTF8.GetBytes(AgentGoodbyeJson);
    public static byte[] GoodbyeAckBytes => Encoding.UTF8.GetBytes(GoodbyeAckJson);
    public static byte[] LoginBytes => Encoding.UTF8.GetBytes(LoginJson);
    public static byte[] LoginResultBytes => Encoding.UTF8.GetBytes(LoginResultJson);
    public static byte[] IncidentAckBytes => Encoding.UTF8.GetBytes(IncidentAckJson);
    public static byte[] CommandResultBytes => Encoding.UTF8.GetBytes(CommandResultJson);
    public static byte[] ErrorBytes => Encoding.UTF8.GetBytes(ErrorJson);

    public static byte[] HeartbeatFramed => FrameCodec.Encode(HeartbeatBytes);
    public static byte[] HeartbeatAckFramed => FrameCodec.Encode(HeartbeatAckBytes);
    public static byte[] MetricBatchFramed => FrameCodec.Encode(MetricBatchBytes);
    public static byte[] BatchAckFramed => FrameCodec.Encode(BatchAckBytes);
    public static byte[] ServiceStatusFramed => FrameCodec.Encode(ServiceStatusBytes);
    public static byte[] HelloAgentFramed => FrameCodec.Encode(HelloAgentBytes);
    public static byte[] HelloAckFramed => FrameCodec.Encode(HelloAckBytes);
    public static byte[] AuthAgentFramed => FrameCodec.Encode(AuthAgentBytes);
    public static byte[] AuthResultFramed => FrameCodec.Encode(AuthResultBytes);
    public static byte[] AgentGoodbyeFramed => FrameCodec.Encode(AgentGoodbyeBytes);
    public static byte[] GoodbyeAckFramed => FrameCodec.Encode(GoodbyeAckBytes);
    public static byte[] LoginFramed => FrameCodec.Encode(LoginBytes);
    public static byte[] LoginResultFramed => FrameCodec.Encode(LoginResultBytes);
    public static byte[] IncidentAckFramed => FrameCodec.Encode(IncidentAckBytes);
    public static byte[] CommandResultFramed => FrameCodec.Encode(CommandResultBytes);
    public static byte[] ErrorFramed => FrameCodec.Encode(ErrorBytes);

    // =========================================================================
    // 16 CANONICAL FIXTURE REGISTRY
    // =========================================================================
    public static readonly (string FileName, string Json, string MessageType)[] All16 =
    [
        ("heartbeat.json", HeartbeatJson, MessageTypes.Heartbeat),
        ("heartbeat_ack.json", HeartbeatAckJson, MessageTypes.HeartbeatAck),
        ("metric_batch.json", MetricBatchJson, MessageTypes.MetricBatch),
        ("batch_ack.json", BatchAckJson, MessageTypes.BatchAck),
        ("service_status.json", ServiceStatusJson, MessageTypes.ServiceStatus),
        ("hello_agent.json", HelloAgentJson, MessageTypes.HelloAgent),
        ("hello_ack.json", HelloAckJson, MessageTypes.HelloAck),
        ("auth_agent.json", AuthAgentJson, MessageTypes.AuthAgent),
        ("auth_result.json", AuthResultJson, MessageTypes.AuthResult),
        ("agent_goodbye.json", AgentGoodbyeJson, MessageTypes.AgentGoodbye),
        ("goodbye_ack.json", GoodbyeAckJson, MessageTypes.GoodbyeAck),
        ("login.json", LoginJson, MessageTypes.Login),
        ("login_result.json", LoginResultJson, MessageTypes.LoginResult),
        ("incident_ack.json", IncidentAckJson, MessageTypes.IncidentAck),
        ("command_result.json", CommandResultJson, MessageTypes.CommandResult),
        ("error.json", ErrorJson, MessageTypes.Error)
    ];

    // =========================================================================
    // ENVELOPE FACTORY METHODS
    // =========================================================================
    public static WireEnvelope<HeartbeatPayload> CreateHeartbeatEnvelope() => new()
    {
        Version = 1,
        MessageType = MessageTypes.Heartbeat,
        RequestId = "c0000000-0000-4000-8000-000000000001",
        DeviceId = "PC-LAB1-01",
        SentAtUtc = "2026-09-20T10:00:00.0000000Z",
        Payload = new HeartbeatPayload { SequenceNumber = 1, UptimeSeconds = 3600 }
    };

    public static WireEnvelope<HeartbeatAckPayload> CreateHeartbeatAckEnvelope() => new()
    {
        Version = 1,
        MessageType = MessageTypes.HeartbeatAck,
        RequestId = "c0000000-0000-4000-8000-000000000009",
        DeviceId = "CENTRAL-SERVER",
        SentAtUtc = "2026-09-20T10:00:00.0000000Z",
        Payload = new HeartbeatAckPayload { ServerTimeUtc = "2026-09-20T10:00:00.0000000Z", AcknowledgedSequence = 1 }
    };

    public static WireEnvelope<MetricBatchPayload> CreateMetricBatchEnvelope() => new()
    {
        Version = 1,
        MessageType = MessageTypes.MetricBatch,
        RequestId = "c0000000-0000-4000-8000-000000000002",
        DeviceId = "PC-LAB1-01",
        SentAtUtc = "2026-09-20T10:00:00.0000000Z",
        Payload = new MetricBatchPayload
        {
            BootId = "b0000000-0000-4000-8000-000000000001",
            BatchId = "b0000000-0000-4000-8000-000000000002",
            SampleCount = 2,
            UptimeSeconds = 3600,
            IsReplay = false,
            Samples =
            [
                new MetricSampleDto { MetricName = "CPU", MetricValue = 45.5, Unit = "%", RecordedAtUtc = "2026-09-20T10:00:00.0000000Z" },
                new MetricSampleDto { MetricName = "RAM", MetricValue = 62.0, Unit = "%", RecordedAtUtc = "2026-09-20T10:00:00.0000000Z" }
            ]
        }
    };

    public static WireEnvelope<BatchAckPayload> CreateBatchAckEnvelope() => new()
    {
        Version = 1,
        MessageType = MessageTypes.BatchAck,
        RequestId = "c0000000-0000-4000-8000-000000000010",
        DeviceId = "CENTRAL-SERVER",
        SentAtUtc = "2026-09-20T10:00:00.0000000Z",
        Payload = new BatchAckPayload
        {
            BootId = "b0000000-0000-4000-8000-000000000001",
            BatchId = "b0000000-0000-4000-8000-000000000002",
            Accepted = true,
            SamplesRecorded = 2,
            ErrorMessage = null
        }
    };

    public static WireEnvelope<ServiceStatusPayload> CreateServiceStatusEnvelope() => new()
    {
        Version = 1,
        MessageType = MessageTypes.ServiceStatus,
        RequestId = "c0000000-0000-4000-8000-000000000011",
        DeviceId = "PC-LAB1-01",
        SentAtUtc = "2026-09-20T10:00:00.0000000Z",
        Payload = new ServiceStatusPayload
        {
            IsReplay = false,
            Services =
            [
                new ServiceStatusItemDto { ServiceName = "HTTP", Port = 80, IsListening = true, ResponseTimeMs = 4, LastCheckedUtc = "2026-09-20T10:00:00.0000000Z" },
                new ServiceStatusItemDto { ServiceName = "SSH", Port = 22, IsListening = true, ResponseTimeMs = 12, LastCheckedUtc = "2026-09-20T10:00:00.0000000Z" }
            ]
        }
    };

    public static WireEnvelope<HelloAgentPayload> CreateHelloAgentEnvelope() => new()
    {
        Version = 1,
        MessageType = MessageTypes.HelloAgent,
        RequestId = "c0000000-0000-4000-8000-000000000005",
        DeviceId = "PC-LAB1-01",
        SentAtUtc = "2026-09-20T10:00:00.0000000Z",
        Payload = new HelloAgentPayload
        {
            AgentVersion = "1.0.0",
            OsVersion = "Microsoft Windows 11 Pro",
            Hostname = "LAB01-PC01",
            MacAddress = "00:1A:2B:3C:4D:5E"
        }
    };

    public static WireEnvelope<HelloAckPayload> CreateHelloAckEnvelope() => new()
    {
        Version = 1,
        MessageType = MessageTypes.HelloAck,
        RequestId = "c0000000-0000-4000-8000-000000000006",
        DeviceId = "CENTRAL-SERVER",
        SentAtUtc = "2026-09-20T10:00:00.0000000Z",
        Payload = new HelloAckPayload
        {
            ServerVersion = "1.0.0",
            Capabilities = ["TLS", "JSON_FRAMING", "OCC"],
            ServerTimeUtc = "2026-09-20T10:00:00.0000000Z",
            HeartbeatIntervalSeconds = 5
        }
    };

    public static WireEnvelope<AuthAgentPayload> CreateAuthAgentEnvelope() => new()
    {
        Version = 1,
        MessageType = MessageTypes.AuthAgent,
        RequestId = "c0000000-0000-4000-8000-000000000007",
        DeviceId = "PC-LAB1-01",
        SentAtUtc = "2026-09-20T10:00:00.0000000Z",
        Payload = new AuthAgentPayload
        {
            PreSharedToken = "LABGUARD_TOKEN_2026",
            ClientNonce = "a0000000000000000000000000000001"
        }
    };

    public static WireEnvelope<AuthResultPayload> CreateAuthResultEnvelope() => new()
    {
        Version = 1,
        MessageType = MessageTypes.AuthResult,
        RequestId = "c0000000-0000-4000-8000-000000000008",
        DeviceId = "CENTRAL-SERVER",
        SentAtUtc = "2026-09-20T10:00:00.0000000Z",
        Payload = new AuthResultPayload
        {
            Success = true,
            SessionId = "s0000000-0000-4000-8000-000000000001",
            AssignedDeviceId = "PC-LAB1-01",
            ErrorMessage = null
        }
    };

    public static WireEnvelope<AgentGoodbyePayload> CreateAgentGoodbyeEnvelope() => new()
    {
        Version = 1,
        MessageType = MessageTypes.AgentGoodbye,
        RequestId = "c0000000-0000-4000-8000-000000000012",
        DeviceId = "PC-LAB1-01",
        SentAtUtc = "2026-09-20T10:00:00.0000000Z",
        Payload = new AgentGoodbyePayload
        {
            Reason = "NORMAL_SHUTDOWN",
            UptimeSeconds = 7200
        }
    };

    public static WireEnvelope<GoodbyeAckPayload> CreateGoodbyeAckEnvelope() => new()
    {
        Version = 1,
        MessageType = MessageTypes.GoodbyeAck,
        RequestId = "c0000000-0000-4000-8000-000000000013",
        DeviceId = "CENTRAL-SERVER",
        SentAtUtc = "2026-09-20T10:00:00.0000000Z",
        Payload = new GoodbyeAckPayload { Success = true }
    };

    public static WireEnvelope<LoginPayload> CreateLoginEnvelope() => new()
    {
        Version = 1,
        MessageType = MessageTypes.Login,
        RequestId = "c0000000-0000-4000-8000-000000000003",
        DeviceId = "ADMIN-CONSOLE",
        SentAtUtc = "2026-09-20T10:00:00.0000000Z",
        Payload = new LoginPayload
        {
            Username = "admin",
            Password = "AdminPassword123!"
        }
    };

    public static WireEnvelope<LoginResultPayload> CreateLoginResultEnvelope() => new()
    {
        Version = 1,
        MessageType = MessageTypes.LoginResult,
        RequestId = "c0000000-0000-4000-8000-000000000014",
        DeviceId = "CENTRAL-SERVER",
        SentAtUtc = "2026-09-20T10:00:00.0000000Z",
        Payload = new LoginResultPayload
        {
            Success = true,
            SessionToken = "t0000000-0000-4000-8000-000000000001",
            UserId = 1,
            Role = "Admin",
            FullName = "Administrator",
            ErrorMessage = null
        }
    };

    public static WireEnvelope<IncidentCommandPayload> CreateIncidentAckEnvelope() => new()
    {
        Version = 1,
        MessageType = MessageTypes.IncidentAck,
        RequestId = "c0000000-0000-4000-8000-000000000004",
        DeviceId = "ADMIN-CONSOLE",
        SentAtUtc = "2026-09-20T10:00:00.0000000Z",
        Payload = new IncidentCommandPayload
        {
            CommandId = "c0000000-0000-4000-8000-000000000005",
            CommandType = MessageTypes.IncidentAck,
            IncidentId = 1,
            ExpectedRowVersion = "0x0000000000000001",
            CommandParams = []
        }
    };

    public static WireEnvelope<CommandResultPayload> CreateCommandResultEnvelope() => new()
    {
        Version = 1,
        MessageType = MessageTypes.CommandResult,
        RequestId = "c0000000-0000-4000-8000-000000000015",
        DeviceId = "CENTRAL-SERVER",
        SentAtUtc = "2026-09-20T10:00:00.0000000Z",
        Payload = new CommandResultPayload
        {
            CommandId = "c0000000-0000-4000-8000-000000000005",
            Success = true,
            StatusCode = 200,
            ErrorCode = null,
            ErrorMessage = null,
            UpdatedRowVersionHex = "0x0000000000000002"
        }
    };

    public static WireEnvelope<ErrorPayload> CreateErrorEnvelope() => new()
    {
        Version = 1,
        MessageType = MessageTypes.Error,
        RequestId = "c0000000-0000-4000-8000-000000000016",
        DeviceId = "CENTRAL-SERVER",
        SentAtUtc = "2026-09-20T10:00:00.0000000Z",
        Payload = new ErrorPayload
        {
            ErrorCode = "ERR_INVALID_PAYLOAD",
            ErrorMessage = "Invalid envelope payload.",
            Details = null
        }
    };
}
