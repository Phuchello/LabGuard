namespace LabGuard.Protocol.Dtos;

public class HelloAgentPayload
{
    public string AgentVersion { get; set; } = string.Empty;
    public string OsVersion { get; set; } = string.Empty;
    public string Hostname { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
}

public class HelloAdminPayload
{
    public string ClientVersion { get; set; } = string.Empty;
    public string Hostname { get; set; } = string.Empty;
}

public class HelloAckPayload
{
    public string ServerVersion { get; set; } = "1.0.0";
    public string[] Capabilities { get; set; } = ["TLS", "JSON_FRAMING", "OCC"];
    public string ServerTimeUtc { get; set; } = DateTime.UtcNow.ToString("O");
    public int HeartbeatIntervalSeconds { get; set; } = 5;
}

public class AuthAgentPayload
{
    public string PreSharedToken { get; set; } = string.Empty;
    public string ClientNonce { get; set; } = Guid.NewGuid().ToString("N");
}

public class AuthResultPayload
{
    public bool Success { get; set; }
    public string? SessionId { get; set; }
    public string? AssignedDeviceId { get; set; }
    public string? ErrorMessage { get; set; }
}

public class AgentGoodbyePayload
{
    public string Reason { get; set; } = "NORMAL_SHUTDOWN";
    public long UptimeSeconds { get; set; }
}

public class GoodbyeAckPayload
{
    public bool Success { get; set; } = true;
}
