using System.Net;
using LabGuard.Agent;
using LabGuard.Central;
using LabGuard.Protocol.Dtos;
using Xunit;
using Xunit.Abstractions;

namespace LabGuard.Central.Tests;

public class IncidentDetectionTests : IAsyncDisposable
{
    private readonly ITestOutputHelper _testOutput;
    private readonly List<string> _traceLogs = [];
    private readonly string _dbConnectionString = "Server=localhost;Database=LabGuardDb;Trusted_Connection=True;TrustServerCertificate=True;";

    public IncidentDetectionTests(ITestOutputHelper testOutput)
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

    [Fact]
    public void HighCpu_ThreeConsecutiveSamplesOver90_TriggersActive_AndThreeUnder75_Recovers()
    {
        var engine = new RuleEngine(LogTrace, cooldown: TimeSpan.Zero);
        string deviceId = "TEST-PC-CPU";

        int triggerCount = 0;
        int recoverCount = 0;
        engine.OnIncidentTriggered += evt => { triggerCount++; return Task.CompletedTask; };
        engine.OnIncidentRecovered += evt => { recoverCount++; return Task.CompletedTask; };

        // 1. Two samples >= 90% -> not active
        engine.ProcessMetricBatch(deviceId, 91.0, 50.0);
        engine.ProcessMetricBatch(deviceId, 92.5, 50.0);
        Assert.False(engine.IsIncidentActive(deviceId, "HIGH_CPU"));
        Assert.Equal(0, triggerCount);

        // 2. 3rd sample >= 90% -> triggers ACTIVE
        engine.ProcessMetricBatch(deviceId, 90.0, 50.0);
        Assert.True(engine.IsIncidentActive(deviceId, "HIGH_CPU"));
        Assert.Equal(1, triggerCount);

        // 3. Two samples <= 75% -> still active
        engine.ProcessMetricBatch(deviceId, 74.0, 50.0);
        engine.ProcessMetricBatch(deviceId, 71.0, 50.0);
        Assert.True(engine.IsIncidentActive(deviceId, "HIGH_CPU"));
        Assert.Equal(0, recoverCount);

        // 4. 3rd sample <= 75% -> triggers RECOVERED
        engine.ProcessMetricBatch(deviceId, 75.0, 50.0);
        Assert.False(engine.IsIncidentActive(deviceId, "HIGH_CPU"));
        Assert.Equal(1, recoverCount);
    }

    [Fact]
    public void HighMemory_ThreeConsecutiveSamplesOver85_TriggersActive_AndThreeUnder70_Recovers()
    {
        var engine = new RuleEngine(LogTrace, cooldown: TimeSpan.Zero);
        string deviceId = "TEST-PC-MEM";

        int triggerCount = 0;
        int recoverCount = 0;
        engine.OnIncidentTriggered += evt => { triggerCount++; return Task.CompletedTask; };
        engine.OnIncidentRecovered += evt => { recoverCount++; return Task.CompletedTask; };

        // 1. Two samples >= 85% -> not active
        engine.ProcessMetricBatch(deviceId, 40.0, 86.0);
        engine.ProcessMetricBatch(deviceId, 40.0, 88.0);
        Assert.False(engine.IsIncidentActive(deviceId, "HIGH_MEMORY"));
        Assert.Equal(0, triggerCount);

        // 2. 3rd sample >= 85% -> triggers ACTIVE
        engine.ProcessMetricBatch(deviceId, 40.0, 85.0);
        Assert.True(engine.IsIncidentActive(deviceId, "HIGH_MEMORY"));
        Assert.Equal(1, triggerCount);

        // 3. Two samples <= 70% -> still active
        engine.ProcessMetricBatch(deviceId, 40.0, 69.5);
        engine.ProcessMetricBatch(deviceId, 40.0, 68.0);
        Assert.True(engine.IsIncidentActive(deviceId, "HIGH_MEMORY"));
        Assert.Equal(0, recoverCount);

        // 4. 3rd sample <= 70% -> triggers RECOVERED
        engine.ProcessMetricBatch(deviceId, 40.0, 70.0);
        Assert.False(engine.IsIncidentActive(deviceId, "HIGH_MEMORY"));
        Assert.Equal(1, recoverCount);
    }

    [Fact]
    public void ServiceDown_TwoConsecutiveFailedProbes_TriggersActive_AndTwoSuccesses_Recovers()
    {
        var engine = new RuleEngine(LogTrace, cooldown: TimeSpan.Zero);
        string deviceId = "TEST-PC-SVC";

        int triggerCount = 0;
        int recoverCount = 0;
        engine.OnIncidentTriggered += evt => { triggerCount++; return Task.CompletedTask; };
        engine.OnIncidentRecovered += evt => { recoverCount++; return Task.CompletedTask; };

        var probeFail = new List<ServiceStatusItemDto>
        {
            new() { ServiceName = "HTTP", Port = 80, IsListening = false }
        };
        var probePass = new List<ServiceStatusItemDto>
        {
            new() { ServiceName = "HTTP", Port = 80, IsListening = true }
        };

        // 1. One failed probe -> not active
        engine.ProcessServiceStatus(deviceId, probeFail);
        Assert.False(engine.IsIncidentActive(deviceId, "SERVICE_DOWN"));
        Assert.Equal(0, triggerCount);

        // 2. Second failed probe -> triggers ACTIVE
        engine.ProcessServiceStatus(deviceId, probeFail);
        Assert.True(engine.IsIncidentActive(deviceId, "SERVICE_DOWN"));
        Assert.Equal(1, triggerCount);

        // 3. One successful probe -> still active
        engine.ProcessServiceStatus(deviceId, probePass);
        Assert.True(engine.IsIncidentActive(deviceId, "SERVICE_DOWN"));
        Assert.Equal(0, recoverCount);

        // 4. Second successful probe -> triggers RECOVERED
        engine.ProcessServiceStatus(deviceId, probePass);
        Assert.False(engine.IsIncidentActive(deviceId, "SERVICE_DOWN"));
        Assert.Equal(1, recoverCount);
    }

    [Fact]
    public async Task NoDuplicateActiveIncident_CreatedForSameDeviceAndRuleType()
    {
        var db = new DatabaseRepository(_dbConnectionString, LogTrace);
        await db.EnsureDatabaseInitializedAsync();

        string deviceId = "INC-DEDUP-01";
        string ruleType = "HIGH_CPU";

        // Clean up any old active incident for test repeatability
        await db.RecoverIncidentAsync(deviceId, ruleType);

        // 1. First trigger: creates new ACTIVE incident
        var inc1 = await db.OpenOrUpdateIncidentAsync(deviceId, ruleType, "HIGH", 92.5);
        Assert.NotNull(inc1);
        Assert.Equal("ACTIVE", inc1.ConditionState);
        Assert.Equal(1, inc1.OccurrenceCount);

        // 2. Second trigger while still ACTIVE: increments OccurrenceCount on existing row
        var inc2 = await db.OpenOrUpdateIncidentAsync(deviceId, ruleType, "HIGH", 96.0);
        Assert.NotNull(inc2);
        Assert.Equal(inc1.IncidentId, inc2.IncidentId); // Must be same incident ID
        Assert.Equal("ACTIVE", inc2.ConditionState);
        Assert.Equal(2, inc2.OccurrenceCount);

        // 3. Recover the incident
        var rec = await db.RecoverIncidentAsync(deviceId, ruleType);
        Assert.NotNull(rec);
        Assert.Equal(inc1.IncidentId, rec.IncidentId);
        Assert.Equal("RECOVERED", rec.ConditionState);
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
