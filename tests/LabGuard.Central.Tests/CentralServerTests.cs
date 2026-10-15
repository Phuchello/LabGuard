using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using LabGuard.Protocol;
using LabGuard.Protocol.Dtos;
using LabGuard.Protocol.Serialization;

namespace LabGuard.Central.Tests;

/// <summary>
/// Bộ kiểm thử tự động toàn diện cho CentralServer (LG-B-001).
/// Kiểm tra: Mở TCP listener cổng 5000 / dynamic port, đa kết nối đồng thời,
/// đọc gói tin bằng FrameCodec.ReadEnvelopeAsync, hiển thị DeviceId và MessageType,
/// xử lý ngắt kết nối và dữ liệu sai an toàn, dừng sạch sẽ khi có tín hiệu cancellation.
/// </summary>
public class CentralServerTests
{
    private static async Task<(CentralServer Server, Task ServerTask, CancellationTokenSource Cts)> StartTestServerAsync()
    {
        var server = new CentralServer(0, IPAddress.Loopback);
        var cts = new CancellationTokenSource();
        var serverTask = Task.Run(() => server.StartAsync(cts.Token));
        await server.WaitForStartAsync();
        return (server, serverTask, cts);
    }

    private static async Task SendRawFrameAsync(TcpClient client, byte[] frameBytes)
    {
        var stream = client.GetStream();
        await stream.WriteAsync(frameBytes);
        await stream.FlushAsync();
    }

    private static async Task SendEnvelopeAsync<T>(TcpClient client, WireEnvelope<T> envelope)
    {
        byte[] jsonBytes = ProtocolSerializer.Serialize(envelope);
        byte[] frameBytes = FrameCodec.Encode(jsonBytes);
        await SendRawFrameAsync(client, frameBytes);
    }

    [Fact]
    public async Task CentralServer_StartsAndStops_Cleanly()
    {
        var (server, serverTask, cts) = await StartTestServerAsync();

        try
        {
            Assert.True(server.IsRunning);
            Assert.True(server.Port > 0);
        }
        finally
        {
            cts.Cancel();
            await serverTask;
            Assert.False(server.IsRunning);
            await server.DisposeAsync();
        }
    }

    [Fact]
    public async Task CentralServer_ReceivesValidHeartbeat_Successfully()
    {
        var (server, serverTask, cts) = await StartTestServerAsync();
        var receivedTcs = new TaskCompletionSource<WireEnvelope<JsonElement>>(TaskCreationOptions.RunContinuationsAsynchronously);

        server.EnvelopeReceived += (env, ep) =>
        {
            if (env.MessageType == MessageTypes.Heartbeat)
            {
                receivedTcs.TrySetResult(env);
            }
        };

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, server.Port);

            var heartbeatPayload = new HeartbeatPayload
            {
                SequenceNumber = 42,
                UptimeSeconds = 3600
            };
            var envelope = WireEnvelope<HeartbeatPayload>.Create(
                MessageTypes.Heartbeat,
                "PC-AGENT-01",
                heartbeatPayload
            );

            await SendEnvelopeAsync(client, envelope);

            var receivedEnv = await receivedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.NotNull(receivedEnv);
            Assert.Equal(MessageTypes.Heartbeat, receivedEnv.MessageType);
            Assert.Equal("PC-AGENT-01", receivedEnv.DeviceId);
            Assert.Equal(envelope.RequestId, receivedEnv.RequestId);

            // Deserialize typed payload from JsonElement
            var payload = ProtocolSerializer.ExtractPayload<HeartbeatPayload>(receivedEnv.Payload);
            Assert.NotNull(payload);
            Assert.Equal(42, payload.SequenceNumber);
            Assert.Equal(3600, payload.UptimeSeconds);
        }
        finally
        {
            cts.Cancel();
            await serverTask;
            await server.DisposeAsync();
        }
    }

    [Fact]
    public async Task CentralServer_ReceivesValidMetricBatch_Successfully()
    {
        var (server, serverTask, cts) = await StartTestServerAsync();
        var receivedTcs = new TaskCompletionSource<WireEnvelope<JsonElement>>(TaskCreationOptions.RunContinuationsAsynchronously);

        server.EnvelopeReceived += (env, ep) =>
        {
            if (env.MessageType == MessageTypes.MetricBatch)
            {
                receivedTcs.TrySetResult(env);
            }
        };

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, server.Port);

            var batchPayload = new MetricBatchPayload
            {
                BootId = Guid.NewGuid().ToString("D"),
                BatchId = Guid.NewGuid().ToString("D"),
                UptimeSeconds = 1200,
                SampleCount = 2,
                Samples =
                [
                    new MetricSampleDto { MetricName = "CPU", MetricValue = 35.5, Unit = "%" },
                    new MetricSampleDto { MetricName = "RAM", MetricValue = 62.0, Unit = "%" }
                ]
            };
            var envelope = WireEnvelope<MetricBatchPayload>.Create(
                MessageTypes.MetricBatch,
                "PC-LAB-10",
                batchPayload
            );

            await SendEnvelopeAsync(client, envelope);

            var receivedEnv = await receivedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.NotNull(receivedEnv);
            Assert.Equal(MessageTypes.MetricBatch, receivedEnv.MessageType);
            Assert.Equal("PC-LAB-10", receivedEnv.DeviceId);

            var payload = ProtocolSerializer.ExtractPayload<MetricBatchPayload>(receivedEnv.Payload);
            Assert.NotNull(payload);
            Assert.Equal(batchPayload.BootId, payload.BootId);
            Assert.Equal(batchPayload.BatchId, payload.BatchId);
            Assert.Equal(2, payload.SampleCount);
            Assert.Equal(2, payload.Samples.Count);
        }
        finally
        {
            cts.Cancel();
            await serverTask;
            await server.DisposeAsync();
        }
    }

    [Fact]
    public async Task CentralServer_ReceivesValidHelloAgent_Successfully()
    {
        var (server, serverTask, cts) = await StartTestServerAsync();
        var receivedTcs = new TaskCompletionSource<WireEnvelope<JsonElement>>(TaskCreationOptions.RunContinuationsAsynchronously);

        server.EnvelopeReceived += (env, ep) =>
        {
            if (env.MessageType == MessageTypes.HelloAgent)
            {
                receivedTcs.TrySetResult(env);
            }
        };

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, server.Port);

            var helloPayload = new HelloAgentPayload
            {
                AgentVersion = "1.0.0",
                OsVersion = "Windows 11",
                Hostname = "LAB-PC-01",
                MacAddress = "00:1A:2B:3C:4D:5E"
            };
            var envelope = WireEnvelope<HelloAgentPayload>.Create(
                MessageTypes.HelloAgent,
                "PC-HELLO-01",
                helloPayload
            );

            await SendEnvelopeAsync(client, envelope);

            var receivedEnv = await receivedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.NotNull(receivedEnv);
            Assert.Equal(MessageTypes.HelloAgent, receivedEnv.MessageType);
            Assert.Equal("PC-HELLO-01", receivedEnv.DeviceId);
        }
        finally
        {
            cts.Cancel();
            await serverTask;
            await server.DisposeAsync();
        }
    }

    [Fact]
    public async Task CentralServer_MultipleConcurrentClients_HandledCorrectly()
    {
        var (server, serverTask, cts) = await StartTestServerAsync();
        const int clientCount = 5;
        const int messagesPerClient = 2;
        var receivedEnvelopes = new ConcurrentBag<WireEnvelope<JsonElement>>();
        var allReceivedCountdown = new CountdownEvent(clientCount * messagesPerClient);

        server.EnvelopeReceived += (env, ep) =>
        {
            receivedEnvelopes.Add(env);
            if (!allReceivedCountdown.IsSet)
            {
                allReceivedCountdown.Signal();
            }
        };

        try
        {
            var clientTasks = Enumerable.Range(1, clientCount).Select(async i =>
            {
                using var client = new TcpClient();
                await client.ConnectAsync(IPAddress.Loopback, server.Port);

                for (int m = 1; m <= messagesPerClient; m++)
                {
                    var envelope = WireEnvelope<HeartbeatPayload>.Create(
                        MessageTypes.Heartbeat,
                        $"PC-CLIENT-{i:D2}",
                        new HeartbeatPayload { SequenceNumber = m, UptimeSeconds = m * 10 }
                    );
                    await SendEnvelopeAsync(client, envelope);
                    await Task.Delay(20);
                }
            });

            await Task.WhenAll(clientTasks);

            bool completed = allReceivedCountdown.Wait(TimeSpan.FromSeconds(10));
            Assert.True(completed, $"Expected {clientCount * messagesPerClient} envelopes, received {receivedEnvelopes.Count}");
            Assert.Equal(clientCount * messagesPerClient, receivedEnvelopes.Count);
        }
        finally
        {
            cts.Cancel();
            await serverTask;
            await server.DisposeAsync();
        }
    }

    [Fact]
    public async Task CentralServer_ClientGracefulEof_DoesNotThrow()
    {
        var (server, serverTask, cts) = await StartTestServerAsync();
        var disconnectTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        server.ClientDisconnected += ep => disconnectTcs.TrySetResult(true);

        try
        {
            using (var client = new TcpClient())
            {
                await client.ConnectAsync(IPAddress.Loopback, server.Port);

                // Send 1 valid message
                var envelope = WireEnvelope<HeartbeatPayload>.Create(
                    MessageTypes.Heartbeat,
                    "PC-DISCONNECT-TEST",
                    new HeartbeatPayload { SequenceNumber = 1, UptimeSeconds = 5 }
                );
                await SendEnvelopeAsync(client, envelope);

                // Cleanly shutdown sending side to simulate EOF at frame boundary
                client.Client.Shutdown(SocketShutdown.Both);
            }

            var disconnected = await disconnectTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(disconnected);
            Assert.True(server.IsRunning);
        }
        finally
        {
            cts.Cancel();
            await serverTask;
            await server.DisposeAsync();
        }
    }

    [Fact]
    public async Task CentralServer_MalformedJsonPayload_RejectsWithoutCrashing()
    {
        var (server, serverTask, cts) = await StartTestServerAsync();
        var rejectedTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var subsequentSuccessTcs = new TaskCompletionSource<WireEnvelope<JsonElement>>(TaskCreationOptions.RunContinuationsAsynchronously);

        server.MalformedMessageRejected += (ep, reason) => rejectedTcs.TrySetResult(reason);
        server.EnvelopeReceived += (env, ep) =>
        {
            if (env.DeviceId == "PC-RECOVERY-CLIENT")
            {
                subsequentSuccessTcs.TrySetResult(env);
            }
        };

        try
        {
            // Client 1 sends malformed JSON
            using (var badClient = new TcpClient())
            {
                await badClient.ConnectAsync(IPAddress.Loopback, server.Port);

                byte[] invalidJsonBytes = Encoding.UTF8.GetBytes("{ not valid json: 123 }");
                byte[] badFrame = FrameCodec.Encode(invalidJsonBytes);
                await SendRawFrameAsync(badClient, badFrame);
            }

            var reason = await rejectedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.NotNull(reason);
            Assert.True(server.IsRunning, "Server should remain running after receiving malformed data");

            // Client 2 connects immediately and sends valid envelope
            using (var goodClient = new TcpClient())
            {
                await goodClient.ConnectAsync(IPAddress.Loopback, server.Port);

                var envelope = WireEnvelope<HeartbeatPayload>.Create(
                    MessageTypes.Heartbeat,
                    "PC-RECOVERY-CLIENT",
                    new HeartbeatPayload { SequenceNumber = 1, UptimeSeconds = 10 }
                );
                await SendEnvelopeAsync(goodClient, envelope);

                var received = await subsequentSuccessTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
                Assert.NotNull(received);
                Assert.Equal("PC-RECOVERY-CLIENT", received.DeviceId);
            }
        }
        finally
        {
            cts.Cancel();
            await serverTask;
            await server.DisposeAsync();
        }
    }

    [Fact]
    public async Task CentralServer_EnvelopeContractViolation_RejectsWithoutCrashing()
    {
        var (server, serverTask, cts) = await StartTestServerAsync();
        var rejectedTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        server.MalformedMessageRejected += (ep, reason) => rejectedTcs.TrySetResult(reason);

        try
        {
            using var badClient = new TcpClient();
            await badClient.ConnectAsync(IPAddress.Loopback, server.Port);

            // Valid JSON, but violates frozen contract: missing 'deviceId' and invalid casing
            byte[] contractViolationBytes = Encoding.UTF8.GetBytes("""
            {
                "version": 1,
                "messageType": "HEARTBEAT",
                "requestId": "550e8400-e29b-41d4-a716-446655440000",
                "sentAtUtc": "2026-10-15T12:00:00Z",
                "payload": {}
            }
            """);
            byte[] frame = FrameCodec.Encode(contractViolationBytes);
            await SendRawFrameAsync(badClient, frame);

            var reason = await rejectedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.NotNull(reason);
            Assert.True(server.IsRunning);
        }
        finally
        {
            cts.Cancel();
            await serverTask;
            await server.DisposeAsync();
        }
    }

    [Fact]
    public async Task CentralServer_CoalescedFrames_DecodedCorrectly()
    {
        var (server, serverTask, cts) = await StartTestServerAsync();
        var receivedEnvelopes = new List<WireEnvelope<JsonElement>>();
        var twoFramesTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        server.EnvelopeReceived += (env, ep) =>
        {
            receivedEnvelopes.Add(env);
            if (receivedEnvelopes.Count == 2)
            {
                twoFramesTcs.TrySetResult(true);
            }
        };

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, server.Port);

            var env1 = WireEnvelope<HeartbeatPayload>.Create(
                MessageTypes.Heartbeat, "PC-COALESCE-01", new HeartbeatPayload { SequenceNumber = 1, UptimeSeconds = 10 });
            var env2 = WireEnvelope<HeartbeatPayload>.Create(
                MessageTypes.Heartbeat, "PC-COALESCE-01", new HeartbeatPayload { SequenceNumber = 2, UptimeSeconds = 15 });

            byte[] frame1 = FrameCodec.Encode(ProtocolSerializer.Serialize(env1));
            byte[] frame2 = FrameCodec.Encode(ProtocolSerializer.Serialize(env2));

            // Concatenate both frames into a single buffer and write in 1 TCP write call
            byte[] combined = new byte[frame1.Length + frame2.Length];
            Buffer.BlockCopy(frame1, 0, combined, 0, frame1.Length);
            Buffer.BlockCopy(frame2, 0, combined, frame1.Length, frame2.Length);

            await SendRawFrameAsync(client, combined);

            var success = await twoFramesTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(success);
            Assert.Equal(2, receivedEnvelopes.Count);
            Assert.Equal("PC-COALESCE-01", receivedEnvelopes[0].DeviceId);
            Assert.Equal("PC-COALESCE-01", receivedEnvelopes[1].DeviceId);
        }
        finally
        {
            cts.Cancel();
            await serverTask;
            await server.DisposeAsync();
        }
    }
}
