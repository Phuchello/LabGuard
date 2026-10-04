using System;
using System.Threading;
using System.Threading.Tasks;

namespace LabGuard.Central
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== LABGUARD CENTRAL SERVER ===");

            var db = new DatabaseManager();
            await db.TestConnectionAsync();

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            var server = new CentralServer();
            await server.StartAsync(cts.Token);
        }
    }
}