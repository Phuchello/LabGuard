using System.Diagnostics;
using System.Net;
using LabGuard.Agent;
using LabGuard.Protocol.Dtos;
using Xunit;
using Xunit.Abstractions;

namespace LabGuard.Central.Tests;

public class HeartbeatLivenessTests : IAsyncDisposable
{
    private const string TestAgentToken = "TEST_ONLY_AGENT_TOKEN_DO_NOT_USE_IN_PRODUCTION";
    private readonly ITestOutputHelper _testOutput;
    private readonly List<string> _traceLogs = [];
    private CentralServer? _server;

    public HeartbeatLivenessTests(ITestOutputHelper testOutput)
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

    private CentralServer CreateAndStartServer(
        TimeSpan? checkInterval = null,
        TimeSpan? offlineThreshold = null)
    {
        var server = new CentralServer(
            expectedToken: TestAgentToken,
            ipAddress: IPAddress.Loopback,
            port: 0,
            logger: LogTrace,
            livenessCheckInterval: checkInterval,
            offlineThreshold: offlineThreshold);

        server.Start();
        return server;
    }

    // =========================================================================
    // TEST 1: Authenticated Agent sends HEARTBEAT -> receives HEARTBEAT_ACK
    // =========================================================================
    [Fact]
    public async Task Heartbeat_AuthenticatedAgent_ReceivesHeartbeatAck()
    {
        _server = CreateAndStartServer();
        await using var client = new AgentSocketClient(LogTrace);

        await client.ConnectAsync("127.0.0.1", _server.Port);
        await client.SendHelloAsync("LAB01-PC01");
        await client.SendAuthAsync("LAB01-PC01", TestAgentToken);

        var ack = await client.SendHeartbeatAsync(sequenceNumber: 1, uptimeSeconds: 10);

        Assert.NotNull(ack);
        Assert.Equal(1, ack.AcknowledgedSequence);
        Assert.False(string.IsNullOrEmpty(ack.ServerTimeUtc));
    }

    // =========================================================================
    // TEST 2: Heartbeat updates LastSeen
    // =========================================================================
    [Fact]
    public async Task Heartbeat_UpdatesLastSeenMonotonicTimestamp()
    {
        _server = CreateAndStartServer();
        await using var client = new AgentSocketClient(LogTrace);

        await client.ConnectAsync("127.0.0.1", _server.Port);
        await client.SendHelloAsync("LAB01-PC02");
        await client.SendAuthAsync("LAB01-PC02", TestAgentToken);

        var record = _server.LivenessRegistry.GetRecord("LAB01-PC02");
        Assert.NotNull(record);
        Assert.Equal(0, record.LastSeenMonotonicTimestamp);

        // Send 1st heartbeat
        await client.SendHeartbeatAsync(sequenceNumber: 1, uptimeSeconds: 10);
        long firstLastSeen = record.LastSeenMonotonicTimestamp;
        Assert.True(firstLastSeen > 0);

        await Task.Delay(40);

        // Send 2nd heartbeat
        await client.SendHeartbeatAsync(sequenceNumber: 2, uptimeSeconds: 11);
        long secondLastSeen = record.LastSeenMonotonicTimestamp;
        Assert.True(secondLastSeen > firstLastSeen);
    }

    // =========================================================================
    // TEST 3: Authenticated Agent remains ONLINE while heartbeats continue
    // =========================================================================
    [Fact]
    public async Task Agent_RemainsOnline_WhileHeartbeatsContinue()
    {
        // 30ms scanner interval, 120ms offline threshold
        _server = CreateAndStartServer(
            checkInterval: TimeSpan.FromMilliseconds(30),
            offlineThreshold: TimeSpan.FromMilliseconds(120));

        await using var client = new AgentSocketClient(LogTrace);

        await client.ConnectAsync("127.0.0.1", _server.Port);
        await client.SendHelloAsync("LAB01-PC03");
        await client.SendAuthAsync("LAB01-PC03", TestAgentToken);

        // First heartbeat marks device ONLINE
        await client.SendHeartbeatAsync(1, 10);
        Assert.Equal(DeviceLivenessStatus.Online, _server.LivenessRegistry.GetStatus("LAB01-PC03"));

        // Run background heartbeat every 35ms for 200ms (> 120ms offline threshold)
        client.StartHeartbeat(TimeSpan.FromMilliseconds(35));
        await Task.Delay(200);

        Assert.Equal(DeviceLivenessStatus.Online, _server.LivenessRegistry.GetStatus("LAB01-PC03"));
        await client.StopHeartbeatAsync();
    }

    // =========================================================================
    // TEST 4: Stop heartbeats. Advance/wait past OfflineThreshold. Server changes ONLINE -> OFFLINE
    // =========================================================================
    [Fact]
    public async Task Agent_TransitionsFromOnlineToOffline_WhenHeartbeatsStop()
    {
        var transitions = new List<(string DeviceId, DeviceLivenessStatus Old, DeviceLivenessStatus New)>();

        _server = CreateAndStartServer(
            checkInterval: TimeSpan.FromMilliseconds(25),
            offlineThreshold: TimeSpan.FromMilliseconds(100));

        _server.LivenessRegistry.OnStatusChanged += (dev, oldS, newS) =>
        {
            transitions.Add((dev, oldS, newS));
        };

        await using var client = new AgentSocketClient(LogTrace);

        await client.ConnectAsync("127.0.0.1", _server.Port);
        await client.SendHelloAsync("LAB01-PC04");
        await client.SendAuthAsync("LAB01-PC04", TestAgentToken);

        // Initial heartbeat -> ONLINE
        await client.SendHeartbeatAsync(1, 10);
        Assert.Equal(DeviceLivenessStatus.Online, _server.LivenessRegistry.GetStatus("LAB01-PC04"));

        // Stop sending heartbeats and wait past threshold (100ms) + scanner margin
        var deadline = DateTime.UtcNow.AddMilliseconds(500);
        while (_server.LivenessRegistry.GetStatus("LAB01-PC04") == DeviceLivenessStatus.Online && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20);
        }

        Assert.Equal(DeviceLivenessStatus.Offline, _server.LivenessRegistry.GetStatus("LAB01-PC04"));

        // Assert transition ONLINE -> OFFLINE was recorded exactly once
        var offlineTransitions = transitions.Where(t => t.DeviceId == "LAB01-PC04" && t.New == DeviceLivenessStatus.Offline).ToList();
        Assert.Single(offlineTransitions);
    }

    // =========================================================================
    // TEST 5: Verify OFFLINE is detected on scanner tick after > threshold, not assumed exactly at 0s
    // =========================================================================
    [Fact]
    public async Task OfflineDetection_OccursOnScannerTick_AfterThresholdElapsed()
    {
        long offlineDetectedTimestamp = 0;
        long lastSeenAtT0 = 0;

        _server = CreateAndStartServer(
            checkInterval: TimeSpan.FromMilliseconds(40),
            offlineThreshold: TimeSpan.FromMilliseconds(120));

        _server.LivenessRegistry.OnStatusChanged += (dev, oldS, newS) =>
        {
            if (dev == "LAB01-PC05" && newS == DeviceLivenessStatus.Offline)
            {
                offlineDetectedTimestamp = Stopwatch.GetTimestamp();
            }
        };

        await using var client = new AgentSocketClient(LogTrace);

        await client.ConnectAsync("127.0.0.1", _server.Port);
        await client.SendHelloAsync("LAB01-PC05");
        await client.SendAuthAsync("LAB01-PC05", TestAgentToken);

        await client.SendHeartbeatAsync(1, 10);
        lastSeenAtT0 = _server.LivenessRegistry.GetRecord("LAB01-PC05")!.LastSeenMonotonicTimestamp;

        // Await offline detection
        var deadline = DateTime.UtcNow.AddMilliseconds(600);
        while (offlineDetectedTimestamp == 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(15);
        }

        Assert.True(offlineDetectedTimestamp > 0, "OFFLINE status should have been detected.");

        double elapsedSeconds = (offlineDetectedTimestamp - lastSeenAtT0) / (double)Stopwatch.Frequency;

        // Must be strictly greater than offline threshold (0.120s)
        Assert.True(elapsedSeconds > 0.115, $"Detection must happen after threshold (got {elapsedSeconds:F3}s)");
        // And reasonably within scanner tick window (0.120s + 0.040s + reasonable OS jitter)
        Assert.True(elapsedSeconds < 0.350, $"Detection must happen near scanner tick (got {elapsedSeconds:F3}s)");
    }

    // =========================================================================
    // TEST 6: HELLO_AGENT without successful AUTH must NOT mark device ONLINE
    // =========================================================================
    [Fact]
    public async Task HelloAgent_WithoutSuccessfulAuth_DoesNotMarkDeviceOnline()
    {
        _server = CreateAndStartServer(
            checkInterval: TimeSpan.FromMilliseconds(20),
            offlineThreshold: TimeSpan.FromMilliseconds(50));

        await using var client = new AgentSocketClient(LogTrace);

        await client.ConnectAsync("127.0.0.1", _server.Port);
        var ack = await client.SendHelloAsync("LAB01-PC06");
        Assert.NotNull(ack);

        // Do not authenticate!
        await Task.Delay(80);

        Assert.Equal(DeviceLivenessStatus.Offline, _server.LivenessRegistry.GetStatus("LAB01-PC06"));
        Assert.Null(_server.LivenessRegistry.GetRecord("LAB01-PC06"));
    }

    // =========================================================================
    // TEST 7: After an OFFLINE device reconnects: HELLO/AUTH succeeds but device remains not recovered until first valid authenticated frame
    // =========================================================================
    [Fact]
    public async Task OfflineDevice_Reconnects_RemainsNotRecoveredUntilFirstAuthenticatedFrame()
    {
        _server = CreateAndStartServer(
            checkInterval: TimeSpan.FromMilliseconds(50),
            offlineThreshold: TimeSpan.FromMilliseconds(200));

        // 1. First session goes ONLINE then OFFLINE
        {
            await using var client1 = new AgentSocketClient(LogTrace);
            await client1.ConnectAsync("127.0.0.1", _server.Port);
            await client1.SendHelloAsync("LAB01-PC07");
            await client1.SendAuthAsync("LAB01-PC07", TestAgentToken);
            await client1.SendHeartbeatAsync(1, 10);
            Assert.Equal(DeviceLivenessStatus.Online, _server.LivenessRegistry.GetStatus("LAB01-PC07"));

            await client1.DisconnectAsync();
        }

        // Wait for scanner to mark device OFFLINE
        var deadline = DateTime.UtcNow.AddMilliseconds(1000);
        while (_server.LivenessRegistry.GetStatus("LAB01-PC07") == DeviceLivenessStatus.Online && DateTime.UtcNow < deadline)
        {
            await Task.Delay(30);
        }
        Assert.Equal(DeviceLivenessStatus.Offline, _server.LivenessRegistry.GetStatus("LAB01-PC07"));

        // 2. Client reconnects: HELLO + AUTH succeed
        await using var client2 = new AgentSocketClient(LogTrace);
        await client2.ConnectAsync("127.0.0.1", _server.Port);
        await client2.SendHelloAsync("LAB01-PC07");
        var authResult = await client2.SendAuthAsync("LAB01-PC07", TestAgentToken);
        Assert.True(authResult.Success);

        // BUT: device must remain not recovered (OFFLINE) before sending first authenticated frame
        Assert.Equal(DeviceLivenessStatus.Offline, _server.LivenessRegistry.GetStatus("LAB01-PC07"));
    }

    // =========================================================================
    // TEST 8: First authenticated HEARTBEAT after reconnect: OFFLINE -> ONLINE
    // =========================================================================
    [Fact]
    public async Task FirstAuthenticatedHeartbeat_AfterReconnect_RecoversOnlineState()
    {
        var transitions = new List<(string DeviceId, DeviceLivenessStatus Old, DeviceLivenessStatus New)>();

        _server = CreateAndStartServer(
            checkInterval: TimeSpan.FromMilliseconds(25),
            offlineThreshold: TimeSpan.FromMilliseconds(80));

        _server.LivenessRegistry.OnStatusChanged += (dev, oldS, newS) =>
        {
            transitions.Add((dev, oldS, newS));
        };

        // 1. Initial session goes ONLINE then OFFLINE
        {
            await using var client1 = new AgentSocketClient(LogTrace);
            await client1.ConnectAsync("127.0.0.1", _server.Port);
            await client1.SendHelloAsync("LAB01-PC08");
            await client1.SendAuthAsync("LAB01-PC08", TestAgentToken);
            await client1.SendHeartbeatAsync(1, 10);
            Assert.Equal(DeviceLivenessStatus.Online, _server.LivenessRegistry.GetStatus("LAB01-PC08"));
            await client1.DisconnectAsync();
        }

        var deadline = DateTime.UtcNow.AddMilliseconds(500);
        while (_server.LivenessRegistry.GetStatus("LAB01-PC08") == DeviceLivenessStatus.Online && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20);
        }
        Assert.Equal(DeviceLivenessStatus.Offline, _server.LivenessRegistry.GetStatus("LAB01-PC08"));

        // 2. Client 2 reconnects
        await using var client2 = new AgentSocketClient(LogTrace);
        await client2.ConnectAsync("127.0.0.1", _server.Port);
        await client2.SendHelloAsync("LAB01-PC08");
        await client2.SendAuthAsync("LAB01-PC08", TestAgentToken);

        // Send first authenticated heartbeat
        var ack = await client2.SendHeartbeatAsync(1, 20);
        Assert.NotNull(ack);

        // Now device has recovered to ONLINE!
        Assert.Equal(DeviceLivenessStatus.Online, _server.LivenessRegistry.GetStatus("LAB01-PC08"));

        // Verify recovery transition OFFLINE -> ONLINE was recorded
        Assert.Contains(transitions, t => t.DeviceId == "LAB01-PC08" && t.Old == DeviceLivenessStatus.Offline && t.New == DeviceLivenessStatus.Online);
    }

    // =========================================================================
    // TEST 9: Old/stale connection heartbeat must not overwrite liveness of newer ConnectionGeneration
    // =========================================================================
    [Fact]
    public async Task StaleConnectionHeartbeat_DoesNotOverwriteLivenessOfNewerGeneration()
    {
        _server = CreateAndStartServer(
            checkInterval: TimeSpan.FromMilliseconds(25),
            offlineThreshold: TimeSpan.FromMilliseconds(120));

        // Client 1 connects (Gen 1)
        await using var client1 = new AgentSocketClient(LogTrace);
        await client1.ConnectAsync("127.0.0.1", _server.Port);
        await client1.SendHelloAsync("LAB01-PC09");
        await client1.SendAuthAsync("LAB01-PC09", TestAgentToken);

        await client1.SendHeartbeatAsync(1, 10);
        Assert.Equal(1, _server.LivenessRegistry.GetLatestGeneration("LAB01-PC09"));

        // Client 2 connects from same device (Gen 2 supersedes Gen 1)
        await using var client2 = new AgentSocketClient(LogTrace);
        await client2.ConnectAsync("127.0.0.1", _server.Port);
        await client2.SendHelloAsync("LAB01-PC09");
        await client2.SendAuthAsync("LAB01-PC09", TestAgentToken);

        Assert.Equal(2, _server.LivenessRegistry.GetLatestGeneration("LAB01-PC09"));

        // Client 2 sends a heartbeat at t0
        await client2.SendHeartbeatAsync(1, 20);
        long gen2Timestamp = _server.LivenessRegistry.GetRecord("LAB01-PC09")!.LastSeenMonotonicTimestamp;

        await Task.Delay(40);

        // Stale Client 1 tries to send a heartbeat with bounded timeout.
        // Central rejects it, closes connection cleanly or returns ERROR; Client 1 throws and terminates cleanly.
        using var staleCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await Assert.ThrowsAnyAsync<Exception>(() => client1.SendHeartbeatAsync(2, 12, staleCts.Token));

        long afterStaleTimestamp = _server.LivenessRegistry.GetRecord("LAB01-PC09")!.LastSeenMonotonicTimestamp;

        // LastSeen must NOT have been updated by Client 1
        Assert.Equal(gen2Timestamp, afterStaleTimestamp);

        // Wait for Client 2's threshold to expire without heartbeats
        var deadline = DateTime.UtcNow.AddMilliseconds(500);
        while (_server.LivenessRegistry.GetStatus("LAB01-PC09") == DeviceLivenessStatus.Online && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20);
        }

        // Must be OFFLINE because Client 1's stale heartbeat did not sustain liveness
        Assert.Equal(DeviceLivenessStatus.Offline, _server.LivenessRegistry.GetStatus("LAB01-PC09"));
    }

    // =========================================================================
    // TEST 10: Stopping Central Server must stop liveness timer cleanly with no unobserved Task exceptions
    // =========================================================================
    [Fact]
    public async Task ServerShutdown_StopsLivenessMonitorCleanly()
    {
        _server = CreateAndStartServer(
            checkInterval: TimeSpan.FromMilliseconds(20),
            offlineThreshold: TimeSpan.FromMilliseconds(50));

        Assert.True(_server.LivenessMonitor.IsRunning);

        // Stop server
        await _server.StopAsync();

        Assert.False(_server.LivenessMonitor.IsRunning);
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
