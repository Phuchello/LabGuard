using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LabGuard.Protocol.Serialization;

public static class ProtocolSerializer
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public static byte[] Serialize<T>(T value)
    {
        return JsonSerializer.SerializeToUtf8Bytes(value, Options);
    }

    public static string SerializeToString<T>(T value)
    {
        return JsonSerializer.Serialize(value, Options);
    }

    public static T? Deserialize<T>(ReadOnlySpan<byte> utf8Bytes)
    {
        return JsonSerializer.Deserialize<T>(utf8Bytes, Options);
    }

    public static T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, Options);
    }

    public static WireEnvelope<JsonElement>? DeserializeEnvelope(ReadOnlySpan<byte> utf8Bytes)
    {
        return JsonSerializer.Deserialize<WireEnvelope<JsonElement>>(utf8Bytes, Options);
    }

    public static T? ExtractPayload<T>(JsonElement element)
    {
        return JsonSerializer.Deserialize<T>(element.GetRawText(), Options);
    }

    /// <summary>
    /// Computes deterministic SHA-256 RequestDigest per CP2 frozen specification:
    /// Canonical format: commandType|incidentId|expectedRowVersion|assignedToUserId|resolutionNote|commandId
    /// Returns 64-character lowercase hex string.
    /// </summary>
    public static string ComputeRequestDigest(
        string commandType,
        long incidentId,
        string expectedRowVersion,
        int? assignedToUserId,
        string? resolutionNote,
        string commandId)
    {
        string normCommandType = commandType.Trim().ToUpperInvariant();
        string normIncidentId = incidentId.ToString(System.Globalization.CultureInfo.InvariantCulture);

        // Normalize expectedRowVersion: 0x + 16 hex digits in lowercase
        string cleanRowVer = expectedRowVersion.Trim().ToLowerInvariant();
        if (cleanRowVer.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            cleanRowVer = cleanRowVer[2..];
        }
        string normRowVersion = "0x" + cleanRowVer.PadLeft(16, '0');

        string normAssignedToUserId = assignedToUserId.HasValue
            ? assignedToUserId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : "null";

        string normResolutionNote = resolutionNote != null ? resolutionNote.Trim() : string.Empty;

        // Normalize commandId as canonical UUID lowercase "D" format
        string normCommandId = Guid.TryParse(commandId, out var parsedGuid)
            ? parsedGuid.ToString("D").ToLowerInvariant()
            : commandId.Trim().ToLowerInvariant();

        string canonicalString = $"{normCommandType}|{normIncidentId}|{normRowVersion}|{normAssignedToUserId}|{normResolutionNote}|{normCommandId}";
        byte[] canonicalBytes = Encoding.UTF8.GetBytes(canonicalString);
        byte[] hash = SHA256.HashData(canonicalBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
