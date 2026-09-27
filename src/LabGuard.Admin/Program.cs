using LabGuard.Admin;
namespace LabGuard.Admin;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new global::LabGuard.Admin.DashboardForm());
    }
}