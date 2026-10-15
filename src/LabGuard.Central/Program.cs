using System;
using System.Threading;
using System.Threading.Tasks;

namespace LabGuard.Central;

internal class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("==================================================");
        Console.WriteLine("LabGuard Central Server — Máy chủ trung tâm (B-001)");
        Console.WriteLine("==================================================");

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("\n[Central] Nhan tin hieu dung (Ctrl+C). Dang dung may chu...");
            cts.Cancel();
        };

        var server = new CentralServer(5000);
        await server.StartAsync(cts.Token);
    }
}