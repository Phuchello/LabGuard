namespace LabGuard.Protocol;

/// <summary>
/// Canonical 69-Message Catalog defined in 03_PROTOCOL_SPEC.md.
/// Categorized into P0-MVP REQUIRED (34), P0 OPTIONAL (25), and P1 CONDITIONAL (10).
/// </summary>
public static class MessageTypes
{
    // =========================================================================
    // 1. P0-MVP REQUIRED (34 messages) — Mandatory for 5-week MVP acceptance
    // =========================================================================

    // 1.1 Handshake & Transport Greeting (3)
    public const string HelloAgent = "HELLO_AGENT";
    public const string HelloAdmin = "HELLO_ADMIN";
    public const string HelloAck = "HELLO_ACK";

    // 1.2 Agent Authentication (2)
    public const string AuthAgent = "AUTH_AGENT";
    public const string AuthResult = "AUTH_RESULT";

    // 1.3 Heartbeat & Liveness (2)
    public const string Heartbeat = "HEARTBEAT";
    public const string HeartbeatAck = "HEARTBEAT_ACK";

    // 1.4 Telemetry & Service Status (3)
    public const string MetricBatch = "METRIC_BATCH";
    public const string BatchAck = "BATCH_ACK";
    public const string ServiceStatus = "SERVICE_STATUS";

    // 1.5 Disconnect & Keepalive (2)
    public const string AgentGoodbye = "AGENT_GOODBYE";
    public const string GoodbyeAck = "GOODBYE_ACK";

    // 1.6 Admin Auth & Identity Management (4)
    public const string Login = "LOGIN";
    public const string LoginResult = "LOGIN_RESULT";
    public const string Logout = "LOGOUT";
    public const string LogoutResult = "LOGOUT_RESULT";

    // 1.7 Device & Incident Queries (4)
    public const string ListDevices = "LIST_DEVICES";
    public const string DevicePage = "DEVICE_PAGE";
    public const string ListIncidents = "LIST_INCIDENTS";
    public const string IncidentPage = "INCIDENT_PAGE";

    // 1.8 Subscription & Snapshot Sync (5)
    public const string SubscribeDevices = "SUBSCRIBE_DEVICES";
    public const string SubscribeIncidents = "SUBSCRIBE_INCIDENTS";
    public const string SnapshotBegin = "SNAPSHOT_BEGIN";
    public const string SnapshotPage = "SNAPSHOT_PAGE";
    public const string SnapshotEnd = "SNAPSHOT_END";

    // 1.9 Realtime Event Broadcast (3)
    public const string DeviceUpdate = "DEVICE_UPDATE";
    public const string IncidentOpen = "INCIDENT_OPEN";
    public const string IncidentUpdate = "INCIDENT_UPDATE";

    // 1.10 Incident Actions & Error Handling (6)
    public const string IncidentAck = "INCIDENT_ACK";
    public const string IncidentAssign = "INCIDENT_ASSIGN";
    public const string IncidentStart = "INCIDENT_START";
    public const string IncidentResolve = "INCIDENT_RESOLVE";
    public const string CommandResult = "COMMAND_RESULT";
    public const string Error = "ERROR";

    // =========================================================================
    // 2. P0 OPTIONAL (25 messages) — Non-blocking extensions
    // =========================================================================
    public const string ConfigUpdate = "CONFIG_UPDATE";
    public const string ConfigResult = "CONFIG_RESULT";
    public const string ServiceCheckRequest = "SERVICE_CHECK_REQUEST";
    public const string Ping = "PING";
    public const string Pong = "PONG";
    public const string SignUp = "SIGN_UP";
    public const string SignUpResult = "SIGN_UP_RESULT";
    public const string CreateInvite = "CREATE_INVITE";
    public const string InviteResult = "INVITE_RESULT";
    public const string GetDevice = "GET_DEVICE";
    public const string DeviceDetail = "DEVICE_DETAIL";
    public const string GetMetrics = "GET_METRICS";
    public const string MetricsResult = "METRICS_RESULT";
    public const string GetIncident = "GET_INCIDENT";
    public const string IncidentDetail = "INCIDENT_DETAIL";
    public const string GetHistory = "GET_HISTORY";
    public const string HistoryPage = "HISTORY_PAGE";
    public const string GetServerHealth = "GET_SERVER_HEALTH";
    public const string ServerHealth = "SERVER_HEALTH";
    public const string GetReport = "GET_REPORT";
    public const string ReportResult = "REPORT_RESULT";
    public const string SubscribeHealth = "SUBSCRIBE_HEALTH";
    public const string HealthUpdate = "HEALTH_UPDATE";
    public const string Unsubscribe = "UNSUBSCRIBE";
    public const string UnsubscribeResult = "UNSUBSCRIBE_RESULT";

    // =========================================================================
    // 3. P1 CONDITIONAL (10 messages) — Distributed Gateway / Collector Routing
    // =========================================================================
    public const string HelloCollector = "HELLO_COLLECTOR";
    public const string AuthCollector = "AUTH_COLLECTOR";
    public const string ForwardOpen = "FORWARD_OPEN";
    public const string ForwardData = "FORWARD_DATA";
    public const string ForwardResult = "FORWARD_RESULT";
    public const string ForwardClose = "FORWARD_CLOSE";
    public const string CloseSession = "CLOSE_SESSION";
    public const string CollectorHeartbeat = "COLLECTOR_HEARTBEAT";
    public const string HealthProbe = "HEALTH_PROBE";
    public const string HealthResult = "HEALTH_RESULT";

    /// <summary>
    /// All 69 canonical messages recognized by LabGuard.
    /// </summary>
    public static readonly HashSet<string> All = new(StringComparer.Ordinal)
    {
        // P0 Required (34)
        HelloAgent, HelloAdmin, HelloAck,
        AuthAgent, AuthResult,
        Heartbeat, HeartbeatAck,
        MetricBatch, BatchAck, ServiceStatus,
        AgentGoodbye, GoodbyeAck,
        Login, LoginResult, Logout, LogoutResult,
        ListDevices, DevicePage, ListIncidents, IncidentPage,
        SubscribeDevices, SubscribeIncidents, SnapshotBegin, SnapshotPage, SnapshotEnd,
        DeviceUpdate, IncidentOpen, IncidentUpdate,
        IncidentAck, IncidentAssign, IncidentStart, IncidentResolve, CommandResult, Error,

        // P0 Optional (25)
        ConfigUpdate, ConfigResult, ServiceCheckRequest,
        Ping, Pong,
        SignUp, SignUpResult, CreateInvite, InviteResult,
        GetDevice, DeviceDetail, GetMetrics, MetricsResult,
        GetIncident, IncidentDetail, GetHistory, HistoryPage,
        GetServerHealth, ServerHealth, GetReport, ReportResult,
        SubscribeHealth, HealthUpdate, Unsubscribe, UnsubscribeResult,

        // P1 Conditional (10)
        HelloCollector, AuthCollector,
        ForwardOpen, ForwardData, ForwardResult, ForwardClose, CloseSession,
        CollectorHeartbeat, HealthProbe, HealthResult
    };

    public static bool IsValid(string messageType) => !string.IsNullOrEmpty(messageType) && All.Contains(messageType);
}
