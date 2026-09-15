using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using LabGuard.Agent;
using LabGuard.Protocol;
using LabGuard.Protocol.Dtos;
using LabGuard.Protocol.Serialization;
using Xunit;
using Xunit.Abstractions;

namespace LabGuard.Central.Tests;

public class TcpHandshakeIntegrationTests : IAsyncDisposable
{
    private const string TestAgentToken = "TEST_ONLY_AGENT_TOKEN_DO_NOT_USE_IN_PRODUCTION";
    private readonly ITestOutputHelper _testOutput;
    private readonly List<string> _traceLogs = [];
    private CentralServer? _server;

    public TcpHandshakeIntegrationTests(ITestOutputHelper testOutput)
    {
        _testOutput = testOutput;
    }

    private void LogTrace(string message)
    {
        lock (_traceLogs)
        {
            _traceLogs.Add(message);
        }
        _testOutput.WriteLine(message);
    }

    private CentralServer CreateAndStartServer(string expectedToken = TestAgentToken)
    {
        var server = new CentralServer(
            expectedToken: expectedToken,
            ipAddress: IPAddress.Loopback,
            port: 0,
            logger: LogTrace);

        server.Start();
        return server;
    }

    // =========================================================================
    // TEST 1: HELLO_AGENT -> HELLO_ACK succeeds
    // =========================================================================
    [Fact]
    public async Task HelloAgent_Handshake_ReturnsHelloAck_Successfully()
    {
        _server = CreateAndStartServer();
        await using var client = new AgentSocketClient(LogTrace);

        await client.ConnectAsync("127.0.0.1", _server.Port);
        var ack = await client.SendHelloAsync("LAB01-PC01");

        Assert.NotNull(ack);
        Assert.Equal("1.0.0", ack.ServerVersion);
        Assert.Contains("TLS", ack.Capabilities);
        Assert.Contains("JSON_FRAMING", ack.Capabilities);
        Assert.Contains("OCC", ack.Capabilities);
        Assert.Equal(5, ack.HeartbeatIntervalSeconds);
        Assert.NotEmpty(ack.ServerTimeUtc);
    }

    // =========================================================================
    // TEST 2: AUTH_AGENT -> AUTH_RESULT(success=true) succeeds
    // =========================================================================
    [Fact]
    public async Task AuthAgent_WithValidToken_ReturnsAuthResultSuccess()
    {
        _server = CreateAndStartServer();
        await using var client = new AgentSocketClient(LogTrace);

        await client.ConnectAsync("127.0.0.1", _server.Port);
        await client.SendHelloAsync("LAB01-PC02");
        var authResult = await client.SendAuthAsync("LAB01-PC02", TestAgentToken);

        Assert.True(authResult.Success);
        Assert.NotNull(authResult.SessionId);
        Assert.True(Guid.TryParseExact(authResult.SessionId, "D", out _));
        Assert.Equal("LAB01-PC02", authResult.AssignedDeviceId);
        Assert.Null(authResult.ErrorMessage);
        Assert.Equal(authResult.SessionId, client.SessionId);
    }

    // =========================================================================
    // TEST 3: AUTH_AGENT before HELLO_AGENT is rejected
    // =========================================================================
    [Fact]
    public async Task AuthAgent_BeforeHelloAgent_IsRejectedByCentral()
    {
        _server = CreateAndStartServer();
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync("127.0.0.1", _server.Port);
        var stream = tcpClient.GetStream();

        string correlationId = Guid.NewGuid().ToString("D");
        var authPayload = new AuthAgentPayload
        {
            PreSharedToken = TestAgentToken,
            ClientNonce = Guid.NewGuid().ToString("N")
        };

        var envelope = WireEnvelope<AuthAgentPayload>.Create(
            MessageTypes.AuthAgent,
            "LAB01-PC03",
            authPayload,
            correlationId);

        byte[] envelopeBytes = ProtocolSerializer.Serialize(envelope);

        // Act: Send AUTH_AGENT directly without HELLO_AGENT
        await FrameCodec.WriteFrameAsync(stream, envelopeBytes);

        // Assert: Central Server replies with ERROR envelope
        byte[]? responseBytes = await FrameCodec.ReadFrameAsync(stream);
        Assert.NotNull(responseBytes);

        var response = ProtocolSerializer.DeserializeEnvelope(responseBytes);
        Assert.NotNull(response);
        Assert.Equal(MessageTypes.Error, response.MessageType);
        Assert.Equal(correlationId, response.RequestId);

        var error = ProtocolSerializer.ExtractPayload<ErrorPayload>(response.Payload);
        Assert.NotNull(error);
        Assert.Equal(ErrorCodes.Unauthenticated, error.ErrorCode);
    }

    // =========================================================================
    // TEST 4: Unknown message type is rejected
    // =========================================================================
    [Fact]
    public async Task UnknownMessageType_IsRejectedWithErrorEnvelope()
    {
        _server = CreateAndStartServer();
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync("127.0.0.1", _server.Port);
        var stream = tcpClient.GetStream();

        string correlationId = Guid.NewGuid().ToString("D");
        var envelope = WireEnvelope<object>.Create(
            "UNKNOWN_CUSTOM_NON_EXISTENT_MSG",
            "LAB01-PC04",
            new { test = 123 },
            correlationId);

        byte[] envelopeBytes = ProtocolSerializer.Serialize(envelope);

        await FrameCodec.WriteFrameAsync(stream, envelopeBytes);

        byte[]? responseBytes = await FrameCodec.ReadFrameAsync(stream);
        Assert.NotNull(responseBytes);

        var response = ProtocolSerializer.DeserializeEnvelope(responseBytes);
        Assert.NotNull(response);
        Assert.Equal(MessageTypes.Error, response.MessageType);
        Assert.Equal(correlationId, response.RequestId);

        var error = ProtocolSerializer.ExtractPayload<ErrorPayload>(response.Payload);
        Assert.NotNull(error);
        Assert.Equal(ErrorCodes.UnknownMessageType, error.ErrorCode);
    }

    // =========================================================================
    // TEST 5: Invalid/malformed JSON payload does not crash Central
    // =========================================================================
    [Fact]
    public async Task MalformedJsonPayload_DoesNotCrashCentral_ReturnsInvalidPayloadError()
    {
        _server = CreateAndStartServer();
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync("127.0.0.1", _server.Port);
        var stream = tcpClient.GetStream();

        byte[] malformedJson = Encoding.UTF8.GetBytes("{ broken json string: [ unclosed");

        await FrameCodec.WriteFrameAsync(stream, malformedJson);

        byte[]? responseBytes = await FrameCodec.ReadFrameAsync(stream);
        Assert.NotNull(responseBytes);

        var response = ProtocolSerializer.DeserializeEnvelope(responseBytes);
        Assert.NotNull(response);
        Assert.Equal(MessageTypes.Error, response.MessageType);

        var error = ProtocolSerializer.ExtractPayload<ErrorPayload>(response.Payload);
        Assert.NotNull(error);
        Assert.Equal(ErrorCodes.InvalidPayload, error.ErrorCode);

        // Verify Central is still alive and accepts legitimate clients
        await using var client2 = new AgentSocketClient(LogTrace);
        await client2.ConnectAsync("127.0.0.1", _server.Port);
        var ack = await client2.SendHelloAsync("LAB01-PC05");
        Assert.NotNull(ack);
    }

    // =========================================================================
    // TEST 6: Oversized frame is rejected deterministically over real TCP
    // =========================================================================
    [Fact]
    public async Task OversizedFrame_OverRealTcp_IsRejectedDeterministically()
    {
        _server = CreateAndStartServer();
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync("127.0.0.1", _server.Port);
        var stream = tcpClient.GetStream();

        // 4-byte header declaring 500,000 bytes (exceeds 262,144 limit)
        byte[] oversizedHeader = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(oversizedHeader, 500_000);

        await stream.WriteAsync(oversizedHeader);
        await stream.FlushAsync();

        // Bounded read timeout of 2 seconds
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        bool connectionRejectedOrClosed = false;
        try
        {
            byte[]? response = await FrameCodec.ReadFrameAsync(stream, FrameCodec.MaxPayloadSize, timeoutCts.Token);
            if (response != null)
            {
                var envelope = ProtocolSerializer.DeserializeEnvelope(response);
                if (envelope != null && envelope.MessageType == MessageTypes.Error)
                {
                    var error = ProtocolSerializer.ExtractPayload<ErrorPayload>(envelope.Payload);
                    connectionRejectedOrClosed = error?.ErrorCode == ErrorCodes.FrameTooLarge;
                }
            }
            else
            {
                // Clean socket closure by server
                connectionRejectedOrClosed = true;
            }
        }
        catch (EndOfStreamException)
        {
            // Socket closed cleanly by server upon frame validation failure
            connectionRejectedOrClosed = true;
        }
        catch (IOException)
        {
            connectionRejectedOrClosed = true;
        }

        Assert.True(connectionRejectedOrClosed, "Central server must reject oversized frame by returning ERROR or closing socket.");

        // Verify Central is still running and accepts new clean client
        await using var cleanClient = new AgentSocketClient(LogTrace);
        await cleanClient.ConnectAsync("127.0.0.1", _server.Port);
        var ack = await cleanClient.SendHelloAsync("LAB01-PC06");
        Assert.NotNull(ack);
    }

    // =========================================================================
    // TEST 7: Client disconnect during handshake is handled without exception
    // =========================================================================
    [Fact]
    public async Task ClientDisconnect_MidFrame_HandledWithoutUnobservedException()
    {
        _server = CreateAndStartServer();

        // Scenario A: Client connects, sends 2 header bytes, and abruptly closes socket
        using (var clientA = new TcpClient())
        {
            await clientA.ConnectAsync("127.0.0.1", _server.Port);
            var streamA = clientA.GetStream();
            await streamA.WriteAsync(new byte[] { 0x00, 0x01 });
            await streamA.FlushAsync();
            clientA.Close();
        }

        // Scenario B: Client connects, sends 4 header bytes declaring 200 bytes, sends 20 payload bytes, closes
        using (var clientB = new TcpClient())
        {
            await clientB.ConnectAsync("127.0.0.1", _server.Port);
            var streamB = clientB.GetStream();
            byte[] headerAndPartial = new byte[4 + 20];
            BinaryPrimitives.WriteUInt32BigEndian(headerAndPartial.AsSpan(0, 4), 200);
            await streamB.WriteAsync(headerAndPartial);
            await streamB.FlushAsync();
            clientB.Close();
        }

        await Task.Delay(100);

        await using var cleanClient = new AgentSocketClient(LogTrace);
        await cleanClient.ConnectAsync("127.0.0.1", _server.Port);
        var ack = await cleanClient.SendHelloAsync("LAB01-PC07");
        Assert.NotNull(ack);
    }

    // =========================================================================
    // TEST 8: RequestId correlation is preserved with valid UUIDv4
    // =========================================================================
    [Fact]
    public async Task RequestId_CorrelationIsPreserved_WithValidUuid()
    {
        _server = CreateAndStartServer();
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync("127.0.0.1", _server.Port);
        var stream = tcpClient.GetStream();

        // 1. HELLO with specific UUIDv4 requestId
        string helloRequestId = Guid.NewGuid().ToString("D");
        var helloEnvelope = WireEnvelope<HelloAgentPayload>.Create(
            MessageTypes.HelloAgent,
            "LAB01-PC08",
            new HelloAgentPayload { AgentVersion = "1.0.0", Hostname = "PC08" },
            helloRequestId);

        await FrameCodec.WriteFrameAsync(stream, ProtocolSerializer.Serialize(helloEnvelope));
        byte[]? helloRespBytes = await FrameCodec.ReadFrameAsync(stream);
        Assert.NotNull(helloRespBytes);

        var helloResp = ProtocolSerializer.DeserializeEnvelope(helloRespBytes);
        Assert.NotNull(helloResp);
        Assert.Equal(MessageTypes.HelloAck, helloResp.MessageType);
        Assert.Equal(helloRequestId, helloResp.RequestId);

        // 2. AUTH with different specific UUIDv4 requestId
        string authRequestId = Guid.NewGuid().ToString("D");
        var authEnvelope = WireEnvelope<AuthAgentPayload>.Create(
            MessageTypes.AuthAgent,
            "LAB01-PC08",
            new AuthAgentPayload { PreSharedToken = TestAgentToken },
            authRequestId);

        await FrameCodec.WriteFrameAsync(stream, ProtocolSerializer.Serialize(authEnvelope));
        byte[]? authRespBytes = await FrameCodec.ReadFrameAsync(stream);
        Assert.NotNull(authRespBytes);

        var authResp = ProtocolSerializer.DeserializeEnvelope(authRespBytes);
        Assert.NotNull(authResp);
        Assert.Equal(MessageTypes.AuthResult, authResp.MessageType);
        Assert.Equal(authRequestId, authResp.RequestId);
    }

    // =========================================================================
    // TEST 9: Full End-to-End Handshake Trace Evidence
    // =========================================================================
    [Fact]
    public async Task FullHandshake_GeneratesCompleteTraceEvidence()
    {
        _server = CreateAndStartServer();
        await using var client = new AgentSocketClient(LogTrace);

        await client.ConnectAsync("127.0.0.1", _server.Port);
        var helloAck = await client.SendHelloAsync("LAB01-PC09");
        var authResult = await client.SendAuthAsync("LAB01-PC09", TestAgentToken);
        await client.DisconnectAsync();

        Assert.NotNull(helloAck);
        Assert.True(authResult.Success);
        Assert.NotNull(authResult.SessionId);

        lock (_traceLogs)
        {
            Assert.Contains(_traceLogs, l => l.Contains("HELLO_AGENT") && l.Contains("LAB01-PC09"));
            Assert.Contains(_traceLogs, l => l.Contains("HELLO_ACK"));
            Assert.Contains(_traceLogs, l => l.Contains("AUTH_AGENT") && l.Contains("LAB01-PC09"));
            Assert.Contains(_traceLogs, l => l.Contains("AUTH_RESULT") && l.Contains("SUCCESS"));
            Assert.Contains(_traceLogs, l => l.Contains("AGENT_GOODBYE"));
            Assert.Contains(_traceLogs, l => l.Contains("GOODBYE_ACK"));
            // Ensure auth secret is NEVER logged
            Assert.DoesNotContain(_traceLogs, l => l.Contains(TestAgentToken));
        }
    }

    // =========================================================================
    // TEST 10: Server Shutdown Lifecycle Integration Test
    // =========================================================================
    [Fact]
    public async Task ServerShutdown_WithActiveClients_GracefullyTerminatesAllTasks()
    {
        _server = CreateAndStartServer();

        // Connect 3 clients
        var client1 = new AgentSocketClient(LogTrace);
        var client2 = new AgentSocketClient(LogTrace);
        var client3 = new AgentSocketClient(LogTrace);

        await client1.ConnectAsync("127.0.0.1", _server.Port);
        await client2.ConnectAsync("127.0.0.1", _server.Port);
        await client3.ConnectAsync("127.0.0.1", _server.Port);

        await client1.SendHelloAsync("DEV-01");
        await client2.SendHelloAsync("DEV-02");
        await client3.SendHelloAsync("DEV-03");

        Assert.Equal(3, _server.ActiveConnectionCount);

        // Act: Stop server
        await _server.StopAsync();

        // Assert: All tasks terminated and active connection count is 0
        Assert.Equal(0, _server.ActiveConnectionCount);
        Assert.False(_server.IsRunning);

        await client1.DisposeAsync();
        await client2.DisposeAsync();
        await client3.DisposeAsync();
    }

    // =========================================================================
    // TEST 11: HELLO_AGENT with wrong payload type (e.g. integer or empty)
    // =========================================================================
    [Fact]
    public async Task HelloAgent_WithInvalidPayloadType_IsRejected()
    {
        _server = CreateAndStartServer();
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync("127.0.0.1", _server.Port);
        var stream = tcpClient.GetStream();

        string correlationId = Guid.NewGuid().ToString("D");
        // Payload is an integer instead of JSON object
        string json = $"{{\"version\":1,\"messageType\":\"HELLO_AGENT\",\"requestId\":\"{correlationId}\",\"deviceId\":\"LAB01-PC11\",\"sentAtUtc\":\"{DateTime.UtcNow:O}\",\"payload\":12345}}";

        await FrameCodec.WriteFrameAsync(stream, Encoding.UTF8.GetBytes(json));

        byte[]? responseBytes = await FrameCodec.ReadFrameAsync(stream);
        Assert.NotNull(responseBytes);

        var response = ProtocolSerializer.DeserializeEnvelope(responseBytes);
        Assert.NotNull(response);
        Assert.Equal(MessageTypes.Error, response.MessageType);
        Assert.Equal(correlationId, response.RequestId);

        var error = ProtocolSerializer.ExtractPayload<ErrorPayload>(response.Payload);
        Assert.NotNull(error);
        Assert.Equal(ErrorCodes.InvalidPayload, error.ErrorCode);
    }

    // =========================================================================
    // TEST 12: AUTH_AGENT with wrong payload type
    // =========================================================================
    [Fact]
    public async Task AuthAgent_WithInvalidPayloadType_IsRejected()
    {
        _server = CreateAndStartServer();
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync("127.0.0.1", _server.Port);
        var stream = tcpClient.GetStream();

        // 1. Legitimate HELLO
        string helloId = Guid.NewGuid().ToString("D");
        var helloEnvelope = WireEnvelope<HelloAgentPayload>.Create(MessageTypes.HelloAgent, "LAB01-PC12", new HelloAgentPayload { AgentVersion = "1.0.0", Hostname = "PC12" }, helloId);
        await FrameCodec.WriteFrameAsync(stream, ProtocolSerializer.Serialize(helloEnvelope));
        var helloAckBytes = await FrameCodec.ReadFrameAsync(stream);
        Assert.NotNull(helloAckBytes);

        // 2. AUTH with array payload
        string authId = Guid.NewGuid().ToString("D");
        string json = $"{{\"version\":1,\"messageType\":\"AUTH_AGENT\",\"requestId\":\"{authId}\",\"deviceId\":\"LAB01-PC12\",\"sentAtUtc\":\"{DateTime.UtcNow:O}\",\"payload\":[\"invalid\",\"array\"]}}";
        await FrameCodec.WriteFrameAsync(stream, Encoding.UTF8.GetBytes(json));

        byte[]? authRespBytes = await FrameCodec.ReadFrameAsync(stream);
        Assert.NotNull(authRespBytes);

        var authResp = ProtocolSerializer.DeserializeEnvelope(authRespBytes);
        Assert.NotNull(authResp);
        Assert.Equal(MessageTypes.AuthResult, authResp.MessageType);

        var result = ProtocolSerializer.ExtractPayload<AuthResultPayload>(authResp.Payload);
        Assert.NotNull(result);
        Assert.False(result.Success);
    }

    // =========================================================================
    // TEST 13: AUTH DeviceId mismatch with HELLO DeviceId
    // =========================================================================
    [Fact]
    public async Task AuthAgent_DeviceIdMismatch_IsRejected()
    {
        _server = CreateAndStartServer();
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync("127.0.0.1", _server.Port);
        var stream = tcpClient.GetStream();

        // 1. HELLO as PC-A
        string helloId = Guid.NewGuid().ToString("D");
        var helloEnvelope = WireEnvelope<HelloAgentPayload>.Create(MessageTypes.HelloAgent, "PC-A", new HelloAgentPayload { AgentVersion = "1.0.0", Hostname = "PC-A" }, helloId);
        await FrameCodec.WriteFrameAsync(stream, ProtocolSerializer.Serialize(helloEnvelope));
        var helloAckBytes = await FrameCodec.ReadFrameAsync(stream);
        Assert.NotNull(helloAckBytes);

        // 2. AUTH claiming to be PC-B
        string authId = Guid.NewGuid().ToString("D");
        var authEnvelope = WireEnvelope<AuthAgentPayload>.Create(MessageTypes.AuthAgent, "PC-B", new AuthAgentPayload { PreSharedToken = TestAgentToken }, authId);
        await FrameCodec.WriteFrameAsync(stream, ProtocolSerializer.Serialize(authEnvelope));

        byte[]? authRespBytes = await FrameCodec.ReadFrameAsync(stream);
        Assert.NotNull(authRespBytes);

        var authResp = ProtocolSerializer.DeserializeEnvelope(authRespBytes);
        Assert.NotNull(authResp);
        Assert.Equal(MessageTypes.AuthResult, authResp.MessageType);

        var result = ProtocolSerializer.ExtractPayload<AuthResultPayload>(authResp.Payload);
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("mismatch", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================================
    // TEST 14: AUTH with invalid token
    // =========================================================================
    [Fact]
    public async Task AuthAgent_InvalidToken_IsRejected()
    {
        _server = CreateAndStartServer();
        await using var client = new AgentSocketClient(LogTrace);

        await client.ConnectAsync("127.0.0.1", _server.Port);
        await client.SendHelloAsync("LAB01-PC14");
        var authResult = await client.SendAuthAsync("LAB01-PC14", "INCORRECT_SECRET_TOKEN");

        Assert.False(authResult.Success);
        Assert.Null(authResult.SessionId);
        Assert.Contains("invalid", authResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================================
    // TEST 15: Envelope missing requestId is rejected
    // =========================================================================
    [Fact]
    public async Task Envelope_MissingRequestId_IsRejected()
    {
        _server = CreateAndStartServer();
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync("127.0.0.1", _server.Port);
        var stream = tcpClient.GetStream();

        // Omit requestId property
        string json = $"{{\"version\":1,\"messageType\":\"HELLO_AGENT\",\"deviceId\":\"LAB01-PC15\",\"sentAtUtc\":\"{DateTime.UtcNow:O}\",\"payload\":{{\"agentVersion\":\"1.0\",\"hostname\":\"PC\"}}}}";
        await FrameCodec.WriteFrameAsync(stream, Encoding.UTF8.GetBytes(json));

        byte[]? respBytes = await FrameCodec.ReadFrameAsync(stream);
        Assert.NotNull(respBytes);

        var resp = ProtocolSerializer.DeserializeEnvelope(respBytes);
        Assert.NotNull(resp);
        Assert.Equal(MessageTypes.Error, resp.MessageType);

        var error = ProtocolSerializer.ExtractPayload<ErrorPayload>(resp.Payload);
        Assert.NotNull(error);
        Assert.Equal(ErrorCodes.InvalidPayload, error.ErrorCode);
    }

    // =========================================================================
    // TEST 16: Envelope with non-UUID requestId is rejected
    // =========================================================================
    [Fact]
    public async Task Envelope_NonUuidRequestId_IsRejected()
    {
        _server = CreateAndStartServer();
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync("127.0.0.1", _server.Port);
        var stream = tcpClient.GetStream();

        string json = $"{{\"version\":1,\"messageType\":\"HELLO_AGENT\",\"requestId\":\"not-a-valid-uuid-123\",\"deviceId\":\"LAB01-PC16\",\"sentAtUtc\":\"{DateTime.UtcNow:O}\",\"payload\":{{\"agentVersion\":\"1.0\",\"hostname\":\"PC\"}}}}";
        await FrameCodec.WriteFrameAsync(stream, Encoding.UTF8.GetBytes(json));

        byte[]? respBytes = await FrameCodec.ReadFrameAsync(stream);
        Assert.NotNull(respBytes);

        var resp = ProtocolSerializer.DeserializeEnvelope(respBytes);
        Assert.NotNull(resp);
        Assert.Equal(MessageTypes.Error, resp.MessageType);

        var error = ProtocolSerializer.ExtractPayload<ErrorPayload>(resp.Payload);
        Assert.NotNull(error);
        Assert.Equal(ErrorCodes.InvalidPayload, error.ErrorCode);
    }

    // =========================================================================
    // TEST 17: Envelope with empty deviceId is rejected
    // =========================================================================
    [Fact]
    public async Task Envelope_EmptyDeviceId_IsRejected()
    {
        _server = CreateAndStartServer();
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync("127.0.0.1", _server.Port);
        var stream = tcpClient.GetStream();

        string id = Guid.NewGuid().ToString("D");
        string json = $"{{\"version\":1,\"messageType\":\"HELLO_AGENT\",\"requestId\":\"{id}\",\"deviceId\":\"\",\"sentAtUtc\":\"{DateTime.UtcNow:O}\",\"payload\":{{\"agentVersion\":\"1.0\",\"hostname\":\"PC\"}}}}";
        await FrameCodec.WriteFrameAsync(stream, Encoding.UTF8.GetBytes(json));

        byte[]? respBytes = await FrameCodec.ReadFrameAsync(stream);
        Assert.NotNull(respBytes);

        var resp = ProtocolSerializer.DeserializeEnvelope(respBytes);
        Assert.NotNull(resp);
        Assert.Equal(MessageTypes.Error, resp.MessageType);

        var error = ProtocolSerializer.ExtractPayload<ErrorPayload>(resp.Payload);
        Assert.NotNull(error);
        Assert.Equal(ErrorCodes.InvalidPayload, error.ErrorCode);
    }

    // =========================================================================
    // TEST 18: Envelope with unsupported version is rejected
    // =========================================================================
    [Fact]
    public async Task Envelope_UnsupportedVersion_IsRejected()
    {
        _server = CreateAndStartServer();
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync("127.0.0.1", _server.Port);
        var stream = tcpClient.GetStream();

        string id = Guid.NewGuid().ToString("D");
        string json = $"{{\"version\":2,\"messageType\":\"HELLO_AGENT\",\"requestId\":\"{id}\",\"deviceId\":\"LAB01-PC18\",\"sentAtUtc\":\"{DateTime.UtcNow:O}\",\"payload\":{{\"agentVersion\":\"1.0\",\"hostname\":\"PC\"}}}}";
        await FrameCodec.WriteFrameAsync(stream, Encoding.UTF8.GetBytes(json));

        byte[]? respBytes = await FrameCodec.ReadFrameAsync(stream);
        Assert.NotNull(respBytes);

        var resp = ProtocolSerializer.DeserializeEnvelope(respBytes);
        Assert.NotNull(resp);
        Assert.Equal(MessageTypes.Error, resp.MessageType);

        var error = ProtocolSerializer.ExtractPayload<ErrorPayload>(resp.Payload);
        Assert.NotNull(error);
        Assert.Equal(ErrorCodes.InvalidPayload, error.ErrorCode);
    }

    // =========================================================================
    // TEST 19: Source scan proving NO hardcoded secrets under src/
    // =========================================================================
    [Fact]
    public void SourceSecurityScan_NoHardcodedSecretsUnderSrc()
    {
        string[] forbiddenStrings =
        [
            "LABGUARD_AGENT_SECRET_KEY_2026",
            "TEST_ONLY_AGENT_TOKEN_DO_NOT_USE_IN_PRODUCTION"
        ];

        // Find solution src directory
        string baseDir = AppContext.BaseDirectory;
        DirectoryInfo? dir = new DirectoryInfo(baseDir);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "src")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        string srcPath = Path.Combine(dir.FullName, "src");
        Assert.True(Directory.Exists(srcPath));

        var csFiles = Directory.GetFiles(srcPath, "*.cs", SearchOption.AllDirectories);
        Assert.NotEmpty(csFiles);

        var violations = new List<string>();
        foreach (var file in csFiles)
        {
            string content = File.ReadAllText(file);
            foreach (var forbidden in forbiddenStrings)
            {
                if (content.Contains(forbidden, StringComparison.OrdinalIgnoreCase))
                {
                    violations.Add($"File '{Path.GetFileName(file)}' contains forbidden string '{forbidden}'.");
                }
            }
        }

        Assert.Empty(violations);
    }

    // =========================================================================
    // TEST 20: Envelope with UUIDv1 requestId is rejected over TCP
    // =========================================================================
    [Fact]
    public async Task Envelope_UuidV1RequestId_IsRejectedOverTcp()
    {
        _server = CreateAndStartServer();
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync("127.0.0.1", _server.Port);
        var stream = tcpClient.GetStream();

        string uuidV1 = "6ba7b810-9dad-11d1-80b4-00c04fd430c8";
        string json = $"{{\"version\":1,\"messageType\":\"HELLO_AGENT\",\"requestId\":\"{uuidV1}\",\"deviceId\":\"LAB01-PC20\",\"sentAtUtc\":\"{DateTime.UtcNow:O}\",\"payload\":{{\"agentVersion\":\"1.0\",\"hostname\":\"PC\"}}}}";
        await FrameCodec.WriteFrameAsync(stream, Encoding.UTF8.GetBytes(json));

        byte[]? respBytes = await FrameCodec.ReadFrameAsync(stream);
        Assert.NotNull(respBytes);

        var resp = ProtocolSerializer.DeserializeEnvelope(respBytes);
        Assert.NotNull(resp);
        Assert.Equal(MessageTypes.Error, resp.MessageType);

        var error = ProtocolSerializer.ExtractPayload<ErrorPayload>(resp.Payload);
        Assert.NotNull(error);
        Assert.Equal(ErrorCodes.InvalidPayload, error.ErrorCode);
    }

    // =========================================================================
    // TEST 21: sentAtUtc UTC zero offset enforcement over TCP
    // =========================================================================
    [Fact]
    public async Task Envelope_SentAtUtc_EnforcesZeroOffsetOverTcp()
    {
        _server = CreateAndStartServer();

        // Subcase A: +07:00 offset is rejected with ERR_INVALID_PAYLOAD
        {
            using var client7 = new TcpClient();
            await client7.ConnectAsync("127.0.0.1", _server.Port);
            var stream7 = client7.GetStream();

            string id7 = Guid.NewGuid().ToString("D");
            string json7 = $"{{\"version\":1,\"messageType\":\"HELLO_AGENT\",\"requestId\":\"{id7}\",\"deviceId\":\"LAB01-PC21A\",\"sentAtUtc\":\"2026-09-14T14:00:00+07:00\",\"payload\":{{\"agentVersion\":\"1.0\",\"hostname\":\"PC\"}}}}";
            await FrameCodec.WriteFrameAsync(stream7, Encoding.UTF8.GetBytes(json7));

            byte[]? resp7 = await FrameCodec.ReadFrameAsync(stream7);
            Assert.NotNull(resp7);
            var env7 = ProtocolSerializer.DeserializeEnvelope(resp7);
            Assert.NotNull(env7);
            Assert.Equal(MessageTypes.Error, env7.MessageType);
            var err7 = ProtocolSerializer.ExtractPayload<ErrorPayload>(env7.Payload);
            Assert.Equal(ErrorCodes.InvalidPayload, err7?.ErrorCode);
        }

        // Subcase B: +00:00 offset is accepted with HELLO_ACK
        {
            using var client0 = new TcpClient();
            await client0.ConnectAsync("127.0.0.1", _server.Port);
            var stream0 = client0.GetStream();

            string id0 = Guid.NewGuid().ToString("D");
            string json0 = $"{{\"version\":1,\"messageType\":\"HELLO_AGENT\",\"requestId\":\"{id0}\",\"deviceId\":\"LAB01-PC21B\",\"sentAtUtc\":\"2026-09-14T07:00:00+00:00\",\"payload\":{{\"agentVersion\":\"1.0\",\"hostname\":\"PC\"}}}}";
            await FrameCodec.WriteFrameAsync(stream0, Encoding.UTF8.GetBytes(json0));

            byte[]? resp0 = await FrameCodec.ReadFrameAsync(stream0);
            Assert.NotNull(resp0);
            var env0 = ProtocolSerializer.DeserializeEnvelope(resp0);
            Assert.NotNull(env0);
            Assert.Equal(MessageTypes.HelloAck, env0.MessageType);
        }

        // Subcase C: Z offset is accepted with HELLO_ACK
        {
            using var clientZ = new TcpClient();
            await clientZ.ConnectAsync("127.0.0.1", _server.Port);
            var streamZ = clientZ.GetStream();

            string idZ = Guid.NewGuid().ToString("D");
            string jsonZ = $"{{\"version\":1,\"messageType\":\"HELLO_AGENT\",\"requestId\":\"{idZ}\",\"deviceId\":\"LAB01-PC21C\",\"sentAtUtc\":\"2026-09-14T07:00:00.0000000Z\",\"payload\":{{\"agentVersion\":\"1.0\",\"hostname\":\"PC\"}}}}";
            await FrameCodec.WriteFrameAsync(streamZ, Encoding.UTF8.GetBytes(jsonZ));

            byte[]? respZ = await FrameCodec.ReadFrameAsync(streamZ);
            Assert.NotNull(respZ);
            var envZ = ProtocolSerializer.DeserializeEnvelope(respZ);
            Assert.NotNull(envZ);
            Assert.Equal(MessageTypes.HelloAck, envZ.MessageType);
        }
    }

    // =========================================================================
    // TEST 22: HELLO_AGENT payload missing required fields is rejected over TCP
    // =========================================================================
    [Fact]
    public async Task HelloAgentPayload_MissingRequiredFields_IsRejectedOverTcp()
    {
        _server = CreateAndStartServer();

        // 1. Empty payload object {}
        {
            using var client = new TcpClient();
            await client.ConnectAsync("127.0.0.1", _server.Port);
            var stream = client.GetStream();

            string id = Guid.NewGuid().ToString("D");
            string json = $"{{\"version\":1,\"messageType\":\"HELLO_AGENT\",\"requestId\":\"{id}\",\"deviceId\":\"LAB01-PC22A\",\"sentAtUtc\":\"{DateTime.UtcNow:O}\",\"payload\":{{}}}}";
            await FrameCodec.WriteFrameAsync(stream, Encoding.UTF8.GetBytes(json));

            byte[]? resp = await FrameCodec.ReadFrameAsync(stream);
            Assert.NotNull(resp);
            var env = ProtocolSerializer.DeserializeEnvelope(resp);
            Assert.NotNull(env);
            Assert.Equal(MessageTypes.Error, env.MessageType);
            var err = ProtocolSerializer.ExtractPayload<ErrorPayload>(env.Payload);
            Assert.Equal(ErrorCodes.InvalidPayload, err?.ErrorCode);
        }

        // 2. Missing agentVersion (only hostname present)
        {
            using var client = new TcpClient();
            await client.ConnectAsync("127.0.0.1", _server.Port);
            var stream = client.GetStream();

            string id = Guid.NewGuid().ToString("D");
            string json = $"{{\"version\":1,\"messageType\":\"HELLO_AGENT\",\"requestId\":\"{id}\",\"deviceId\":\"LAB01-PC22B\",\"sentAtUtc\":\"{DateTime.UtcNow:O}\",\"payload\":{{\"hostname\":\"PC01\"}}}}";
            await FrameCodec.WriteFrameAsync(stream, Encoding.UTF8.GetBytes(json));

            byte[]? resp = await FrameCodec.ReadFrameAsync(stream);
            Assert.NotNull(resp);
            var env = ProtocolSerializer.DeserializeEnvelope(resp);
            Assert.NotNull(env);
            Assert.Equal(MessageTypes.Error, env.MessageType);
            var err = ProtocolSerializer.ExtractPayload<ErrorPayload>(env.Payload);
            Assert.Equal(ErrorCodes.InvalidPayload, err?.ErrorCode);
        }

        // 3. Missing hostname (only agentVersion present)
        {
            using var client = new TcpClient();
            await client.ConnectAsync("127.0.0.1", _server.Port);
            var stream = client.GetStream();

            string id = Guid.NewGuid().ToString("D");
            string json = $"{{\"version\":1,\"messageType\":\"HELLO_AGENT\",\"requestId\":\"{id}\",\"deviceId\":\"LAB01-PC22C\",\"sentAtUtc\":\"{DateTime.UtcNow:O}\",\"payload\":{{\"agentVersion\":\"1.0.0\"}}}}";
            await FrameCodec.WriteFrameAsync(stream, Encoding.UTF8.GetBytes(json));

            byte[]? resp = await FrameCodec.ReadFrameAsync(stream);
            Assert.NotNull(resp);
            var env = ProtocolSerializer.DeserializeEnvelope(resp);
            Assert.NotNull(env);
            Assert.Equal(MessageTypes.Error, env.MessageType);
            var err = ProtocolSerializer.ExtractPayload<ErrorPayload>(env.Payload);
            Assert.Equal(ErrorCodes.InvalidPayload, err?.ErrorCode);
        }

        // 4. Valid AgentSocketClient HELLO must still PASS
        {
            await using var agentClient = new AgentSocketClient(LogTrace);
            await agentClient.ConnectAsync("127.0.0.1", _server.Port);
            var ack = await agentClient.SendHelloAsync("LAB01-PC22D");
            Assert.NotNull(ack);
            Assert.Equal("1.0.0", ack.ServerVersion);
        }
    }

    // =========================================================================
    // TEST 23: Rapid connect/disconnect leaves zero active connections
    // =========================================================================
    [Fact]
    public async Task ConnectionRace_RapidConnectDisconnect_LeavesZeroActiveConnections()
    {
        _server = CreateAndStartServer();

        // Connect and immediately close 50 times
        for (int i = 0; i < 50; i++)
        {
            using var client = new TcpClient();
            await client.ConnectAsync("127.0.0.1", _server.Port);
            client.Close();
        }

        // Bounded wait for ActiveConnectionCount to drain to 0
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (_server.ActiveConnectionCount > 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
        }

        Assert.Equal(0, _server.ActiveConnectionCount);

        // Clean client connects and performs handshake successfully
        await using var cleanClient = new AgentSocketClient(LogTrace);
        await cleanClient.ConnectAsync("127.0.0.1", _server.Port);
        var ack = await cleanClient.SendHelloAsync("LAB01-CLEAN");
        Assert.NotNull(ack);
        Assert.Equal("1.0.0", ack.ServerVersion);
    }

    // =========================================================================
    // TEST 24: Agent AGENT_GOODBYE receives correlated GOODBYE_ACK
    // =========================================================================
    [Fact]
    public async Task AgentGoodbye_AwaitsCorrelatedGoodbyeAck()
    {
        _server = CreateAndStartServer();
        await using var client = new AgentSocketClient(LogTrace);

        await client.ConnectAsync("127.0.0.1", _server.Port);
        await client.SendHelloAsync("LAB01-GOODBYE");
        await client.SendAuthAsync("LAB01-GOODBYE", TestAgentToken);

        Assert.True(client.IsAuthenticated);

        // Disconnect gracefully sends AGENT_GOODBYE and awaits correlated GOODBYE_ACK
        await client.DisconnectAsync();
        Assert.False(client.IsConnected);

        // Assert trace contains correlated GOODBYE_ACK verification
        lock (_traceLogs)
        {
            Assert.Contains(_traceLogs, l => l.Contains("Received correlated GOODBYE_ACK"));
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_server != null)
        {
            await _server.DisposeAsync();
            _server = null;
        }
    }
}
