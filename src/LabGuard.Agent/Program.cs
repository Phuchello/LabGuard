namespace LabGuard.Agent;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        string host = Environment.GetEnvironmentVariable("LABGUARD_SERVER_HOST") ?? "127.0.0.1";
        int port = 5000;
        if (int.TryParse(Environment.GetEnvironmentVariable("LABGUARD_SERVER_PORT"), out int envPort))
        {
            port = envPort;
        }

        string deviceId = Environment.GetEnvironmentVariable("LABGUARD_DEVICE_ID") ?? "LAB01-PC01";
        string token = Environment.GetEnvironmentVariable("LABGUARD_AGENT_TOKEN") ?? "development-only-token";

        // Allow passing deviceId as first arg: dotnet run --project src/LabGuard.Agent LAB01-PC01
        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
        {
            deviceId = args[0];
        }

        Console.WriteLine("===================================");
        Console.WriteLine(" LABGUARD AGENT");
        Console.WriteLine($" Device: {deviceId}");
        Console.WriteLine("===================================");
        Console.WriteLine();

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("\nStopping Agent...");
            cts.Cancel();
        };

        void Log(string message)
        {
            // Do not print routine heartbeats to console to keep output clean and readable
            if (!message.Contains("HEARTBEAT") && !message.Contains("Heartbeat"))
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
            }
        }

        var replayBuffer = new ReplayBuffer(Log);
        int attempt = 0;

        while (!cts.IsCancellationRequested)
        {
            AgentSocketClient? client = null;
            try
            {
                client = new AgentSocketClient(Log, replayBuffer);
                Console.WriteLine($"Connecting to {host}:{port}...");
                await client.ConnectAsync(host, port, cts.Token);
                Console.WriteLine("Connected.");

                var helloAck = await client.SendHelloAsync(deviceId, ct: cts.Token);
                if (helloAck == null)
                {
                    throw new InvalidOperationException("HELLO failed: rejected by server.");
                }

                var auth = await client.SendAuthAsync(deviceId, token, cts.Token);
                if (!auth.Success)
                {
                    throw new InvalidOperationException($"Authentication failed: {auth.ErrorMessage}");
                }

                Console.WriteLine("Authenticated.");
                attempt = 0; // Reset backoff counter ONLY after AUTH_RESULT(success=true)

                // Flush unacknowledged batches from ReplayBuffer
                int replayed = await client.ReplayPendingBatchesAsync(cts.Token);
                if (replayed > 0)
                {
                    Console.WriteLine($"Replayed {replayed} unacknowledged telemetry batches.");
                }

                // Start background services
                client.StartHeartbeat(TimeSpan.FromSeconds(5.0));
                client.StartMetrics(TimeSpan.FromSeconds(5.0));
                client.StartServiceMonitoring();
                Console.WriteLine("Heartbeat active every 5s.");
                Console.WriteLine("Telemetry metrics active every 5s.");
                Console.WriteLine("Service monitor active every 5s.");
                Console.WriteLine("Press Ctrl+C to stop.");

                // Await disconnection or user termination
                await client.WaitForDisconnectAsync(cts.Token);
                Console.WriteLine("Connection lost.");
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection lost: {ex.Message}");
            }
            finally
            {
                if (client != null)
                {
                    try
                    {
                        await client.DisconnectAsync();
                    }
                    catch
                    {
                        // Best effort cleanup
                    }
                }
            }

            if (cts.IsCancellationRequested)
            {
                break;
            }

            double delaySec = AgentSocketClient.CalculateBackoffDelaySeconds(attempt);
            Console.WriteLine($"Reconnect in {delaySec:F1}s...");
            attempt++;

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(delaySec), cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        Console.WriteLine("Agent stopped.");
        return 0;
    }
}
