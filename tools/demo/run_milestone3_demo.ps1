Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$captureCode = @"
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class WindowCapturer {
    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public static void Capture(IntPtr hWnd, string filePath) {
        ShowWindow(hWnd, 9); // SW_RESTORE
        SetForegroundWindow(hWnd);
        System.Threading.Thread.Sleep(800);

        RECT rect;
        GetWindowRect(hWnd, out rect);
        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;

        if (width <= 100 || height <= 100) {
            Rectangle bounds = Screen.PrimaryScreen.Bounds;
            using (Bitmap bmp = new Bitmap(bounds.Width, bounds.Height)) {
                using (Graphics g = Graphics.FromImage(bmp)) {
                    g.CopyFromScreen(0, 0, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
                }
                bmp.Save(filePath, ImageFormat.Png);
            }
            return;
        }

        using (Bitmap bmp = new Bitmap(width, height)) {
            using (Graphics g = Graphics.FromImage(bmp)) {
                g.CopyFromScreen(rect.Left, rect.Top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
            }
            bmp.Save(filePath, ImageFormat.Png);
        }
    }
}
"@

Add-Type -TypeDefinition $captureCode -ReferencedAssemblies System.Drawing, System.Windows.Forms

Write-Host "===================================================="
Write-Host " LABGUARD PRODUCT MILESTONE 3 AUTOMATED DEMO RUNNER"
Write-Host "===================================================="

# Clean up any leftover processes
Get-Process -Name "LabGuard.Central", "LabGuard.Agent", "LabGuard.Admin" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

# Ensure docs/screenshots folder exists
if (-not (Test-Path "docs/screenshots")) {
    New-Item -ItemType Directory -Path "docs/screenshots" -Force | Out-Null
}

$connStr = "Server=localhost;Database=LabGuardDb;Trusted_Connection=True;TrustServerCertificate=True;"

# 1. Start a local TCP listener on port 8080 (for service monitoring probe)
Write-Host "`n[1/6] Starting test TCP listener on port 8080..."
$serviceListener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 8080)
$serviceListener.Start()
Write-Host "TCP listener on port 8080 is RUNNING."

# 2. Start Central Server
Write-Host "`n[2/6] Starting Central Server on port 5000..."
$centralProc = Start-Process -FilePath "src/LabGuard.Central/bin/Debug/net9.0/LabGuard.Central.exe" -PassThru
Start-Sleep -Seconds 3

# 3. Start Admin Dashboard WinForms
Write-Host "`n[3/6] Starting Admin Dashboard WinForms..."
$adminProc = Start-Process -FilePath "src/LabGuard.Admin/bin/Debug/net9.0-windows/LabGuard.Admin.exe" -PassThru
Start-Sleep -Seconds 3

# 4. Start Agent (LAB01-PC01)
Write-Host "`n[4/6] Starting Agent (LAB01-PC01)..."
$agentProc = Start-Process -FilePath "src/LabGuard.Agent/bin/Debug/net9.0/LabGuard.Agent.exe" -ArgumentList "LAB01-PC01" -PassThru
Write-Host "Waiting 12 seconds for initial connection, metrics, and service checks..."
Start-Sleep -Seconds 12

# 5. Execute Scenario 1: High CPU Incident Trigger & Recovery
Write-Host "`n[5/6] Executing SCENARIO 1 (CPU): Simulating High CPU..."
Write-Host "Starting multi-core CPU load for 18 seconds..."
$cpuCts = New-Object System.Threading.CancellationTokenSource
$cpuTask = [System.Threading.Tasks.Task]::Run([Action]{
    [System.Threading.Tasks.Parallel]::For(0, [Environment]::ProcessorCount, [Action[int]]{
        param($i)
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        while (-not $cpuCts.IsCancellationRequested -and $sw.ElapsedMilliseconds -lt 18000) {
            # Burn CPU
            $x = 1.0
            for ($j = 0; $j -lt 10000; $j++) { $x = $x * 1.0001 }
        }
    })
})

# Wait for 3 metric batches under load (each is 5s)
Start-Sleep -Seconds 18
$cpuCts.Cancel()
Write-Host "CPU load stopped. Waiting 10 seconds for CPU to cool down and recover..."
Start-Sleep -Seconds 10

# 6. Check Incidents in SQL Server
Write-Host "`n[6/6] Checking SQL Server dbo.Incidents..."
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT IncidentId, DeviceId, RuleType, Severity, ConditionState, WorkflowState, OccurrenceCount, OpenedAtUtc, RecoveredAtUtc FROM dbo.Incidents ORDER BY IncidentId DESC"
$reader = $cmd.ExecuteReader()
$incidentsFound = 0
while ($reader.Read()) {
    $incidentsFound++
    Write-Host ("  [SQL Incident #{0}] Device={1} Rule={2} Sev={3} Cond={4} WF={5} Count={6} OpenedAt={7} RecoveredAt={8}" -f `
        $reader[0], $reader[1], $reader[2], $reader[3], $reader[4], $reader[5], $reader[6], $reader[7], $reader[8])
}
$reader.Close()
$conn.Close()

# Capture Admin Dashboard screenshot
Write-Host "`nCapturing Admin Dashboard screenshot to docs/screenshots/dashboard_milestone3.png..."
$adminProc.Refresh()
$targetHwnd = $adminProc.MainWindowHandle
if ($targetHwnd -eq [IntPtr]::Zero) {
    # Try finding by window title
    $p = Get-Process -Name "LabGuard.Admin" | Where-Object { $_.MainWindowHandle -ne [IntPtr]::Zero } | Select-Object -First 1
    if ($p) { $targetHwnd = $p.MainWindowHandle }
}

[WindowCapturer]::Capture($targetHwnd, "docs/screenshots/dashboard_milestone3.png")
Write-Host "Screenshot saved: $(Test-Path 'docs/screenshots/dashboard_milestone3.png')"

# Clean up
$serviceListener.Stop()
Stop-Process -Id $agentProc.Id -Force -ErrorAction SilentlyContinue
Stop-Process -Id $adminProc.Id -Force -ErrorAction SilentlyContinue
Stop-Process -Id $centralProc.Id -Force -ErrorAction SilentlyContinue
Write-Host "`nDemo finished cleanly."
