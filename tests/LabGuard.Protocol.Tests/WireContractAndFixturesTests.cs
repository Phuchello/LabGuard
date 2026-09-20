using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using LabGuard.Protocol.Dtos;
using LabGuard.Protocol.Serialization;
using LabGuard.Protocol.Tests.Fixtures;
using Xunit;

namespace LabGuard.Protocol.Tests;

/// <summary>
/// Comprehensive automated test suite for LG-A-001 — Wire Contract & Protocol Fixtures.
/// Proves all 8 contract requirements:
/// 1. Normal single frame round-trip
/// 2. 4-byte prefix arriving fragmented
/// 3. JSON payload arriving fragmented
/// 4. Multiple frames concatenated in one underlying stream
/// 5. Payload > 262,144 bytes rejected
/// 6. Malformed JSON rejected
/// 7. Envelope missing required field rejected
/// 8. Wrong protocol version rejected
/// Plus fixture file integrity and minification checks.
/// </summary>
public class WireContractAndFixturesTests
{
    // =========================================================================
    // 1. NORMAL SINGLE FRAME ROUND-TRIP
    // =========================================================================

    [Fact]
    public async Task Requirement1_NormalSingleFrameRoundTrip_HeartbeatFixture()
    {
        var envelope = ProtocolFixtures.CreateHeartbeatEnvelope();
        byte[] framed = FrameCodec.Encode(envelope);

        // Verify framing: 4-byte Big-Endian length prefix
        Assert.True(framed.Length > FrameCodec.HeaderSize);
        uint declaredLength = BinaryPrimitives.ReadUInt32BigEndian(framed.AsSpan(0, 4));
        Assert.Equal((uint)(framed.Length - FrameCodec.HeaderSize), declaredLength);

        // Read from stream
        using var stream = new MemoryStream(framed);
        byte[]? readPayload = await FrameCodec.ReadFrameAsync(stream);
        Assert.NotNull(readPayload);
        Assert.Equal(declaredLength, (uint)readPayload.Length);

        // Deserialize envelope and payload
        var deserialized = ProtocolSerializer.DeserializeEnvelope(readPayload);
        Assert.NotNull(deserialized);
        Assert.Equal(1, deserialized.Version);
        Assert.Equal(MessageTypes.Heartbeat, deserialized.MessageType);
        Assert.Equal("PC-LAB1-01", deserialized.DeviceId);

        var payload = ProtocolSerializer.ExtractPayload<HeartbeatPayload>(deserialized.Payload);
        Assert.NotNull(payload);
        Assert.Equal(1, payload.SequenceNumber);
        Assert.Equal(3600, payload.UptimeSeconds);
    }

    [Fact]
    public async Task Requirement1_NormalSingleFrameRoundTrip_MetricBatchFixture()
    {
        var envelope = ProtocolFixtures.CreateMetricBatchEnvelope();
        byte[] framed = FrameCodec.Encode(envelope);

        using var stream = new MemoryStream(framed);
        var decodedEnvelope = await FrameCodec.ReadEnvelopeAsync(stream);

        Assert.NotNull(decodedEnvelope);
        Assert.Equal(1, decodedEnvelope.Version);
        Assert.Equal(MessageTypes.MetricBatch, decodedEnvelope.MessageType);

        var payload = ProtocolSerializer.ExtractPayload<MetricBatchPayload>(decodedEnvelope.Payload);
        Assert.NotNull(payload);
        Assert.Equal("b0000000-0000-4000-8000-000000000001", payload.BootId);
        Assert.Equal("b0000000-0000-4000-8000-000000000002", payload.BatchId);
        Assert.Equal(2, payload.SampleCount);
        Assert.Equal(2, payload.Samples.Count);
    }

    [Fact]
    public async Task Requirement1_NormalSingleFrameRoundTrip_LoginFixture()
    {
        var envelope = ProtocolFixtures.CreateLoginEnvelope();
        byte[] framed = FrameCodec.Encode(envelope);

        using var stream = new MemoryStream(framed);
        var decodedEnvelope = await FrameCodec.ReadEnvelopeAsync(stream);

        Assert.NotNull(decodedEnvelope);
        Assert.Equal(1, decodedEnvelope.Version);
        Assert.Equal(MessageTypes.Login, decodedEnvelope.MessageType);

        var payload = ProtocolSerializer.ExtractPayload<LoginPayload>(decodedEnvelope.Payload);
        Assert.NotNull(payload);
        Assert.Equal("admin", payload.Username);
        Assert.Equal("AdminPassword123!", payload.Password);
    }

    [Fact]
    public async Task Requirement1_NormalSingleFrameRoundTrip_IncidentAckFixture()
    {
        var envelope = ProtocolFixtures.CreateIncidentAckEnvelope();
        byte[] framed = FrameCodec.Encode(envelope);

        using var stream = new MemoryStream(framed);
        var decodedEnvelope = await FrameCodec.ReadEnvelopeAsync(stream);

        Assert.NotNull(decodedEnvelope);
        Assert.Equal(1, decodedEnvelope.Version);
        Assert.Equal(MessageTypes.IncidentAck, decodedEnvelope.MessageType);

        var payload = ProtocolSerializer.ExtractPayload<IncidentCommandPayload>(decodedEnvelope.Payload);
        Assert.NotNull(payload);
        Assert.Equal(MessageTypes.IncidentAck, payload.CommandType);
        Assert.Equal(1, payload.IncidentId);
        Assert.Equal("0x0000000000000001", payload.ExpectedRowVersion);
    }

    [Fact]
    public void Requirement1_BufferDecode_MatchesBufferEncode()
    {
        byte[] originalJson = ProtocolFixtures.HeartbeatBytes;
        byte[] framed = FrameCodec.Encode(originalJson);
        byte[] decoded = FrameCodec.Decode(framed);

        Assert.Equal(originalJson, decoded);
    }

    // =========================================================================
    // 2. 4-BYTE PREFIX ARRIVING FRAGMENTED
    // =========================================================================

    [Theory]
    [InlineData(new int[] { 1, 1, 1, 1 })] // 1 byte at a time
    [InlineData(new int[] { 1, 3 })]       // 1 byte then 3 bytes
    [InlineData(new int[] { 2, 2 })]       // 2 bytes then 2 bytes
    [InlineData(new int[] { 3, 1 })]       // 3 bytes then 1 byte
    public async Task Requirement2_FragmentedPrefix_ReassemblesCorrectly(int[] headerChunks)
    {
        byte[] framed = ProtocolFixtures.HeartbeatFramed;
        int payloadLength = framed.Length - FrameCodec.HeaderSize;

        // Combine header chunks with full payload as the final chunk
        var allChunks = headerChunks.Concat(new[] { payloadLength }).ToArray();
        using var chunkedStream = new ChunkedStream(framed, allChunks);

        byte[]? result = await FrameCodec.ReadFrameAsync(chunkedStream);
        Assert.NotNull(result);
        Assert.Equal(ProtocolFixtures.HeartbeatBytes, result);
    }

    // =========================================================================
    // 3. JSON PAYLOAD ARRIVING FRAGMENTED
    // =========================================================================

    [Fact]
    public async Task Requirement3_FragmentedPayload_ByteByByte_ReassemblesCorrectly()
    {
        byte[] framed = ProtocolFixtures.LoginFramed;
        int payloadLength = framed.Length - FrameCodec.HeaderSize;

        // Header in 1 chunk (4 bytes), then payload 1 byte at a time
        var chunks = new List<int> { 4 };
        for (int i = 0; i < payloadLength; i++)
        {
            chunks.Add(1);
        }

        using var chunkedStream = new ChunkedStream(framed, chunks.ToArray());
        byte[]? result = await FrameCodec.ReadFrameAsync(chunkedStream);

        Assert.NotNull(result);
        Assert.Equal(ProtocolFixtures.LoginBytes, result);
    }

    [Fact]
    public async Task Requirement3_FragmentedPayload_ArbitrarySmallChunks_ReassemblesCorrectly()
    {
        byte[] framed = ProtocolFixtures.MetricBatchFramed;
        int payloadLength = framed.Length - FrameCodec.HeaderSize;

        // Header (4b), then payload in uneven chunks (7b, 13b, 29b, remaining)
        int chunk1 = 7;
        int chunk2 = 13;
        int chunk3 = 29;
        int chunk4 = payloadLength - chunk1 - chunk2 - chunk3;

        using var chunkedStream = new ChunkedStream(framed, 4, chunk1, chunk2, chunk3, chunk4);
        byte[]? result = await FrameCodec.ReadFrameAsync(chunkedStream);

        Assert.NotNull(result);
        Assert.Equal(ProtocolFixtures.MetricBatchBytes, result);
    }

    // =========================================================================
    // 4. MULTIPLE FRAMES CONCATENATED IN ONE UNDERLYING STREAM
    // =========================================================================

    [Fact]
    public async Task Requirement4_MultipleFramesConcatenated_ReadsSequentiallyUntilCleanEof()
    {
        using var stream = new MemoryStream();

        // Write all 4 canonical fixtures sequentially
        await stream.WriteAsync(ProtocolFixtures.HeartbeatFramed);
        await stream.WriteAsync(ProtocolFixtures.MetricBatchFramed);
        await stream.WriteAsync(ProtocolFixtures.LoginFramed);
        await stream.WriteAsync(ProtocolFixtures.IncidentAckFramed);

        stream.Position = 0;

        // Frame 1: HEARTBEAT
        byte[]? frame1 = await FrameCodec.ReadFrameAsync(stream);
        Assert.NotNull(frame1);
        Assert.Equal(ProtocolFixtures.HeartbeatBytes, frame1);

        // Frame 2: METRIC_BATCH
        byte[]? frame2 = await FrameCodec.ReadFrameAsync(stream);
        Assert.NotNull(frame2);
        Assert.Equal(ProtocolFixtures.MetricBatchBytes, frame2);

        // Frame 3: LOGIN
        byte[]? frame3 = await FrameCodec.ReadFrameAsync(stream);
        Assert.NotNull(frame3);
        Assert.Equal(ProtocolFixtures.LoginBytes, frame3);

        // Frame 4: INCIDENT_ACK
        byte[]? frame4 = await FrameCodec.ReadFrameAsync(stream);
        Assert.NotNull(frame4);
        Assert.Equal(ProtocolFixtures.IncidentAckBytes, frame4);

        // Clean EOF at boundary
        byte[]? eof = await FrameCodec.ReadFrameAsync(stream);
        Assert.Null(eof);
    }

    // =========================================================================
    // 5. PAYLOAD > 262,144 BYTES REJECTED
    // =========================================================================

    [Fact]
    public void Requirement5_PayloadExceedingMax_Encode_ThrowsArgumentException()
    {
        byte[] oversized = new byte[FrameCodec.MaxPayloadSize + 1];
        Assert.Throws<ArgumentException>(() => FrameCodec.Encode(oversized));
    }

    [Fact]
    public async Task Requirement5_PayloadExceedingMax_WriteFrameAsync_ThrowsArgumentException()
    {
        using var stream = new MemoryStream();
        byte[] oversized = new byte[FrameCodec.MaxPayloadSize + 1];
        await Assert.ThrowsAsync<ArgumentException>(() => FrameCodec.WriteFrameAsync(stream, oversized));
    }

    [Theory]
    [InlineData(262_145)]
    [InlineData(300_000)]
    [InlineData(1_000_000)]
    public async Task Requirement5_PayloadExceedingMax_ReadFrameAsync_ThrowsInvalidDataException(int oversizedLength)
    {
        byte[] header = new byte[FrameCodec.HeaderSize];
        BinaryPrimitives.WriteUInt32BigEndian(header, (uint)oversizedLength);
        using var stream = new MemoryStream(header);

        var ex = await Assert.ThrowsAsync<InvalidDataException>(() => FrameCodec.ReadFrameAsync(stream));
        Assert.Contains("exceeds maximum allowed size", ex.Message);
    }

    [Fact]
    public void Requirement5_ExactBoundaryPayload_262144Bytes_Allowed()
    {
        byte[] boundaryPayload = new byte[FrameCodec.MaxPayloadSize];
        Array.Fill(boundaryPayload, (byte)'a');

        byte[] framed = FrameCodec.Encode(boundaryPayload);
        Assert.Equal(FrameCodec.MaxWireFrameSize, framed.Length);

        byte[] decoded = FrameCodec.Decode(framed);
        Assert.Equal(FrameCodec.MaxPayloadSize, decoded.Length);
    }

    // =========================================================================
    // 6. MALFORMED JSON REJECTED
    // =========================================================================

    [Theory]
    [InlineData("{\"version\":1,\"messageType\":")] // Truncated JSON
    [InlineData("NOT_VALID_JSON")]                 // Plain text
    [InlineData("{version:1}")]                    // Unquoted key
    [InlineData("{\"version\":1,\"unclosed")]      // Unclosed object
    [InlineData("{ broken json string: [ unclosed")]
    public void Requirement6_SyntacticallyMalformedJson_ValidateRawJson_ReturnsMalformedJson(string malformed)
    {
        var result = EnvelopeValidator.ValidateRawJson(malformed);
        Assert.False(result.IsValid);
        Assert.Equal(ErrorCodes.MalformedJson, result.ErrorCode);
    }

    [Fact]
    public void Requirement6_EmptyString_ValidateRawJson_ReturnsInvalidPayload()
    {
        var result = EnvelopeValidator.ValidateRawJson("");
        Assert.False(result.IsValid);
        Assert.Equal(ErrorCodes.InvalidPayload, result.ErrorCode);
    }

    [Fact]
    public async Task Requirement6_MalformedJson_ReadEnvelopeAsync_ThrowsInvalidDataException()
    {
        byte[] badJsonBytes = Encoding.UTF8.GetBytes("{\"version\":1,\"unclosed");
        byte[] framed = FrameCodec.Encode(badJsonBytes);

        using var stream = new MemoryStream(framed);
        var ex = await Assert.ThrowsAsync<InvalidDataException>(() => FrameCodec.ReadEnvelopeAsync(stream));
        Assert.Contains(ErrorCodes.MalformedJson, ex.Message);
    }

    // =========================================================================
    // 7. ENVELOPE MISSING REQUIRED FIELD REJECTED
    // =========================================================================

    [Fact]
    public void Requirement7_MissingVersion_Rejected()
    {
        string json = "{\"messageType\":\"HEARTBEAT\",\"requestId\":\"c0000000-0000-4000-8000-000000000001\",\"deviceId\":\"PC-LAB1-01\",\"sentAtUtc\":\"2026-09-20T10:00:00.0000000Z\",\"payload\":{}}";
        var result = EnvelopeValidator.ValidateRawJson(json);
        Assert.False(result.IsValid);
        Assert.Equal(ErrorCodes.InvalidPayload, result.ErrorCode);
    }

    [Fact]
    public void Requirement7_MissingMessageType_Rejected()
    {
        string json = "{\"version\":1,\"requestId\":\"c0000000-0000-4000-8000-000000000001\",\"deviceId\":\"PC-LAB1-01\",\"sentAtUtc\":\"2026-09-20T10:00:00.0000000Z\",\"payload\":{}}";
        var result = EnvelopeValidator.ValidateRawJson(json);
        Assert.False(result.IsValid);
        Assert.Equal(ErrorCodes.InvalidPayload, result.ErrorCode);
    }

    [Fact]
    public void Requirement7_MissingRequestId_Rejected()
    {
        string json = "{\"version\":1,\"messageType\":\"HEARTBEAT\",\"deviceId\":\"PC-LAB1-01\",\"sentAtUtc\":\"2026-09-20T10:00:00.0000000Z\",\"payload\":{}}";
        var result = EnvelopeValidator.ValidateRawJson(json);
        Assert.False(result.IsValid);
        Assert.Equal(ErrorCodes.InvalidPayload, result.ErrorCode);
    }

    [Fact]
    public void Requirement7_MissingDeviceId_Rejected()
    {
        string json = "{\"version\":1,\"messageType\":\"HEARTBEAT\",\"requestId\":\"c0000000-0000-4000-8000-000000000001\",\"sentAtUtc\":\"2026-09-20T10:00:00.0000000Z\",\"payload\":{}}";
        var result = EnvelopeValidator.ValidateRawJson(json);
        Assert.False(result.IsValid);
        Assert.Equal(ErrorCodes.InvalidPayload, result.ErrorCode);
    }

    [Fact]
    public void Requirement7_MissingSentAtUtc_Rejected()
    {
        string json = "{\"version\":1,\"messageType\":\"HEARTBEAT\",\"requestId\":\"c0000000-0000-4000-8000-000000000001\",\"deviceId\":\"PC-LAB1-01\",\"payload\":{}}";
        var result = EnvelopeValidator.ValidateRawJson(json);
        Assert.False(result.IsValid);
        Assert.Equal(ErrorCodes.InvalidPayload, result.ErrorCode);
    }

    [Fact]
    public void Requirement7_MissingPayload_Rejected()
    {
        string json = "{\"version\":1,\"messageType\":\"HEARTBEAT\",\"requestId\":\"c0000000-0000-4000-8000-000000000001\",\"deviceId\":\"PC-LAB1-01\",\"sentAtUtc\":\"2026-09-20T10:00:00.0000000Z\"}";
        var result = EnvelopeValidator.ValidateRawJson(json);
        Assert.False(result.IsValid);
        Assert.Equal(ErrorCodes.InvalidPayload, result.ErrorCode);
    }

    [Fact]
    public void Requirement7_ExtraFieldInOuterEnvelope_Rejected()
    {
        string json = "{\"version\":1,\"messageType\":\"HEARTBEAT\",\"requestId\":\"c0000000-0000-4000-8000-000000000001\",\"deviceId\":\"PC-LAB1-01\",\"sentAtUtc\":\"2026-09-20T10:00:00.0000000Z\",\"payload\":{},\"extraUnauthorizedField\":\"ILLEGAL\"}";
        var result = EnvelopeValidator.ValidateRawJson(json);
        Assert.False(result.IsValid);
        Assert.Equal(ErrorCodes.InvalidPayload, result.ErrorCode);
        Assert.Contains("extraUnauthorizedField", result.ErrorMessage);
    }

    [Fact]
    public void Requirement7_DuplicateVersion_Rejected()
    {
        string json = "{\"version\":1,\"version\":1,\"messageType\":\"HEARTBEAT\",\"requestId\":\"c0000000-0000-4000-8000-000000000001\",\"deviceId\":\"PC-LAB1-01\",\"sentAtUtc\":\"2026-09-20T10:00:00.0000000Z\",\"payload\":{}}";
        var result = EnvelopeValidator.ValidateRawJson(json);
        Assert.False(result.IsValid);
        Assert.Equal(ErrorCodes.InvalidPayload, result.ErrorCode);
        Assert.Contains("duplicate field", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Requirement7_DuplicatePayload_Rejected()
    {
        string json = "{\"version\":1,\"messageType\":\"HEARTBEAT\",\"requestId\":\"c0000000-0000-4000-8000-000000000001\",\"deviceId\":\"PC-LAB1-01\",\"sentAtUtc\":\"2026-09-20T10:00:00.0000000Z\",\"payload\":{},\"payload\":{}}";
        var result = EnvelopeValidator.ValidateRawJson(json);
        Assert.False(result.IsValid);
        Assert.Equal(ErrorCodes.InvalidPayload, result.ErrorCode);
        Assert.Contains("duplicate field", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Version")]
    [InlineData("MessageType")]
    [InlineData("RequestId")]
    [InlineData("DeviceId")]
    [InlineData("SentAtUtc")]
    [InlineData("Payload")]
    public void Requirement7_WrongCasingOfCanonicalFields_Rejected(string wrongCasedField)
    {
        // Build JSON where one field has incorrect casing (e.g. "Version" instead of "version")
        var fields = new Dictionary<string, string>
        {
            ["version"] = "1",
            ["messageType"] = "\"HEARTBEAT\"",
            ["requestId"] = "\"c0000000-0000-4000-8000-000000000001\"",
            ["deviceId"] = "\"PC-LAB1-01\"",
            ["sentAtUtc"] = "\"2026-09-20T10:00:00.0000000Z\"",
            ["payload"] = "{}"
        };

        // Find the canonical matching field to replace with the wrong-cased field
        string canonicalKey = fields.Keys.First(k => string.Equals(k, wrongCasedField, StringComparison.OrdinalIgnoreCase));
        string value = fields[canonicalKey];
        fields.Remove(canonicalKey);
        fields[wrongCasedField] = value;

        string json = "{" + string.Join(",", fields.Select(kv => $"\"{kv.Key}\":{kv.Value}")) + "}";
        var result = EnvelopeValidator.ValidateRawJson(json);

        Assert.False(result.IsValid);
        Assert.Equal(ErrorCodes.InvalidPayload, result.ErrorCode);
        Assert.Contains(wrongCasedField, result.ErrorMessage);
    }

    [Theory]
    [InlineData("\"string_payload\"")] // string
    [InlineData("12345")]             // number
    [InlineData("true")]              // boolean
    [InlineData("[1, 2, 3]")]         // array
    [InlineData("null")]              // null
    public void Requirement2_NonObjectPayload_Rejected(string invalidPayloadJson)
    {
        string json = $"{{\"version\":1,\"messageType\":\"HEARTBEAT\",\"requestId\":\"c0000000-0000-4000-8000-000000000001\",\"deviceId\":\"PC-LAB1-01\",\"sentAtUtc\":\"2026-09-20T10:00:00.0000000Z\",\"payload\":{invalidPayloadJson}}}";
        var result = EnvelopeValidator.ValidateRawJson(json);

        Assert.False(result.IsValid);
        Assert.Equal(ErrorCodes.InvalidPayload, result.ErrorCode);
        Assert.Contains("object", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================================
    // 8. WRONG PROTOCOL VERSION REJECTED (ERR_INVALID_VERSION)
    // =========================================================================

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(-1)]
    [InlineData(99)]
    public void Requirement8_WrongProtocolVersion_Rejected(int wrongVersion)
    {
        string json = $"{{\"version\":{wrongVersion},\"messageType\":\"HEARTBEAT\",\"requestId\":\"c0000000-0000-4000-8000-000000000001\",\"deviceId\":\"PC-LAB1-01\",\"sentAtUtc\":\"2026-09-20T10:00:00.0000000Z\",\"payload\":{{}}}}";
        var result = EnvelopeValidator.ValidateRawJson(json);
        Assert.False(result.IsValid);
        Assert.Equal(ErrorCodes.InvalidVersion, result.ErrorCode);
        Assert.Contains($"Unsupported envelope version: {wrongVersion}", result.ErrorMessage);
    }

    // =========================================================================
    // 9. FIXTURE FILES INTEGRITY & CONFORMANCE (EXACTLY 16 LG-A-001 CANONICAL FIXTURES)
    // =========================================================================

    [Theory]
    [InlineData("heartbeat.json", ProtocolFixtures.HeartbeatJson)]
    [InlineData("heartbeat_ack.json", ProtocolFixtures.HeartbeatAckJson)]
    [InlineData("metric_batch.json", ProtocolFixtures.MetricBatchJson)]
    [InlineData("batch_ack.json", ProtocolFixtures.BatchAckJson)]
    [InlineData("service_status.json", ProtocolFixtures.ServiceStatusJson)]
    [InlineData("hello_agent.json", ProtocolFixtures.HelloAgentJson)]
    [InlineData("hello_ack.json", ProtocolFixtures.HelloAckJson)]
    [InlineData("auth_agent.json", ProtocolFixtures.AuthAgentJson)]
    [InlineData("auth_result.json", ProtocolFixtures.AuthResultJson)]
    [InlineData("agent_goodbye.json", ProtocolFixtures.AgentGoodbyeJson)]
    [InlineData("goodbye_ack.json", ProtocolFixtures.GoodbyeAckJson)]
    [InlineData("login.json", ProtocolFixtures.LoginJson)]
    [InlineData("login_result.json", ProtocolFixtures.LoginResultJson)]
    [InlineData("incident_ack.json", ProtocolFixtures.IncidentAckJson)]
    [InlineData("command_result.json", ProtocolFixtures.CommandResultJson)]
    [InlineData("error.json", ProtocolFixtures.ErrorJson)]
    public void Fixtures_DiskFiles_MatchConstantsExactly_NoBom_Minified(string fileName, string expectedJson)
    {
        string baseDir = AppContext.BaseDirectory;
        // Search for the file in the project directory or output directory
        string fixturePath = Path.Combine(baseDir, "Fixtures", fileName);
        if (!File.Exists(fixturePath))
        {
            // Try relative to source tree
            fixturePath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "Fixtures", fileName));
        }

        Assert.True(File.Exists(fixturePath), $"Fixture file not found: {fixturePath}");

        byte[] fileBytes = File.ReadAllBytes(fixturePath);

        // Assert no UTF-8 BOM (0xEF, 0xBB, 0xBF)
        if (fileBytes.Length >= 3)
        {
            bool hasBom = fileBytes[0] == 0xEF && fileBytes[1] == 0xBB && fileBytes[2] == 0xBF;
            Assert.False(hasBom, $"Fixture file '{fileName}' contains UTF-8 BOM, which is prohibited.");
        }

        string fileContent = Encoding.UTF8.GetString(fileBytes);

        // Assert minified (no CRLF or LF)
        Assert.DoesNotContain("\r", fileContent);
        Assert.DoesNotContain("\n", fileContent);

        // Assert exact match with constant
        Assert.Equal(expectedJson, fileContent);

        // Assert under MAX_JSON_BYTES
        Assert.True(fileBytes.Length <= FrameCodec.MaxPayloadSize, "Fixture exceeds MAX_JSON_BYTES.");

        // Assert validates against wire contract
        var validation = EnvelopeValidator.ValidateRawJson(fileBytes);
        Assert.True(validation.IsValid, $"Fixture '{fileName}' failed validation: {validation.ErrorMessage}");
    }

    [Fact]
    public void Fixtures_All16_DeserializeIntoIntendedDtos()
    {
        // 1. HEARTBEAT
        var hb = ProtocolSerializer.ExtractPayload<HeartbeatPayload>(ProtocolSerializer.DeserializeEnvelope(ProtocolFixtures.HeartbeatBytes)!.Payload);
        Assert.NotNull(hb);
        Assert.Equal(1, hb.SequenceNumber);

        // 2. HEARTBEAT_ACK
        var hbAck = ProtocolSerializer.ExtractPayload<HeartbeatAckPayload>(ProtocolSerializer.DeserializeEnvelope(ProtocolFixtures.HeartbeatAckBytes)!.Payload);
        Assert.NotNull(hbAck);
        Assert.Equal(1, hbAck.AcknowledgedSequence);

        // 3. METRIC_BATCH
        var mb = ProtocolSerializer.ExtractPayload<MetricBatchPayload>(ProtocolSerializer.DeserializeEnvelope(ProtocolFixtures.MetricBatchBytes)!.Payload);
        Assert.NotNull(mb);
        Assert.Equal(2, mb.SampleCount);

        // 4. BATCH_ACK
        var bAck = ProtocolSerializer.ExtractPayload<BatchAckPayload>(ProtocolSerializer.DeserializeEnvelope(ProtocolFixtures.BatchAckBytes)!.Payload);
        Assert.NotNull(bAck);
        Assert.True(bAck.Accepted);

        // 5. SERVICE_STATUS
        var ss = ProtocolSerializer.ExtractPayload<ServiceStatusPayload>(ProtocolSerializer.DeserializeEnvelope(ProtocolFixtures.ServiceStatusBytes)!.Payload);
        Assert.NotNull(ss);
        Assert.Equal(2, ss.Services.Count);

        // 6. HELLO_AGENT
        var ha = ProtocolSerializer.ExtractPayload<HelloAgentPayload>(ProtocolSerializer.DeserializeEnvelope(ProtocolFixtures.HelloAgentBytes)!.Payload);
        Assert.NotNull(ha);
        Assert.Equal("1.0.0", ha.AgentVersion);

        // 7. HELLO_ACK
        var hAck = ProtocolSerializer.ExtractPayload<HelloAckPayload>(ProtocolSerializer.DeserializeEnvelope(ProtocolFixtures.HelloAckBytes)!.Payload);
        Assert.NotNull(hAck);
        Assert.Equal(5, hAck.HeartbeatIntervalSeconds);

        // 8. AUTH_AGENT
        var authA = ProtocolSerializer.ExtractPayload<AuthAgentPayload>(ProtocolSerializer.DeserializeEnvelope(ProtocolFixtures.AuthAgentBytes)!.Payload);
        Assert.NotNull(authA);
        Assert.Equal("LABGUARD_TOKEN_2026", authA.PreSharedToken);

        // 9. AUTH_RESULT
        var authR = ProtocolSerializer.ExtractPayload<AuthResultPayload>(ProtocolSerializer.DeserializeEnvelope(ProtocolFixtures.AuthResultBytes)!.Payload);
        Assert.NotNull(authR);
        Assert.True(authR.Success);

        // 10. AGENT_GOODBYE
        var gb = ProtocolSerializer.ExtractPayload<AgentGoodbyePayload>(ProtocolSerializer.DeserializeEnvelope(ProtocolFixtures.AgentGoodbyeBytes)!.Payload);
        Assert.NotNull(gb);
        Assert.Equal("NORMAL_SHUTDOWN", gb.Reason);

        // 11. GOODBYE_ACK
        var gbAck = ProtocolSerializer.ExtractPayload<GoodbyeAckPayload>(ProtocolSerializer.DeserializeEnvelope(ProtocolFixtures.GoodbyeAckBytes)!.Payload);
        Assert.NotNull(gbAck);
        Assert.True(gbAck.Success);

        // 12. LOGIN
        var login = ProtocolSerializer.ExtractPayload<LoginPayload>(ProtocolSerializer.DeserializeEnvelope(ProtocolFixtures.LoginBytes)!.Payload);
        Assert.NotNull(login);
        Assert.Equal("admin", login.Username);

        // 13. LOGIN_RESULT
        var loginR = ProtocolSerializer.ExtractPayload<LoginResultPayload>(ProtocolSerializer.DeserializeEnvelope(ProtocolFixtures.LoginResultBytes)!.Payload);
        Assert.NotNull(loginR);
        Assert.True(loginR.Success);

        // 14. INCIDENT_ACK
        var incAck = ProtocolSerializer.ExtractPayload<IncidentCommandPayload>(ProtocolSerializer.DeserializeEnvelope(ProtocolFixtures.IncidentAckBytes)!.Payload);
        Assert.NotNull(incAck);
        Assert.Equal(1, incAck.IncidentId);

        // 15. COMMAND_RESULT
        var cmdR = ProtocolSerializer.ExtractPayload<CommandResultPayload>(ProtocolSerializer.DeserializeEnvelope(ProtocolFixtures.CommandResultBytes)!.Payload);
        Assert.NotNull(cmdR);
        Assert.True(cmdR.Success);

        // 16. ERROR
        var err = ProtocolSerializer.ExtractPayload<ErrorPayload>(ProtocolSerializer.DeserializeEnvelope(ProtocolFixtures.ErrorBytes)!.Payload);
        Assert.NotNull(err);
        Assert.Equal("ERR_INVALID_PAYLOAD", err.ErrorCode);
    }

    // =========================================================================
    // 10. EXPLICIT UTC DESIGNATOR TESTS (REQUIREMENT 5)
    // =========================================================================

    [Theory]
    [InlineData("2026-09-20T10:00:00Z", true)]
    [InlineData("2026-09-20T10:00:00+00:00", true)]
    [InlineData("2026-09-20T10:00:00", false)]
    [InlineData("2026-09-20T10:00:00+07:00", false)]
    [InlineData("2026-09-20T10:00:00z", false)]
    [InlineData("2026-09-20T10:00:00-00:00", false)]
    public void SentAtUtc_ExplicitUtcDesignator_Validation(string sentAtUtc, bool expectedValid)
    {
        string id = Guid.NewGuid().ToString("D");
        string json = $"{{\"version\":1,\"messageType\":\"HEARTBEAT\",\"requestId\":\"{id}\",\"deviceId\":\"PC-01\",\"sentAtUtc\":\"{sentAtUtc}\",\"payload\":{{\"sequenceNumber\":1,\"uptimeSeconds\":3600}}}}";

        var validation = EnvelopeValidator.ValidateRawJson(json);
        if (expectedValid)
        {
            Assert.True(validation.IsValid, $"Expected sentAtUtc '{sentAtUtc}' to be valid, but got: {validation.ErrorMessage}");
        }
        else
        {
            Assert.False(validation.IsValid, $"Expected sentAtUtc '{sentAtUtc}' to be rejected, but it passed.");
            Assert.Equal(ErrorCodes.InvalidPayload, validation.ErrorCode);
        }
    }

    // =========================================================================
    // 11. READENVELOPEASYNC STRICTNESS (REQUIREMENT 4)
    // =========================================================================

    [Fact]
    public async Task ReadEnvelopeAsync_InvalidEnvelope_ThrowsInvalidDataException()
    {
        // Envelope with unknown outer field
        string id = Guid.NewGuid().ToString("D");
        string json = $"{{\"version\":1,\"messageType\":\"HEARTBEAT\",\"requestId\":\"{id}\",\"deviceId\":\"PC-01\",\"sentAtUtc\":\"2026-09-20T10:00:00Z\",\"payload\":{{}},\"unauthorized\":true}}";
        byte[] framed = FrameCodec.Encode(Encoding.UTF8.GetBytes(json));

        using var stream = new MemoryStream(framed);
        var ex = await Assert.ThrowsAsync<InvalidDataException>(() => FrameCodec.ReadEnvelopeAsync(stream));
        Assert.Contains(ErrorCodes.InvalidPayload, ex.Message);
    }

    [Fact]
    public async Task ReadEnvelopeAsync_MalformedJson_ThrowsInvalidDataException()
    {
        byte[] badJson = Encoding.UTF8.GetBytes("{ broken json string: [ unclosed");
        byte[] framed = FrameCodec.Encode(badJson);

        using var stream = new MemoryStream(framed);
        var ex = await Assert.ThrowsAsync<InvalidDataException>(() => FrameCodec.ReadEnvelopeAsync(stream));
        Assert.Contains(ErrorCodes.MalformedJson, ex.Message);
    }
}
