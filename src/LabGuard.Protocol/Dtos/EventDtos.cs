namespace LabGuard.Protocol.Dtos;

public class SubscribePayload
{
    public string Channel { get; set; } = string.Empty; // "DEVICES", "INCIDENTS"
}

public class SnapshotBeginPayload
{
    public string Channel { get; set; } = string.Empty;
    public int ExpectedItems { get; set; }
}

public class SnapshotPagePayload
{
    public string Channel { get; set; } = string.Empty;
    public int PageIndex { get; set; }
    public string ItemsJson { get; set; } = string.Empty;
}

public class SnapshotEndPayload
{
    public string Channel { get; set; } = string.Empty;
    public int TotalItems { get; set; }
}

public class DeviceUpdatePayload
{
    public string DeviceId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // "ONLINE", "OFFLINE", "DEGRADED"
    public double? CpuUsage { get; set; }
    public double? RamUsage { get; set; }
    public long? UptimeSeconds { get; set; }
    public string UpdatedAtUtc { get; set; } = DateTime.UtcNow.ToString("O");
}

public class IncidentOpenPayload
{
    public long IncidentId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public string ConditionState { get; set; } = "ACTIVE";
    public string WorkflowState { get; set; } = "OPEN";
    public string OpenedAtUtc { get; set; } = DateTime.UtcNow.ToString("O");
    public string RowVersionHex { get; set; } = string.Empty;
}

public class IncidentUpdatePayload
{
    public long IncidentId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string ConditionState { get; set; } = string.Empty;
    public string WorkflowState { get; set; } = string.Empty;
    public string UpdatedAtUtc { get; set; } = DateTime.UtcNow.ToString("O");
    public string RowVersionHex { get; set; } = string.Empty;
}
