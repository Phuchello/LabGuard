namespace LabGuard.Agent;

internal class Program
{
    static async Task<int> Main(string[] args)
    {
        using var shutdown = new CancellationTokenSource();
        ConsoleCancelEventHandler handler = (_, e) => { e.Cancel = true; shutdown.Cancel(); };
        Console.CancelKeyPress += handler;
        try
        {
            return await AgentConsoleApplication.RunAsync(args, Console.Out, Console.Error, shutdown.Token);
        }
        finally
        {
            Console.CancelKeyPress -= handler;
        }
    }
}
