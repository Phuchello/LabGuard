using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using LabGuard.Protocol;
using LabGuard.Protocol.Dtos;
using LabGuard.Protocol.Serialization;
using Xunit;
using Xunit.Abstractions;

namespace LabGuard.Central.Tests;

public class IncidentWorkflowAndOccTests
{
    private readonly ITestOutputHelper _output;
    private readonly string _dbConnectionString = "Server=localhost;Database=LabGuardDb;Trusted_Connection=True;TrustServerCertificate=True;";

    public IncidentWorkflowAndOccTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private void Log(string msg)
    {
        _output.WriteLine(msg);
    }

    [Fact]
    public async Task IncidentAck_WithMatchingRowVersion_TransitionsToAcknowledged_AndUpdatesRowVersion()
    {
        var db = new DatabaseRepository(_dbConnectionString, Log);
        await db.EnsureDatabaseInitializedAsync();

        string deviceId = "TEST-PC-ACK";
        string ruleType = "HIGH_CPU";
        await db.RecoverIncidentAsync(deviceId, ruleType);

        var opened = await db.OpenOrUpdateIncidentAsync(deviceId, ruleType, "HIGH", 92.0);
        Assert.NotNull(opened);
        Assert.Equal("OPEN", opened.WorkflowState);

        var cmd = new IncidentCommandPayload
        {
            CommandId = Guid.NewGuid().ToString("D"),
            CommandType = MessageTypes.IncidentAck,
            IncidentId = opened.IncidentId,
            ExpectedRowVersion = opened.RowVersionHex
        };

        var result = await db.ExecuteIncidentCommandAsync(1, cmd);

        Assert.True(result.Success);
        Assert.Equal(200, result.StatusCode);
        Assert.NotNull(result.UpdatedRowVersionHex);
        Assert.NotEqual(opened.RowVersionHex, result.UpdatedRowVersionHex);

        var updated = await db.GetIncidentByIdAsync(opened.IncidentId);
        Assert.NotNull(updated);
        Assert.Equal("ACKNOWLEDGED", updated.WorkflowState);
        Assert.Equal(result.UpdatedRowVersionHex, updated.RowVersionHex);
    }

    [Fact]
    public async Task IncidentAssign_WithAssignedUserId_TransitionsToInProgress()
    {
        var db = new DatabaseRepository(_dbConnectionString, Log);
        await db.EnsureDatabaseInitializedAsync();

        string deviceId = "TEST-PC-ASSIGN";
        string ruleType = "HIGH_MEMORY";
        await db.RecoverIncidentAsync(deviceId, ruleType);

        var opened = await db.OpenOrUpdateIncidentAsync(deviceId, ruleType, "HIGH", 88.0);
        Assert.NotNull(opened);

        var cmd = new IncidentCommandPayload
        {
            CommandId = Guid.NewGuid().ToString("D"),
            CommandType = MessageTypes.IncidentAssign,
            IncidentId = opened.IncidentId,
            ExpectedRowVersion = opened.RowVersionHex,
            CommandParams = new Dictionary<string, object?>
            {
                ["assignedToUserId"] = 2
            }
        };

        var result = await db.ExecuteIncidentCommandAsync(1, cmd);

        Assert.True(result.Success);
        Assert.Equal(200, result.StatusCode);

        var updated = await db.GetIncidentByIdAsync(opened.IncidentId);
        Assert.NotNull(updated);
        Assert.Equal("IN_PROGRESS", updated.WorkflowState);
    }

    [Fact]
    public async Task IncidentStart_TransitionsToInProgress()
    {
        var db = new DatabaseRepository(_dbConnectionString, Log);
        await db.EnsureDatabaseInitializedAsync();

        string deviceId = "TEST-PC-START";
        string ruleType = "SERVICE_DOWN";
        await db.RecoverIncidentAsync(deviceId, ruleType);

        var opened = await db.OpenOrUpdateIncidentAsync(deviceId, ruleType, "CRITICAL", 0.0);
        Assert.NotNull(opened);

        var cmd = new IncidentCommandPayload
        {
            CommandId = Guid.NewGuid().ToString("D"),
            CommandType = MessageTypes.IncidentStart,
            IncidentId = opened.IncidentId,
            ExpectedRowVersion = opened.RowVersionHex
        };

        var result = await db.ExecuteIncidentCommandAsync(1, cmd);

        Assert.True(result.Success);
        Assert.Equal(200, result.StatusCode);

        var updated = await db.GetIncidentByIdAsync(opened.IncidentId);
        Assert.NotNull(updated);
        Assert.Equal("IN_PROGRESS", updated.WorkflowState);
    }

    [Fact]
    public async Task IncidentResolve_WhenConditionActive_IsRejectedWith400()
    {
        var db = new DatabaseRepository(_dbConnectionString, Log);
        await db.EnsureDatabaseInitializedAsync();

        string deviceId = "TEST-PC-RES-ACT";
        string ruleType = "HIGH_CPU";
        await db.RecoverIncidentAsync(deviceId, ruleType);

        var opened = await db.OpenOrUpdateIncidentAsync(deviceId, ruleType, "HIGH", 94.0);
        Assert.NotNull(opened);
        Assert.Equal("ACTIVE", opened.ConditionState);

        var cmd = new IncidentCommandPayload
        {
            CommandId = Guid.NewGuid().ToString("D"),
            CommandType = MessageTypes.IncidentResolve,
            IncidentId = opened.IncidentId,
            ExpectedRowVersion = opened.RowVersionHex,
            CommandParams = new Dictionary<string, object?>
            {
                ["resolutionNote"] = "Trying to close while still hot"
            }
        };

        var result = await db.ExecuteIncidentCommandAsync(1, cmd);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("ERR_CONDITION_STILL_ACTIVE", result.ErrorCode);

        var unchanged = await db.GetIncidentByIdAsync(opened.IncidentId);
        Assert.NotNull(unchanged);
        Assert.NotEqual("RESOLVED", unchanged.WorkflowState);
    }

    [Fact]
    public async Task IncidentResolve_WhenConditionRecoveredAndNoteProvided_TransitionsToResolved()
    {
        var db = new DatabaseRepository(_dbConnectionString, Log);
        await db.EnsureDatabaseInitializedAsync();

        string deviceId = "TEST-PC-RES-OK";
        string ruleType = "DEVICE_OFFLINE";
        await db.RecoverIncidentAsync(deviceId, ruleType);

        var opened = await db.OpenOrUpdateIncidentAsync(deviceId, ruleType, "CRITICAL", 22.0);
        Assert.NotNull(opened);

        // Transition condition to RECOVERED
        var recovered = await db.RecoverIncidentAsync(deviceId, ruleType);
        Assert.NotNull(recovered);
        Assert.Equal("RECOVERED", recovered.ConditionState);

        var cmd = new IncidentCommandPayload
        {
            CommandId = Guid.NewGuid().ToString("D"),
            CommandType = MessageTypes.IncidentResolve,
            IncidentId = recovered.IncidentId,
            ExpectedRowVersion = recovered.RowVersionHex,
            CommandParams = new Dictionary<string, object?>
            {
                ["resolutionNote"] = "Đã kiểm tra lại kết nối cáp mạng phòng máy."
            }
        };

        var result = await db.ExecuteIncidentCommandAsync(1, cmd);

        Assert.True(result.Success);
        Assert.Equal(200, result.StatusCode);

        var resolved = await db.GetIncidentByIdAsync(recovered.IncidentId);
        Assert.NotNull(resolved);
        Assert.Equal("RESOLVED", resolved.WorkflowState);
    }

    [Fact]
    public async Task IncidentCommand_WithMismatchedRowVersion_RejectsWith409Conflict_WithoutMutatingDatabase()
    {
        var db = new DatabaseRepository(_dbConnectionString, Log);
        await db.EnsureDatabaseInitializedAsync();

        string deviceId = "TEST-PC-OCC";
        string ruleType = "HIGH_CPU";
        await db.RecoverIncidentAsync(deviceId, ruleType);

        var opened = await db.OpenOrUpdateIncidentAsync(deviceId, ruleType, "HIGH", 91.0);
        Assert.NotNull(opened);

        // Stale expected token that does not match actual rowversion
        string staleRowVersion = "0x0000000000000001";

        var cmd = new IncidentCommandPayload
        {
            CommandId = Guid.NewGuid().ToString("D"),
            CommandType = MessageTypes.IncidentAck,
            IncidentId = opened.IncidentId,
            ExpectedRowVersion = staleRowVersion
        };

        var result = await db.ExecuteIncidentCommandAsync(1, cmd);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal("ERR_CONCURRENCY_CONFLICT", result.ErrorCode);

        // Database row must NOT be mutated
        var unchanged = await db.GetIncidentByIdAsync(opened.IncidentId);
        Assert.NotNull(unchanged);
        Assert.Equal("OPEN", unchanged.WorkflowState);
        Assert.Equal(opened.RowVersionHex, unchanged.RowVersionHex);
    }

    [Fact]
    public async Task IncidentCommand_DuplicateWithSameDigest_ReturnsIdempotentSuccess_WithoutReexecuting()
    {
        var db = new DatabaseRepository(_dbConnectionString, Log);
        await db.EnsureDatabaseInitializedAsync();

        string deviceId = "TEST-PC-IDEMP";
        string ruleType = "HIGH_MEMORY";
        await db.RecoverIncidentAsync(deviceId, ruleType);

        var opened = await db.OpenOrUpdateIncidentAsync(deviceId, ruleType, "HIGH", 89.0);
        Assert.NotNull(opened);

        string commandId = Guid.NewGuid().ToString("D");

        var cmd = new IncidentCommandPayload
        {
            CommandId = commandId,
            CommandType = MessageTypes.IncidentAck,
            IncidentId = opened.IncidentId,
            ExpectedRowVersion = opened.RowVersionHex
        };

        // 1. First execution -> 200 OK
        var result1 = await db.ExecuteIncidentCommandAsync(1, cmd);
        Assert.True(result1.Success);
        Assert.Equal(200, result1.StatusCode);
        string newRv = result1.UpdatedRowVersionHex!;

        // 2. Replay with identical parameters and commandId -> Must return cached 200 OK without error
        var result2 = await db.ExecuteIncidentCommandAsync(1, cmd);
        Assert.True(result2.Success);
        Assert.Equal(200, result2.StatusCode);
        Assert.Equal(newRv, result2.UpdatedRowVersionHex);
    }

    [Fact]
    public async Task IncidentCommand_DuplicateWithDifferentDigest_RejectsWith409IdempotencyKeyReuse()
    {
        var db = new DatabaseRepository(_dbConnectionString, Log);
        await db.EnsureDatabaseInitializedAsync();

        string deviceId = "TEST-PC-REUSE";
        string ruleType = "SERVICE_DOWN";
        await db.RecoverIncidentAsync(deviceId, ruleType);

        var opened = await db.OpenOrUpdateIncidentAsync(deviceId, ruleType, "HIGH", 0.0);
        Assert.NotNull(opened);

        string commandId = Guid.NewGuid().ToString("D");

        var cmd1 = new IncidentCommandPayload
        {
            CommandId = commandId,
            CommandType = MessageTypes.IncidentAck,
            IncidentId = opened.IncidentId,
            ExpectedRowVersion = opened.RowVersionHex
        };

        var result1 = await db.ExecuteIncidentCommandAsync(1, cmd1);
        Assert.True(result1.Success);
        Assert.Equal(200, result1.StatusCode);

        // Send a different command reusing the same commandId
        var cmd2 = new IncidentCommandPayload
        {
            CommandId = commandId, // Same commandId
            CommandType = MessageTypes.IncidentResolve, // Different commandType and payload
            IncidentId = opened.IncidentId,
            ExpectedRowVersion = opened.RowVersionHex,
            CommandParams = new Dictionary<string, object?> { ["resolutionNote"] = "Different action" }
        };

        var result2 = await db.ExecuteIncidentCommandAsync(1, cmd2);

        Assert.False(result2.Success);
        Assert.Equal(409, result2.StatusCode);
        Assert.Equal("ERR_IDEMPOTENCY_KEY_REUSE", result2.ErrorCode);
    }

    [Fact]
    public async Task IncidentCommand_OverTcp_SucceedsAndReceivesCommandResult()
    {
        var db = new DatabaseRepository(_dbConnectionString, Log);
        await db.EnsureDatabaseInitializedAsync();

        var server = new CentralServer(
            expectedToken: "TEST_TOKEN",
            ipAddress: IPAddress.Loopback,
            port: 0,
            database: db,
            logger: Log);

        server.Start();

        try
        {
            using var tcp = new TcpClient();
            await tcp.ConnectAsync("127.0.0.1", server.Port);
            var stream = tcp.GetStream();

            // 1. HELLO_ADMIN
            var helloEnv = WireEnvelope<HelloAdminPayload>.Create(
                MessageTypes.HelloAdmin,
                "ADMIN-CONSOLE",
                new HelloAdminPayload { ClientVersion = "1.0.0", Hostname = "TEST-ADMIN" },
                Guid.NewGuid().ToString("D"));
            await FrameCodec.WriteFrameAsync(stream, ProtocolSerializer.Serialize(helloEnv));

            byte[]? helloAckBytes = await FrameCodec.ReadFrameAsync(stream, FrameCodec.MaxPayloadSize);
            Assert.NotNull(helloAckBytes);
            var helloAckEnv = ProtocolSerializer.DeserializeEnvelope(helloAckBytes);
            Assert.NotNull(helloAckEnv);
            Assert.Equal(MessageTypes.HelloAck, helloAckEnv.MessageType);

            // 2. LOGIN
            var loginEnv = WireEnvelope<LoginPayload>.Create(
                MessageTypes.Login,
                "ADMIN-CONSOLE",
                new LoginPayload { Username = "admin", Password = "Admin@123" },
                Guid.NewGuid().ToString("D"));
            await FrameCodec.WriteFrameAsync(stream, ProtocolSerializer.Serialize(loginEnv));

            byte[]? loginAckBytes = await FrameCodec.ReadFrameAsync(stream, FrameCodec.MaxPayloadSize);
            Assert.NotNull(loginAckBytes);
            var loginAckEnv = ProtocolSerializer.DeserializeEnvelope(loginAckBytes);
            Assert.NotNull(loginAckEnv);
            Assert.Equal(MessageTypes.LoginResult, loginAckEnv.MessageType);

            // 3. Open test incident
            string deviceId = "TEST-TCP-OCC";
            string ruleType = "HIGH_CPU";
            await db.RecoverIncidentAsync(deviceId, ruleType);
            var opened = await db.OpenOrUpdateIncidentAsync(deviceId, ruleType, "HIGH", 93.0);
            Assert.NotNull(opened);

            // 4. Send INCIDENT_ACK over TCP
            string reqId = Guid.NewGuid().ToString("D");
            var ackCmd = new IncidentCommandPayload
            {
                CommandId = Guid.NewGuid().ToString("D"),
                CommandType = MessageTypes.IncidentAck,
                IncidentId = opened.IncidentId,
                ExpectedRowVersion = opened.RowVersionHex
            };
            var ackEnv = WireEnvelope<IncidentCommandPayload>.Create(
                MessageTypes.IncidentAck,
                "ADMIN-CONSOLE",
                ackCmd,
                reqId);

            await FrameCodec.WriteFrameAsync(stream, ProtocolSerializer.Serialize(ackEnv));

            WireEnvelope<JsonElement>? ackResultEnv = null;
            WireEnvelope<JsonElement>? broadcastUpdateEnv = null;

            for (int i = 0; i < 2; i++)
            {
                byte[]? frameBytes = await FrameCodec.ReadFrameAsync(stream, FrameCodec.MaxPayloadSize);
                Assert.NotNull(frameBytes);
                var env = ProtocolSerializer.DeserializeEnvelope(frameBytes);
                Assert.NotNull(env);
                if (env.MessageType == MessageTypes.IncidentUpdate)
                {
                    broadcastUpdateEnv = env;
                }
                else if (env.MessageType == MessageTypes.CommandResult)
                {
                    ackResultEnv = env;
                }
            }

            Assert.NotNull(broadcastUpdateEnv);
            Assert.NotNull(ackResultEnv);
            Assert.Equal(reqId, ackResultEnv.RequestId);

            var cmdResult = ProtocolSerializer.ExtractPayload<CommandResultPayload>(ackResultEnv.Payload);
            Assert.NotNull(cmdResult);
            Assert.True(cmdResult.Success);
            Assert.Equal(200, cmdResult.StatusCode);
            Assert.NotNull(cmdResult.UpdatedRowVersionHex);

            // 5. Send stale INCIDENT_ACK over TCP -> 409 OCC Conflict
            string staleReqId = Guid.NewGuid().ToString("D");
            var staleCmd = new IncidentCommandPayload
            {
                CommandId = Guid.NewGuid().ToString("D"),
                CommandType = MessageTypes.IncidentAck,
                IncidentId = opened.IncidentId,
                ExpectedRowVersion = "0x0000000000000001" // Stale
            };
            var staleEnv = WireEnvelope<IncidentCommandPayload>.Create(
                MessageTypes.IncidentAck,
                "ADMIN-CONSOLE",
                staleCmd,
                staleReqId);

            await FrameCodec.WriteFrameAsync(stream, ProtocolSerializer.Serialize(staleEnv));

            byte[]? staleResultBytes = await FrameCodec.ReadFrameAsync(stream, FrameCodec.MaxPayloadSize);
            Assert.NotNull(staleResultBytes);
            var staleResultEnv = ProtocolSerializer.DeserializeEnvelope(staleResultBytes);
            Assert.NotNull(staleResultEnv);
            Assert.Equal(MessageTypes.CommandResult, staleResultEnv.MessageType);

            var staleCmdResult = ProtocolSerializer.ExtractPayload<CommandResultPayload>(staleResultEnv.Payload);
            Assert.NotNull(staleCmdResult);
            Assert.False(staleCmdResult.Success);
            Assert.Equal(409, staleCmdResult.StatusCode);
            Assert.Equal("ERR_CONCURRENCY_CONFLICT", staleCmdResult.ErrorCode);
        }
        finally
        {
            await server.StopAsync();
        }
    }
}
