using System.Text.Json.Serialization;

namespace LabGuard.Protocol;

/// <summary>
/// Root envelope containing the standardized 6 fields for all LabGuard wire messages.
/// </summary>
public class WireEnvelope<T>
{
    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("messageType")]
    public string MessageType { get; set; } = string.Empty;

    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = string.Empty;

    [JsonPropertyName("deviceId")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("sentAtUtc")]
    public string SentAtUtc { get; set; } = string.Empty;

    [JsonPropertyName("payload")]
    public T Payload { get; set; } = default!;

    public static WireEnvelope<T> Create(string messageType, string deviceId, T payload, string? requestId = null)
    {
        return new WireEnvelope<T>
        {
            Version = 1,
            MessageType = messageType,
            DeviceId = deviceId,
            RequestId = requestId ?? Guid.NewGuid().ToString("D"),
            SentAtUtc = DateTime.UtcNow.ToString("O"),
            Payload = payload
        };
    }
}
