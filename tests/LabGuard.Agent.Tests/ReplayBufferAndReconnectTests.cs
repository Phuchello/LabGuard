using LabGuard.Agent;
using LabGuard.Protocol.Dtos;
using Xunit;

namespace LabGuard.Agent.Tests;

public class ReplayBufferAndReconnectTests
{
    [Fact]
    public void ReplayBuffer_MaintainsFifo_AndDropsOldestWhenExceeding50()
    {
        var droppedLogs = new List<string>();
        var buffer = new ReplayBuffer(msg => droppedLogs.Add(msg));

        // Enqueue 50 batches
        for (int i = 0; i < 50; i++)
        {
            buffer.Enqueue(new MetricBatchPayload
            {
                BatchId = $"batch-{i:D2}",
                BootId = "boot-01",
                SampleCount = 1
            });
        }

        Assert.Equal(50, buffer.Count);
        Assert.Empty(droppedLogs);

        // Enqueue 51st batch -> must drop oldest (batch-00)
        buffer.Enqueue(new MetricBatchPayload
        {
            BatchId = "batch-50",
            BootId = "boot-01",
            SampleCount = 1
        });

        Assert.Equal(50, buffer.Count);
        Assert.Single(droppedLogs);
        Assert.Contains("TELEMETRY_DROPPED", droppedLogs[0]);
        Assert.Contains("batch-00", droppedLogs[0]);

        // Oldest is now batch-01
        var pending = buffer.GetPendingReplays();
        Assert.Equal("batch-01", pending[0].BatchId);
        Assert.Equal("batch-50", pending[49].BatchId);

        // Acknowledge batch-01
        bool removed = buffer.Acknowledge("batch-01");
        Assert.True(removed);
        Assert.Equal(49, buffer.Count);
    }

    [Fact]
    public void Replay_PreservesOriginalBootIdAndBatchId_WithIsReplayTrue()
    {
        var buffer = new ReplayBuffer();
        string originalBootId = Guid.NewGuid().ToString("D");
        string originalBatchId = Guid.NewGuid().ToString("D");

        var originalBatch = new MetricBatchPayload
        {
            BootId = originalBootId,
            BatchId = originalBatchId,
            SampleCount = 2,
            UptimeSeconds = 3600,
            IsReplay = false,
            Samples =
            [
                new MetricSampleDto { MetricName = "CPU", MetricValue = 55.5 },
                new MetricSampleDto { MetricName = "RAM", MetricValue = 62.0 }
            ]
        };

        buffer.Enqueue(originalBatch);

        var replays = buffer.GetPendingReplays();
        Assert.Single(replays);

        var replayItem = replays[0];
        Assert.True(replayItem.IsReplay, "Replay packet must have IsReplay = true.");
        Assert.Equal(originalBootId, replayItem.BootId);
        Assert.Equal(originalBatchId, replayItem.BatchId);
        Assert.Equal(3600, replayItem.UptimeSeconds);
        Assert.Equal(2, replayItem.Samples.Count);
    }

    [Fact]
    public void AgentReconnect_ExponentialBackoff_WithJitter_CalculatesBoundedDelays()
    {
        // 1. Exact deterministic boundary checks
        // k = 0: base = 1.0s, range [0.9s, 1.1s]
        Assert.Equal(0.90, AgentSocketClient.CalculateBackoffDelaySeconds(0, randomFraction: 0.0), precision: 2);
        Assert.Equal(1.00, AgentSocketClient.CalculateBackoffDelaySeconds(0, randomFraction: 0.5), precision: 2);
        Assert.Equal(1.10, AgentSocketClient.CalculateBackoffDelaySeconds(0, randomFraction: 1.0), precision: 2);

        // k = 1: base = 2.0s, range [1.8s, 2.2s]
        Assert.Equal(1.80, AgentSocketClient.CalculateBackoffDelaySeconds(1, randomFraction: 0.0), precision: 2);
        Assert.Equal(2.00, AgentSocketClient.CalculateBackoffDelaySeconds(1, randomFraction: 0.5), precision: 2);
        Assert.Equal(2.20, AgentSocketClient.CalculateBackoffDelaySeconds(1, randomFraction: 1.0), precision: 2);

        // k = 2: base = 4.0s, range [3.6s, 4.4s]
        Assert.Equal(3.60, AgentSocketClient.CalculateBackoffDelaySeconds(2, randomFraction: 0.0), precision: 2);
        Assert.Equal(4.40, AgentSocketClient.CalculateBackoffDelaySeconds(2, randomFraction: 1.0), precision: 2);

        // k = 3: base = 8.0s, range [7.2s, 8.8s]
        Assert.Equal(7.20, AgentSocketClient.CalculateBackoffDelaySeconds(3, randomFraction: 0.0), precision: 2);
        Assert.Equal(8.80, AgentSocketClient.CalculateBackoffDelaySeconds(3, randomFraction: 1.0), precision: 2);

        // k = 4: base = 16.0s, range [14.4s, 17.6s]
        Assert.Equal(14.40, AgentSocketClient.CalculateBackoffDelaySeconds(4, randomFraction: 0.0), precision: 2);
        Assert.Equal(17.60, AgentSocketClient.CalculateBackoffDelaySeconds(4, randomFraction: 1.0), precision: 2);

        // k = 5: base = 30.0s (capped), range [27.0s, 33.0s]
        Assert.Equal(27.00, AgentSocketClient.CalculateBackoffDelaySeconds(5, randomFraction: 0.0), precision: 2);
        Assert.Equal(33.00, AgentSocketClient.CalculateBackoffDelaySeconds(5, randomFraction: 1.0), precision: 2);

        // k = 10: still capped at 30.0s, range [27.0s, 33.0s]
        Assert.Equal(27.00, AgentSocketClient.CalculateBackoffDelaySeconds(10, randomFraction: 0.0), precision: 2);
        Assert.Equal(33.00, AgentSocketClient.CalculateBackoffDelaySeconds(10, randomFraction: 1.0), precision: 2);

        // 2. Randomized runs always strictly within +-10%
        for (int k = 0; k <= 10; k++)
        {
            double baseSec = Math.Min(30.0, Math.Pow(2, k));
            double minAllowed = baseSec * 0.90;
            double maxAllowed = baseSec * 1.10;

            for (int r = 0; r < 20; r++)
            {
                double delay = AgentSocketClient.CalculateBackoffDelaySeconds(k);
                Assert.InRange(delay, minAllowed - 0.001, maxAllowed + 0.001);
            }
        }
    }
}
