using System.Globalization;
using System.Text.Json;

namespace LabGuard.Protocol;

public class EnvelopeValidationResult
{
    public bool IsValid { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    private EnvelopeValidationResult(bool isValid, string? errorCode, string? errorMessage)
    {
        IsValid = isValid;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public static EnvelopeValidationResult Success() => new(true, null, null);
    public static EnvelopeValidationResult Failure(string errorCode, string errorMessage) => new(false, errorCode, errorMessage);
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

    public static EnvelopeValidationResult Validate(WireEnvelope<JsonElement>? envelope)
    {
        if (envelope == null)
        {
            return EnvelopeValidationResult.Failure(ErrorCodes.InvalidPayload, "Envelope is null.");
        }

        // 1. Version must be exactly 1
        if (envelope.Version != 1)
        {
            return EnvelopeValidationResult.Failure(ErrorCodes.InvalidPayload, $"Unsupported envelope version: {envelope.Version}. Expected version 1.");
        }

        // 2. MessageType must be non-empty and in canonical catalog
        if (string.IsNullOrWhiteSpace(envelope.MessageType))
        {
            return EnvelopeValidationResult.Failure(ErrorCodes.InvalidPayload, "Envelope messageType cannot be empty.");
        }

        if (!MessageTypes.IsValid(envelope.MessageType))
        {
            return EnvelopeValidationResult.Failure(ErrorCodes.UnknownMessageType, $"Unknown messageType '{envelope.MessageType}'.");
        }

        // 3. RequestId must be a non-empty, semantically valid canonical UUIDv4 (36 chars)
        if (string.IsNullOrWhiteSpace(envelope.RequestId))
        {
            return EnvelopeValidationResult.Failure(ErrorCodes.InvalidPayload, "Envelope requestId cannot be empty.");
        }

        if (!IsCanonicalUuidV4(envelope.RequestId))
        {
            return EnvelopeValidationResult.Failure(ErrorCodes.InvalidPayload, $"Envelope requestId '{envelope.RequestId}' is not a valid UUIDv4.");
        }

        // 4. DeviceId must be non-empty
        if (string.IsNullOrWhiteSpace(envelope.DeviceId))
        {
            return EnvelopeValidationResult.Failure(ErrorCodes.InvalidPayload, "Envelope deviceId cannot be empty.");
        }

        // 5. SentAtUtc must be a valid ISO-8601 UTC timestamp with zero offset (Z or +00:00)
        if (string.IsNullOrWhiteSpace(envelope.SentAtUtc))
        {
            return EnvelopeValidationResult.Failure(ErrorCodes.InvalidPayload, "Envelope sentAtUtc cannot be empty.");
        }

        if (!DateTimeOffset.TryParse(envelope.SentAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedDto))
        {
            return EnvelopeValidationResult.Failure(ErrorCodes.InvalidPayload, $"Envelope sentAtUtc '{envelope.SentAtUtc}' is not a valid ISO-8601 timestamp.");
        }

        if (parsedDto.Offset != TimeSpan.Zero)
        {
            return EnvelopeValidationResult.Failure(ErrorCodes.InvalidPayload, $"Envelope sentAtUtc '{envelope.SentAtUtc}' must have zero offset (UTC), but got offset '{parsedDto.Offset}'.");
        }

        // 6. Payload must be present (not Undefined or Null)
        if (envelope.Payload.ValueKind == JsonValueKind.Undefined || envelope.Payload.ValueKind == JsonValueKind.Null)
        {
            return EnvelopeValidationResult.Failure(ErrorCodes.InvalidPayload, "Envelope payload cannot be null or undefined.");
        }

        return EnvelopeValidationResult.Success();
    }
}
