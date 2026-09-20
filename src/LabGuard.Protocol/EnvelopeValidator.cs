using System.Globalization;
using System.Text.Json;

namespace LabGuard.Protocol;

public class EnvelopeValidationResult
{
    public bool IsValid { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
    public string? RequestId { get; }
    public string? MessageType { get; }

    private EnvelopeValidationResult(bool isValid, string? errorCode, string? errorMessage, string? requestId = null, string? messageType = null)
    {
        IsValid = isValid;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        RequestId = requestId;
        MessageType = messageType;
    }

    public static EnvelopeValidationResult Success(string? requestId = null, string? messageType = null) => new(true, null, null, requestId, messageType);
    public static EnvelopeValidationResult Failure(string errorCode, string errorMessage, string? requestId = null, string? messageType = null) => new(false, errorCode, errorMessage, requestId, messageType);
}

/// <summary>
/// Centralized strict validator for LabGuard Wire Envelopes according to 03_PROTOCOL_SPEC.md.
/// </summary>
public static class EnvelopeValidator
{
    /// <summary>
    /// Validates that a string is a canonical RFC 4122 UUID version 4 in 36-character hyphenated "D" format.
    /// Format: xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx where y is 8, 9, a, or b (case-insensitive).
    /// </summary>
    public static bool IsCanonicalUuidV4(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 36)
        {
            return false;
        }

        if (!Guid.TryParseExact(value, "D", out _))
        {
            return false;
        }

        // Index 14 is the version digit (position 13 in hex string): must be '4'
        if (value[14] != '4')
        {
            return false;
        }

        // Index 19 is the variant digit: RFC 4122 variant specifies high bits '10xx', which corresponds to 8, 9, a, b (or A, B)
        char variant = value[19];
        if (variant is not ('8' or '9' or 'a' or 'b' or 'A' or 'B'))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates an envelope instance against the wire contract.
    /// Returns an EnvelopeValidationResult indicating success or specific error code and message.
    /// </summary>
    public static EnvelopeValidationResult Validate(WireEnvelope<JsonElement>? envelope)
    {
        if (envelope == null)
        {
            return EnvelopeValidationResult.Failure(ErrorCodes.InvalidPayload, "Envelope cannot be null.");
        }

        string? reqId = IsCanonicalUuidV4(envelope.RequestId) ? envelope.RequestId : null;
        string? msgType = envelope.MessageType;

        // 1. Version must be exactly 1
        if (envelope.Version != 1)
        {
            return EnvelopeValidationResult.Failure(ErrorCodes.InvalidVersion, $"Unsupported envelope version: {envelope.Version}. Expected version 1.", reqId, msgType);
        }

        // 2. MessageType must be non-empty and in canonical catalog
        if (string.IsNullOrWhiteSpace(envelope.MessageType))
        {
            return EnvelopeValidationResult.Failure(ErrorCodes.InvalidPayload, "Envelope messageType cannot be empty.", reqId, msgType);
        }

        if (!MessageTypes.IsValid(envelope.MessageType))
        {
            return EnvelopeValidationResult.Failure(ErrorCodes.UnknownMessageType, $"Unknown messageType '{envelope.MessageType}'.", reqId, msgType);
        }

        // 3. RequestId must be a non-empty, semantically valid canonical UUIDv4 (36 chars)
        if (string.IsNullOrWhiteSpace(envelope.RequestId))
        {
            return EnvelopeValidationResult.Failure(ErrorCodes.InvalidPayload, "Envelope requestId cannot be empty.", null, msgType);
        }

        if (!IsCanonicalUuidV4(envelope.RequestId))
        {
            return EnvelopeValidationResult.Failure(ErrorCodes.InvalidPayload, $"Envelope requestId '{envelope.RequestId}' is not a valid UUIDv4.", null, msgType);
        }

        // 4. DeviceId must be non-empty
        if (string.IsNullOrWhiteSpace(envelope.DeviceId))
        {
            return EnvelopeValidationResult.Failure(ErrorCodes.InvalidPayload, "Envelope deviceId cannot be empty.", reqId, msgType);
        }

        // 5. SentAtUtc must be a valid ISO-8601 UTC timestamp with explicit zero offset (Z or +00:00)
        if (string.IsNullOrWhiteSpace(envelope.SentAtUtc))
        {
            return EnvelopeValidationResult.Failure(ErrorCodes.InvalidPayload, "Envelope sentAtUtc cannot be empty.", reqId, msgType);
        }

        string rawSentAt = envelope.SentAtUtc.Trim();
        bool hasExplicitUtc = rawSentAt.EndsWith('Z') ||
                              rawSentAt.EndsWith("+00:00", StringComparison.Ordinal);

        if (!hasExplicitUtc)
        {
            return EnvelopeValidationResult.Failure(ErrorCodes.InvalidPayload,
                $"Envelope sentAtUtc '{envelope.SentAtUtc}' must specify an explicit UTC designator ('Z' or '+00:00').", reqId, msgType);
        }

        if (!DateTimeOffset.TryParse(envelope.SentAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedDto))
        {
            return EnvelopeValidationResult.Failure(ErrorCodes.InvalidPayload, $"Envelope sentAtUtc '{envelope.SentAtUtc}' is not a valid ISO-8601 timestamp.", reqId, msgType);
        }

        if (parsedDto.Offset != TimeSpan.Zero)
        {
            return EnvelopeValidationResult.Failure(ErrorCodes.InvalidPayload, $"Envelope sentAtUtc '{envelope.SentAtUtc}' must have zero offset (UTC), but got offset '{parsedDto.Offset}'.", reqId, msgType);
        }

        // 6. Payload must be a JSON object (reject string, number, boolean, array, null, undefined)
        if (envelope.Payload.ValueKind != JsonValueKind.Object)
        {
            return EnvelopeValidationResult.Failure(ErrorCodes.InvalidPayload, $"Envelope payload must be a JSON object, but was {envelope.Payload.ValueKind}.", reqId, msgType);
        }

        return EnvelopeValidationResult.Success(reqId, msgType);
    }

    /// <summary>
    /// Validates raw UTF-8 JSON bytes against the immutable wire contract and parses the envelope:
    /// - Must be valid JSON (not malformed)
    /// - Root must be a JSON object
    /// - Outer envelope must contain EXACTLY 6 canonical fields: version, messageType, requestId, deviceId, sentAtUtc, payload
    /// - Canonical names are strictly CASE-SENSITIVE
    /// - Prohibits duplicate fields and extra unauthorized fields
    /// - Payload must be a JSON object (JsonValueKind.Object)
    /// - Version != 1 returns ERR_INVALID_VERSION
    /// </summary>
    public static (EnvelopeValidationResult Result, WireEnvelope<JsonElement>? Envelope) ValidateAndParse(ReadOnlyMemory<byte> utf8Json)
    {
        try
        {
            using var doc = JsonDocument.Parse(utf8Json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return (EnvelopeValidationResult.Failure(ErrorCodes.InvalidPayload, "Envelope root must be a JSON object."), null);
            }

            // Extract valid UUIDv4 requestId early for correlation if present
            string? extractedRequestId = null;
            if (doc.RootElement.TryGetProperty("requestId", out var reqProp) &&
                reqProp.ValueKind == JsonValueKind.String)
            {
                string rawReqId = reqProp.GetString() ?? string.Empty;
                if (IsCanonicalUuidV4(rawReqId))
                {
                    extractedRequestId = rawReqId;
                }
            }

            string? extractedMessageType = null;
            if (doc.RootElement.TryGetProperty("messageType", out var typeProp) &&
                typeProp.ValueKind == JsonValueKind.String)
            {
                extractedMessageType = typeProp.GetString();
            }

            var canonicalFields = new HashSet<string>(StringComparer.Ordinal)
            {
                "version", "messageType", "requestId", "deviceId", "sentAtUtc", "payload"
            };

            var seenFields = new HashSet<string>(StringComparer.Ordinal);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (!canonicalFields.Contains(prop.Name))
                {
                    return (EnvelopeValidationResult.Failure(ErrorCodes.InvalidPayload,
                        $"Outer envelope contains unauthorized or incorrectly-cased field: '{prop.Name}'. Envelope must contain exactly the 6 standardized fields in exact case.", extractedRequestId, extractedMessageType), null);
                }

                if (!seenFields.Add(prop.Name))
                {
                    return (EnvelopeValidationResult.Failure(ErrorCodes.InvalidPayload,
                        $"Outer envelope contains duplicate field: '{prop.Name}'. Duplicate fields are strictly prohibited.", extractedRequestId, extractedMessageType), null);
                }
            }

            foreach (var canonical in canonicalFields)
            {
                if (!seenFields.Contains(canonical))
                {
                    return (EnvelopeValidationResult.Failure(ErrorCodes.InvalidPayload,
                        $"Outer envelope is missing required field: '{canonical}'.", extractedRequestId, extractedMessageType), null);
                }
            }

            // Check version directly in raw JSON to return canonical ERR_INVALID_VERSION
            var versionProp = doc.RootElement.GetProperty("version");
            if (versionProp.ValueKind != JsonValueKind.Number || !versionProp.TryGetInt32(out var verInt) || verInt != 1)
            {
                string verStr = versionProp.ValueKind == JsonValueKind.Number ? versionProp.GetRawText() : versionProp.ToString();
                return (EnvelopeValidationResult.Failure(ErrorCodes.InvalidVersion,
                    $"Unsupported envelope version: {verStr}. Expected version 1.", extractedRequestId, extractedMessageType), null);
            }

            // Check payload directly in raw JSON: must be JsonValueKind.Object
            var payloadProp = doc.RootElement.GetProperty("payload");
            if (payloadProp.ValueKind != JsonValueKind.Object)
            {
                return (EnvelopeValidationResult.Failure(ErrorCodes.InvalidPayload,
                    $"Envelope payload must be a JSON object, but was {payloadProp.ValueKind}.", extractedRequestId, extractedMessageType), null);
            }

            var envelope = Serialization.ProtocolSerializer.DeserializeEnvelope(utf8Json.Span);
            var validation = Validate(envelope!);
            if (!validation.IsValid)
            {
                return (validation, envelope);
            }

            return (EnvelopeValidationResult.Success(extractedRequestId, extractedMessageType), envelope);
        }
        catch (JsonException ex)
        {
            return (EnvelopeValidationResult.Failure(ErrorCodes.MalformedJson, $"Malformed JSON: {ex.Message}"), null);
        }
    }

    public static EnvelopeValidationResult ValidateRawJson(ReadOnlyMemory<byte> utf8Json)
    {
        var (result, _) = ValidateAndParse(utf8Json);
        return result;
    }

    public static EnvelopeValidationResult ValidateRawJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return EnvelopeValidationResult.Failure(ErrorCodes.InvalidPayload, "JSON string is null or empty.");
        }

        return ValidateRawJson((ReadOnlyMemory<byte>)System.Text.Encoding.UTF8.GetBytes(json));
    }
}
