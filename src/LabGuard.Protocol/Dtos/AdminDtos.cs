namespace LabGuard.Protocol.Dtos;

public class LoginPayload
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResultPayload
{
    public bool Success { get; set; }
    public string? SessionToken { get; set; }
    public int? UserId { get; set; }
    public string? Role { get; set; } // "Admin", "Operator", "Viewer"
    public string? FullName { get; set; }
    public string? ErrorMessage { get; set; }
}

public class LogoutPayload
{
    public string SessionToken { get; set; } = string.Empty;
}

public class LogoutResultPayload
{
    public bool Success { get; set; } = true;
}

public class ListDevicesPayload
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? StatusFilter { get; set; } // "ALL", "ONLINE", "OFFLINE", "DEGRADED"
}

public class DeviceSummaryDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string Hostname { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public string Status { get; set; } = "OFFLINE";
    public string? LastSeenUtc { get; set; }
    public double? CpuUsage { get; set; }
    public double? RamUsage { get; set; }
    public long? UptimeSeconds { get; set; }
}

public class DevicePagePayload
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public List<DeviceSummaryDto> Devices { get; set; } = [];
}

public class ListIncidentsPayload
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? StatusFilter { get; set; } // "ALL", "OPEN", "ACKNOWLEDGED", "INVESTIGATING", "RECOVERED", "RESOLVED"
}

public class IncidentSummaryDto
{
    public long IncidentId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public string ConditionState { get; set; } = "ACTIVE"; // "ACTIVE", "RECOVERED"
    public string WorkflowState { get; set; } = "OPEN"; // "OPEN", "ACKNOWLEDGED", "INVESTIGATING", "RESOLVED"
    public int OccurrenceCount { get; set; } = 1;
    public string OpenedAtUtc { get; set; } = string.Empty;
    public int? AssignedToUserId { get; set; }
    public string RowVersionHex { get; set; } = string.Empty; // 18-char hex string "0x0000000000000001"
}

public class IncidentPagePayload
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public List<IncidentSummaryDto> Incidents { get; set; } = [];
}

public class IncidentCommandPayload
{
    public string CommandId { get; set; } = Guid.NewGuid().ToString("D");
    public string CommandType { get; set; } = string.Empty; // "INCIDENT_ACK", "INCIDENT_ASSIGN", "INCIDENT_START", "INCIDENT_RESOLVE"
    public long IncidentId { get; set; }
    public string ExpectedRowVersion { get; set; } = string.Empty;
    public Dictionary<string, object?> CommandParams { get; set; } = [];
}

public class CommandResultPayload
{
    public string CommandId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int StatusCode { get; set; } = 200; // 200 OK, 409 Conflict, 403 Forbidden
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? UpdatedRowVersionHex { get; set; }
}

public class ErrorPayload
{
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string? Details { get; set; }
}
