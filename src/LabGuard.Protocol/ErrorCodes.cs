namespace LabGuard.Protocol;

/// <summary>
/// Standard Error Codes for LabGuard Wire Protocol.
/// </summary>
public static class ErrorCodes
{
    public const string ConditionStillActive = "ERR_CONDITION_STILL_ACTIVE";
    public const string ConcurrencyViolation = "ERR_CONCURRENCY_VIOLATION";
    public const string IdempotencyKeyReuse = "ERR_IDEMPOTENCY_KEY_REUSE";
    public const string DatabaseUnavailable = "ERR_DATABASE_UNAVAILABLE";
    public const string Unauthorized = "ERR_UNAUTHORIZED";
    public const string Unauthenticated = "ERR_UNAUTHENTICATED";
    public const string InvalidPayload = "ERR_INVALID_PAYLOAD";
    public const string FrameTooLarge = "ERR_FRAME_TOO_LARGE";
    public const string FrameZeroLength = "ERR_FRAME_ZERO_LENGTH";
    public const string UnknownMessageType = "ERR_UNKNOWN_MESSAGE_TYPE";
    public const string DeviceNotFound = "ERR_DEVICE_NOT_FOUND";
    public const string IncidentNotFound = "ERR_INCIDENT_NOT_FOUND";
    public const string InviteInvalid = "ERR_INVITE_INVALID";
    public const string InternalError = "ERR_INTERNAL";
}
