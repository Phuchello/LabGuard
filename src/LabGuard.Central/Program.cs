using System.Net;

namespace LabGuard.Central;

public class Program
{
    public static async Task Main(string[] args)
    {
        int port = 5000;
        if (args.Length > 0 && int.TryParse(args[0], out int parsedPort))
        {
            port = parsedPort;
        }
        else if (int.TryParse(Environment.GetEnvironmentVariable("LABGUARD_PORT"), out int envPort))
        {
            port = envPort;
        }

        string agentToken = Environment.GetEnvironmentVariable("LABGUARD_AGENT_TOKEN") ?? "development-only-token";

        Console.WriteLine("===================================");
        Console.WriteLine(" LABGUARD CENTRAL SERVER");
        Console.WriteLine("===================================");
        Console.WriteLine($"Listening: 0.0.0.0:{port}");
        Console.WriteLine();

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("\nStopping Central Server...");
            cts.Cancel();
        };

        void Log(string message)
        {
            // Do not dump routine heartbeat frames every 5s
            if (message.Contains("HeartbeatAck") || message.Contains("HEARTBEAT_ACK"))
            {
                return;
            }

            // Display useful operational events with timestamps
            if (message.Contains("Accepted connection") ||
                message.Contains("AUTH_AGENT successful") ||
                message.Contains("Admin login successful") ||
                message.Contains("Successfully persisted batch") ||
                message.Contains("Connection closed") ||
                message.Contains("error") ||
                message.Contains("rejected") ||
                message.Contains("violation"))
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
            }
        }

        var db = new DatabaseRepository(logger: Log);
        Console.WriteLine("Initializing database connection and schema...");
        bool dbReady = await db.EnsureDatabaseInitializedAsync().ConfigureAwait(false);
        if (dbReady)
        {
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(db.ConnectionString);
            Console.WriteLine($"Database ready: {builder.InitialCatalog}");
        }
        else
        {
            Console.WriteLine("Database unavailable.");
            Console.WriteLine("Check SQL Server and LABGUARD_DB_CONNECTION.");
        }

        await using var server = new CentralServer(
            expectedToken: agentToken,
            ipAddress: IPAddress.Any,
            port: port,
            logger: Log,
            database: db);

        server.LivenessRegistry.OnStatusChanged += (deviceId, oldStatus, newStatus) =>
        {
            if (newStatus == DeviceLivenessStatus.Online)
            {
                Console.WriteLine($"[ONLINE ] {deviceId}");
            }
            else if (newStatus == DeviceLivenessStatus.Offline)
            {
                Console.WriteLine($"[OFFLINE] {deviceId}");
            }
        };

        server.Start();

        try
        {
            await Task.Delay(Timeout.Infinite, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Clean exit requested by Ctrl+C
        }

        await server.StopAsync();
        Console.WriteLine("Central Server stopped cleanly.");
    }
}
