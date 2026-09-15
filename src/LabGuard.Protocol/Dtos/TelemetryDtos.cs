namespace LabGuard.Protocol.Dtos;

public class HeartbeatPayload
{
    public long SequenceNumber { get; set; }
    public long UptimeSeconds { get; set; }
}

public class HeartbeatAckPayload
{
    public string ServerTimeUtc { get; set; } = DateTime.UtcNow.ToString("O");
    public long AcknowledgedSequence { get; set; }
}

public class MetricSampleDto
{
    public string MetricName { get; set; } = string.Empty; // "CPU", "RAM", "NIC" (Disk excluded in P0)
    public double MetricValue { get; set; }
    public string Unit { get; set; } = "%"; // "%", "MB", "KB/s"
    public string RecordedAtUtc { get; set; } = DateTime.UtcNow.ToString("O");
}

public class MetricBatchPayload
{
    public string BootId { get; set; } = Guid.NewGuid().ToString("D");
    public string BatchId { get; set; } = Guid.NewGuid().ToString("D");
    public int SampleCount { get; set; }
    public long UptimeSeconds { get; set; }
    public bool IsReplay { get; set; }
    public List<MetricSampleDto> Samples { get; set; } = [];
}

public class BatchAckPayload
{
    public string BootId { get; set; } = string.Empty;
    public string BatchId { get; set; } = string.Empty;
    public bool Accepted { get; set; }
    public int SamplesRecorded { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ServiceStatusItemDto
{
    public string ServiceName { get; set; } = string.Empty;
    public int Port { get; set; }
    public bool IsListening { get; set; }
    public int ResponseTimeMs { get; set; }
    public string LastCheckedUtc { get; set; } = DateTime.UtcNow.ToString("O");
}

public class ServiceStatusPayload
{
    public bool IsReplay { get; set; }
    public List<ServiceStatusItemDto> Services { get; set; } = [];
}
