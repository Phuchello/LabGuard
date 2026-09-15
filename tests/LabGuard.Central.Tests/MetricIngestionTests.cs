using System.Net;
using LabGuard.Agent;
using LabGuard.Protocol.Dtos;
using Microsoft.Data.SqlClient;
using Xunit;
using Xunit.Abstractions;

namespace LabGuard.Central.Tests;

public class MetricIngestionTests : IAsyncDisposable
{
    private const string TestAgentToken = "TEST_ONLY_AGENT_TOKEN_DO_NOT_USE_IN_PRODUCTION";
    private readonly ITestOutputHelper _testOutput;
    private readonly List<string> _traceLogs = [];
    private CentralServer? _server;
    private readonly string _dbConnectionString = "Server=localhost;Database=LabGuardDb;Trusted_Connection=True;TrustServerCertificate=True;";

    public MetricIngestionTests(ITestOutputHelper testOutput)
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

    private CentralServer CreateAndStartServer(DatabaseRepository? db = null)
    {
        var server = new CentralServer(
            expectedToken: TestAgentToken,
            ipAddress: IPAddress.Loopback,
            port: 0,
            logger: LogTrace,
            database: db ?? new DatabaseRepository(_dbConnectionString, LogTrace));

        server.Start();
        return server;
    }

    // =========================================================================
    // TEST 1: Valid METRIC_BATCH -> SQL row inserted -> BATCH_ACK
    // =========================================================================
    [Fact]
    public async Task ValidMetricBatch_PersistsSqlRow_AndReturnsBatchAck()
    {
        var db = new DatabaseRepository(_dbConnectionString, LogTrace);
        await db.EnsureDatabaseInitializedAsync();

        _server = CreateAndStartServer(db);

        await using var client = new AgentSocketClient(LogTrace);
        await client.ConnectAsync("127.0.0.1", _server.Port);
        await client.SendHelloAsync("LAB01-TEST01");
        await client.SendAuthAsync("LAB01-TEST01", TestAgentToken);

        string bootId = Guid.NewGuid().ToString("D");
        string batchId = Guid.NewGuid().ToString("D");

        var payload = new MetricBatchPayload
        {
            BootId = bootId,
            BatchId = batchId,
            SampleCount = 2,
            UptimeSeconds = 3600,
            Samples =
            [
                new MetricSampleDto { MetricName = "CPU", MetricValue = 28.5, Unit = "%", RecordedAtUtc = DateTime.UtcNow.ToString("O") },
                new MetricSampleDto { MetricName = "RAM", MetricValue = 55.0, Unit = "%", RecordedAtUtc = DateTime.UtcNow.ToString("O") }
            ]
        };

        var ack = await client.SendMetricBatchAsync(payload);

        Assert.NotNull(ack);
        Assert.True(ack.Accepted);
        Assert.Equal(batchId, ack.BatchId);

        // Verify in DB via GetDeviceSummariesAsync
        var summaries = await db.GetDeviceSummariesAsync();
        var device = summaries.Find(d => d.DeviceId == "LAB01-TEST01");
        Assert.NotNull(device);
        Assert.Equal("ONLINE", device.Status);
        Assert.NotNull(device.CpuUsage);
        Assert.Equal(28.5, Math.Round(device.CpuUsage.Value, 1));
        Assert.NotNull(device.RamUsage);
        Assert.Equal(55.0, Math.Round(device.RamUsage.Value, 1));
        Assert.Equal(3600, device.UptimeSeconds);
    }

    // =========================================================================
    // TEST 2: Same BatchId twice -> only one logical metric persisted (Deduplication)
    // =========================================================================
    [Fact]
    public async Task SameBatchIdTwice_PersistsOnlyOneLogicalMetric_ReturnsBatchAck()
    {
        var db = new DatabaseRepository(_dbConnectionString, LogTrace);
        await db.EnsureDatabaseInitializedAsync();

        _server = CreateAndStartServer(db);

        await using var client = new AgentSocketClient(LogTrace);
        await client.ConnectAsync("127.0.0.1", _server.Port);
        await client.SendHelloAsync("LAB01-TEST02");
        await client.SendAuthAsync("LAB01-TEST02", TestAgentToken);

        string bootId = Guid.NewGuid().ToString("D");
        string batchId = Guid.NewGuid().ToString("D");

        var payload = new MetricBatchPayload
        {
            BootId = bootId,
            BatchId = batchId,
            SampleCount = 2,
            UptimeSeconds = 120,
            Samples =
            [
                new MetricSampleDto { MetricName = "CPU", MetricValue = 40.0, Unit = "%", RecordedAtUtc = DateTime.UtcNow.ToString("O") },
                new MetricSampleDto { MetricName = "RAM", MetricValue = 60.0, Unit = "%", RecordedAtUtc = DateTime.UtcNow.ToString("O") }
            ]
        };

        // Send 1st time
        var ack1 = await client.SendMetricBatchAsync(payload);
        Assert.NotNull(ack1);
        Assert.True(ack1.Accepted);

        // Send 2nd time (duplicate)
        var ack2 = await client.SendMetricBatchAsync(payload);
        Assert.NotNull(ack2);
        Assert.True(ack2.Accepted);
        Assert.Equal(batchId, ack2.BatchId);

        // Count rows in MetricSamples for this BatchId
        await using var conn = new SqlConnection(_dbConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.MetricSamples WHERE BatchId = @BatchId", conn);
        cmd.Parameters.AddWithValue("@BatchId", Guid.Parse(batchId));
        int count = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        Assert.Equal(1, count);
    }

    // =========================================================================
    // TEST 3: CPU/RAM/Uptime round trip preserves values
    // =========================================================================
    [Fact]
    public async Task CpuRamUptime_RoundTrip_PreservesValues()
    {
        var db = new DatabaseRepository(_dbConnectionString, LogTrace);
        await db.EnsureDatabaseInitializedAsync();

        _server = CreateAndStartServer(db);

        await using var client = new AgentSocketClient(LogTrace);
        await client.ConnectAsync("127.0.0.1", _server.Port);
        await client.SendHelloAsync("LAB01-TEST03");
        await client.SendAuthAsync("LAB01-TEST03", TestAgentToken);

        string bootId = Guid.NewGuid().ToString("D");
        string batchId = Guid.NewGuid().ToString("D");

        double expectedCpu = 73.45;
        double expectedRam = 88.12;
        long expectedUptime = 987654;

        var payload = new MetricBatchPayload
        {
            BootId = bootId,
            BatchId = batchId,
            SampleCount = 2,
            UptimeSeconds = expectedUptime,
            Samples =
            [
                new MetricSampleDto { MetricName = "CPU", MetricValue = expectedCpu, Unit = "%", RecordedAtUtc = DateTime.UtcNow.ToString("O") },
                new MetricSampleDto { MetricName = "RAM", MetricValue = expectedRam, Unit = "%", RecordedAtUtc = DateTime.UtcNow.ToString("O") }
            ]
        };

        var ack = await client.SendMetricBatchAsync(payload);
        Assert.NotNull(ack);

        var summaries = await db.GetDeviceSummariesAsync();
        var device = summaries.Find(d => d.DeviceId == "LAB01-TEST03");
        Assert.NotNull(device);
        Assert.NotNull(device.CpuUsage);
        Assert.Equal(expectedCpu, Math.Round(device.CpuUsage.Value, 2));
        Assert.NotNull(device.RamUsage);
        Assert.Equal(expectedRam, Math.Round(device.RamUsage.Value, 2));
        Assert.Equal(expectedUptime, device.UptimeSeconds);
    }

    // =========================================================================
    // TEST 4: SQL failure -> no successful BATCH_ACK
    // =========================================================================
    [Fact]
    public async Task SqlFailure_DoesNotReturnSuccessfulBatchAck()
    {
        // Malformed connection string fails instantly without waiting for network timeout
        string invalidDbCs = "InvalidFormat;;;;";
        var faultyDb = new DatabaseRepository(invalidDbCs, LogTrace);

        _server = CreateAndStartServer(faultyDb);

        await using var client = new AgentSocketClient(LogTrace);
        await client.ConnectAsync("127.0.0.1", _server.Port);
        await client.SendHelloAsync("LAB01-TEST04");
        await client.SendAuthAsync("LAB01-TEST04", TestAgentToken);

        var payload = new MetricBatchPayload
        {
            BootId = Guid.NewGuid().ToString("D"),
            BatchId = Guid.NewGuid().ToString("D"),
            SampleCount = 1,
            UptimeSeconds = 50,
            Samples =
            [
                new MetricSampleDto { MetricName = "CPU", MetricValue = 10.0, Unit = "%", RecordedAtUtc = DateTime.UtcNow.ToString("O") }
            ]
        };

        // When SQL fails, Central suppresses BATCH_ACK per Section 9.
        // Client times out waiting for suppressed ACK, throwing TimeoutException.
        var ex = await Assert.ThrowsAsync<TimeoutException>(() => client.SendMetricBatchAsync(payload, timeout: TimeSpan.FromMilliseconds(400)));
        Assert.Contains("BATCH_ACK_TIMEOUT", ex.Message);
    }

    // =========================================================================
    // TEST 5: Metric ACK timeout releases duplex lock and allows subsequent Heartbeat
    // =========================================================================
    [Fact]
    public async Task MetricAckTimeout_ReleasesDuplexLock_AndSubsequentHeartbeatSucceeds()
    {
        string invalidDbCs = "InvalidFormat;;;;";
        var faultyDb = new DatabaseRepository(invalidDbCs, LogTrace);

        _server = CreateAndStartServer(faultyDb);

        await using var client = new AgentSocketClient(LogTrace);
        await client.ConnectAsync("127.0.0.1", _server.Port);
        await client.SendHelloAsync("LAB01-TEST05");
        await client.SendAuthAsync("LAB01-TEST05", TestAgentToken);

        var payload = new MetricBatchPayload
        {
            BootId = Guid.NewGuid().ToString("D"),
            BatchId = Guid.NewGuid().ToString("D"),
            SampleCount = 1,
            UptimeSeconds = 60,
            Samples =
            [
                new MetricSampleDto { MetricName = "CPU", MetricValue = 12.0, Unit = "%", RecordedAtUtc = DateTime.UtcNow.ToString("O") }
            ]
        };

        // 1. SendMetricBatchAsync with bounded timeout (400ms)
        // Central suppresses BATCH_ACK because DB persistence fails.
        // Agent must throw TimeoutException (BATCH_ACK_TIMEOUT) and release the duplex lock.
        var ex = await Assert.ThrowsAsync<TimeoutException>(() => client.SendMetricBatchAsync(payload, timeout: TimeSpan.FromMilliseconds(400)));
        Assert.Contains("BATCH_ACK_TIMEOUT", ex.Message);

        // 2. Since the duplex lock was properly released in finally block,
        // subsequent HEARTBEAT can acquire the lock and succeed!
        var hbAck = await client.SendHeartbeatAsync(sequenceNumber: 1, uptimeSeconds: 61);
        Assert.NotNull(hbAck);
        Assert.Equal(1, hbAck.AcknowledgedSequence);
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