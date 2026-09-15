using System.Text.Json;
using LabGuard.Protocol.Dtos;
using LabGuard.Protocol.Serialization;
using Xunit;

namespace LabGuard.Protocol.Tests;

public class ProtocolSpecificationTests
{
    [Fact]
    public void MessageTypes_CatalogHasExactly69CanonicalMessages()
    {
        Assert.Equal(69, MessageTypes.All.Count);
    }

    [Fact]
    public void MessageTypes_P0RequiredMessages_AreAllValid()
    {
        string[] p0Required = [
            MessageTypes.HelloAgent, MessageTypes.HelloAdmin, MessageTypes.HelloAck,
            MessageTypes.AuthAgent, MessageTypes.AuthResult,
            MessageTypes.Heartbeat, MessageTypes.HeartbeatAck,
            MessageTypes.MetricBatch, MessageTypes.BatchAck, MessageTypes.ServiceStatus,
            MessageTypes.AgentGoodbye, MessageTypes.GoodbyeAck,
            MessageTypes.Login, MessageTypes.LoginResult, MessageTypes.Logout, MessageTypes.LogoutResult,
            MessageTypes.ListDevices, MessageTypes.DevicePage, MessageTypes.ListIncidents, MessageTypes.IncidentPage,
            MessageTypes.SubscribeDevices, MessageTypes.SubscribeIncidents,
            MessageTypes.SnapshotBegin, MessageTypes.SnapshotPage, MessageTypes.SnapshotEnd,
            MessageTypes.DeviceUpdate, MessageTypes.IncidentOpen, MessageTypes.IncidentUpdate,
            MessageTypes.IncidentAck, MessageTypes.IncidentAssign, MessageTypes.IncidentStart, MessageTypes.IncidentResolve,
            MessageTypes.CommandResult, MessageTypes.Error
        ];

        Assert.Equal(34, p0Required.Length);
        foreach (var msg in p0Required)
        {
            Assert.True(MessageTypes.IsValid(msg), $"P0 Message '{msg}' must be valid in catalog.");
        }
    }

    [Fact]
    public void WireEnvelope_RoundTripSerialization_PreservesAllFields()
    {
        var heartbeat = new HeartbeatPayload
        {
            SequenceNumber = 42,
            UptimeSeconds = 3600
        };

        var envelope = WireEnvelope<HeartbeatPayload>.Create(
            MessageTypes.Heartbeat,
            "TEST-DEVICE-01",
            heartbeat,
            "req-123456"
        );

        byte[] bytes = ProtocolSerializer.Serialize(envelope);
        Assert.NotEmpty(bytes);

        var deserialized = ProtocolSerializer.DeserializeEnvelope(bytes);
        Assert.NotNull(deserialized);
        Assert.Equal(1, deserialized.Version);
        Assert.Equal(MessageTypes.Heartbeat, deserialized.MessageType);
        Assert.Equal("TEST-DEVICE-01", deserialized.DeviceId);
        Assert.Equal("req-123456", deserialized.RequestId);

        var payload = ProtocolSerializer.ExtractPayload<HeartbeatPayload>(deserialized.Payload);
        Assert.NotNull(payload);
        Assert.Equal(42, payload.SequenceNumber);
        Assert.Equal(3600, payload.UptimeSeconds);
    }

    [Fact]
    public void RequestDigest_KnownVectorTest_MatchesIndependentlyCalculatedSha256()
    {
        // Independent test vector construction:
        // INCIDENT_ACK|101|0x0000000000000005|null||550e8400-e29b-41d4-a716-446655440000
        string independentCanonical = "INCIDENT_ACK|101|0x0000000000000005|null||550e8400-e29b-41d4-a716-446655440000";
        byte[] expectedHash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(independentCanonical));
        string expectedDigest = Convert.ToHexString(expectedHash).ToLowerInvariant();

        string actualDigest = ProtocolSerializer.ComputeRequestDigest(
            commandType: "INCIDENT_ACK",
            incidentId: 101,
            expectedRowVersion: "0x0000000000000005",
            assignedToUserId: null,
            resolutionNote: null,
            commandId: "550e8400-e29b-41d4-a716-446655440000"
        );

        Assert.Equal(64, actualDigest.Length);
        Assert.Equal(expectedDigest, actualDigest);
    }

    [Fact]
    public void RequestDigest_Normalizations_AreConsistent()
    {
        string baseDigest = ProtocolSerializer.ComputeRequestDigest(
            commandType: "INCIDENT_ACK",
            incidentId: 101,
            expectedRowVersion: "0x0000000000000005",
            assignedToUserId: null,
            resolutionNote: "",
            commandId: "550e8400-e29b-41d4-a716-446655440000"
        );

        // 1. Lowercase commandType normalizes to uppercase
        string lowercaseTypeDigest = ProtocolSerializer.ComputeRequestDigest(
            commandType: "incident_ack",
            incidentId: 101,
            expectedRowVersion: "0x0000000000000005",
            assignedToUserId: null,
            resolutionNote: null,
            commandId: "550e8400-e29b-41d4-a716-446655440000"
        );
        Assert.Equal(baseDigest, lowercaseTypeDigest);

        // 2. Uppercase commandId normalizes to lowercase
        string uppercaseIdDigest = ProtocolSerializer.ComputeRequestDigest(
            commandType: "INCIDENT_ACK",
            incidentId: 101,
            expectedRowVersion: "0x0000000000000005",
            assignedToUserId: null,
            resolutionNote: null,
            commandId: "550E8400-E29B-41D4-A716-446655440000"
        );
        Assert.Equal(baseDigest, uppercaseIdDigest);

        // 3. Trim whitespace on resolutionNote
        string withSpaces = ProtocolSerializer.ComputeRequestDigest(
            commandType: "INCIDENT_RESOLVE",
            incidentId: 101,
            expectedRowVersion: "0x0000000000000005",
            assignedToUserId: 5,
            resolutionNote: "  resolved issue  ",
            commandId: "550e8400-e29b-41d4-a716-446655440000"
        );
        string withoutSpaces = ProtocolSerializer.ComputeRequestDigest(
            commandType: "INCIDENT_RESOLVE",
            incidentId: 101,
            expectedRowVersion: "0x0000000000000005",
            assignedToUserId: 5,
            resolutionNote: "resolved issue",
            commandId: "550e8400-e29b-41d4-a716-446655440000"
        );
        Assert.Equal(withoutSpaces, withSpaces);

        // 4. Changed resolutionNote changes digest
        string differentNote = ProtocolSerializer.ComputeRequestDigest(
            commandType: "INCIDENT_RESOLVE",
            incidentId: 101,
            expectedRowVersion: "0x0000000000000005",
            assignedToUserId: 5,
            resolutionNote: "different note",
            commandId: "550e8400-e29b-41d4-a716-446655440000"
        );
        Assert.NotEqual(withSpaces, differentNote);
    }

    [Fact]
    public void EnvelopeValidator_IsCanonicalUuidV4_EnforcesStrictRfc4122V4()
    {
        // 1. Valid UUIDv4 generated by .NET -> PASS
        string validV4 = Guid.NewGuid().ToString("D");
        Assert.True(EnvelopeValidator.IsCanonicalUuidV4(validV4));

        // 2. Known UUIDv1 (DNS namespace UUID) -> REJECT
        string uuidV1 = "6ba7b810-9dad-11d1-80b4-00c04fd430c8";
        Assert.False(EnvelopeValidator.IsCanonicalUuidV4(uuidV1));

        // 3. Nil/Empty GUID (version 0) -> REJECT
        string nilGuid = "00000000-0000-0000-0000-000000000000";
        Assert.False(EnvelopeValidator.IsCanonicalUuidV4(nilGuid));

        // 4. Malformed strings -> REJECT
        Assert.False(EnvelopeValidator.IsCanonicalUuidV4(null));
        Assert.False(EnvelopeValidator.IsCanonicalUuidV4(""));
        Assert.False(EnvelopeValidator.IsCanonicalUuidV4("not-a-guid"));
        Assert.False(EnvelopeValidator.IsCanonicalUuidV4("550e8400-e29b-41d4-a716"));
        Assert.False(EnvelopeValidator.IsCanonicalUuidV4("{550e8400-e29b-41d4-a716-446655440000}"));
    }

    [Fact]
    public void EnvelopeValidator_SentAtUtc_EnforcesZeroOffset()
    {
        var validPayload = new HeartbeatPayload { SequenceNumber = 1, UptimeSeconds = 10 };
        string v4Id = Guid.NewGuid().ToString("D");

        // 1. UTC with 'Z' suffix -> PASS
        var envelopeUtcZ = WireEnvelope<HeartbeatPayload>.Create(
            MessageTypes.Heartbeat, "DEV01", validPayload, v4Id);
        envelopeUtcZ.SentAtUtc = "2026-09-14T14:00:00Z";
        byte[] bytesZ = ProtocolSerializer.Serialize(envelopeUtcZ);
        var parsedZ = ProtocolSerializer.DeserializeEnvelope(bytesZ);
        Assert.True(EnvelopeValidator.Validate(parsedZ).IsValid);

        // 2. UTC with '+00:00' suffix -> PASS
        var envelopeOffset0 = WireEnvelope<HeartbeatPayload>.Create(
            MessageTypes.Heartbeat, "DEV01", validPayload, v4Id);
        envelopeOffset0.SentAtUtc = "2026-09-14T14:00:00+00:00";
        byte[] bytesOffset0 = ProtocolSerializer.Serialize(envelopeOffset0);
        var parsedOffset0 = ProtocolSerializer.DeserializeEnvelope(bytesOffset0);
        Assert.True(EnvelopeValidator.Validate(parsedOffset0).IsValid);

        // 3. Non-zero offset (+07:00) -> REJECT with InvalidPayload
        var envelopeOffset7 = WireEnvelope<HeartbeatPayload>.Create(
            MessageTypes.Heartbeat, "DEV01", validPayload, v4Id);
        envelopeOffset7.SentAtUtc = "2026-09-14T14:00:00+07:00";
        byte[] bytesOffset7 = ProtocolSerializer.Serialize(envelopeOffset7);
        var parsedOffset7 = ProtocolSerializer.DeserializeEnvelope(bytesOffset7);
        var result7 = EnvelopeValidator.Validate(parsedOffset7);
        Assert.False(result7.IsValid);
        Assert.Equal(ErrorCodes.InvalidPayload, result7.ErrorCode);
    }
}
