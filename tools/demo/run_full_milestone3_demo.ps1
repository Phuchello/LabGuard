Write-Host "================================================================="
Write-Host " LABGUARD PRODUCT MILESTONE 3: COMPLETE AUTOMATED DEMONSTRATION"
Write-Host "================================================================="

# Clean up any leftover processes
Get-Process -Name "LabGuard.Central", "LabGuard.Agent", "LabGuard.Admin" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

$connStr = "Server=localhost;Database=LabGuardDb;Trusted_Connection=True;TrustServerCertificate=True;"

# 1. Start test TCP listener on port 8080
Write-Host "`n>>> [STEP 1] Starting test TCP service listener on port 8080..."
$svcListener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 8080)
$svcListener.Start()
Write-Host "Service listener on port 8080 is ACTIVE."

# 2. Start Central Server
Write-Host "`n>>> [STEP 2] Starting Central Server..."
$centralProc = Start-Process -FilePath "src/LabGuard.Central/bin/Debug/net9.0/LabGuard.Central.exe" -PassThru -RedirectStandardOutput "scratch/central.log" -RedirectStandardError "scratch/central_err.log"
Start-Sleep -Seconds 3

# 3. Start Agent
Write-Host "`n>>> [STEP 3] Starting Agent LAB01-PC01..."
$agentProc = Start-Process -FilePath "src/LabGuard.Agent/bin/Debug/net9.0/LabGuard.Agent.exe" -ArgumentList "LAB01-PC01" -PassThru -RedirectStandardOutput "scratch/agent.log" -RedirectStandardError "scratch/agent_err.log"
Write-Host "Waiting 12s for initial registration, telemetry, and service probes..."
Start-Sleep -Seconds 12

# 4. SCENARIO 1: Generate High CPU Load
Write-Host "`n>>> [STEP 4] SCENARIO 1 (CPU): Applying multi-core CPU load for 18 seconds..."
$burners = 1..[Environment]::ProcessorCount | ForEach-Object {
    Start-Process powershell -ArgumentList "-NoProfile -Command while(1){}" -PassThru -WindowStyle Hidden
}
Write-Host "CPU load active across $([Environment]::ProcessorCount) cores. Waiting 18s for 3 consecutive samples >= 90%..."
Start-Sleep -Seconds 18

# 5. Capture Admin Dashboard Screenshot while incidents are ACTIVE
Write-Host "`n>>> [STEP 5] Capturing live Admin Dashboard screenshot..."
$adminProc = Start-Process -FilePath "src/LabGuard.Admin/bin/Debug/net9.0-windows/LabGuard.Admin.exe" -ArgumentList "--screenshot", "docs/screenshots/dashboard_milestone3.png" -PassThru -Wait
if (Test-Path "docs/screenshots/dashboard_milestone3.png") {
    $imgSize = (Get-Item "docs/screenshots/dashboard_milestone3.png").Length
    Write-Host "Screenshot captured successfully: docs/screenshots/dashboard_milestone3.png ($imgSize bytes)"
} else {
    Write-Host "WARNING: Screenshot was not generated."
}

# Stop CPU load
Write-Host "Stopping CPU load burners..."
$burners | Stop-Process -Force -ErrorAction SilentlyContinue
Write-Host "Waiting 18s for CPU to normalize (<= 75% for 3 samples) to verify RECOVERED state..."
Start-Sleep -Seconds 18

# 6. SCENARIO 3: Service Probe Fail & Recover
Write-Host "`n>>> [STEP 6] SCENARIO 3 (SERVICE): Stopping TCP service on port 8080..."
$svcListener.Stop()
Write-Host "Waiting 12s for Agent to record 2 failed probes (SERVICE_DOWN ACTIVE)..."
Start-Sleep -Seconds 12

Write-Host "Restarting TCP service on port 8080..."
$svcListener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 8080)
$svcListener.Start()
Write-Host "Waiting 12s for Agent to record 2 successful probes (SERVICE_DOWN RECOVERED)..."
Start-Sleep -Seconds 12

# 7. SCENARIO 2: Central Disconnect & Agent Replay
Write-Host "`n>>> [STEP 7] SCENARIO 2 (RECONNECT & REPLAY): Stopping Central Server..."
Stop-Process -Id $centralProc.Id -Force -ErrorAction SilentlyContinue
Write-Host "Central Server stopped. Agent is now buffering unacknowledged telemetry in ReplayBuffer."
Write-Host "Waiting 8s to observe Agent backoff reconnect attempts..."
Start-Sleep -Seconds 8

Write-Host "Restarting Central Server..."
$centralProc2 = Start-Process -FilePath "src/LabGuard.Central/bin/Debug/net9.0/LabGuard.Central.exe" -PassThru -RedirectStandardOutput "scratch/central2.log" -RedirectStandardError "scratch/central2_err.log"
Write-Host "Waiting 10s for Agent to reconnect, authenticate, and flush ReplayBuffer..."
Start-Sleep -Seconds 10

# 8. Check SQL Server Records
Write-Host "`n>>> [STEP 8] Database Verification:"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

Write-Host "`n--- dbo.Incidents ---"
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT IncidentId, DeviceId, RuleType, Severity, ConditionState, WorkflowState, OccurrenceCount, OpenedAtUtc, RecoveredAtUtc FROM dbo.Incidents ORDER BY IncidentId DESC"
$reader = $cmd.ExecuteReader()
while ($reader.Read()) {
    Write-Host ("  Incident #{0} | Device: {1} | Rule: {2,-14} | Sev: {3,-8} | State: {4,-9} | Count: {5} | Opened: {6} | Recovered: {7}" -f `
        $reader[0], $reader[1], $reader[2], $reader[3], $reader[4], $reader[5], $reader[6], $reader[7])
}
$reader.Close()

Write-Host "`n--- dbo.ProcessedBatches (Deduplication Check) ---"
$cmd.CommandText = "SELECT COUNT(*) FROM dbo.ProcessedBatches"
$batchCount = $cmd.ExecuteScalar()
Write-Host "Total ProcessedBatches recorded: $batchCount"

Write-Host "`n--- dbo.MetricSamples (Total Count) ---"
$cmd.CommandText = "SELECT COUNT(*) FROM dbo.MetricSamples WHERE DeviceId = 'LAB01-PC01'"
$sampleCount = $cmd.ExecuteScalar()
Write-Host "Total MetricSamples recorded for LAB01-PC01: $sampleCount"

$conn.Close()

# 9. Check Agent Log for Reconnect & Replay Evidence
Write-Host "`n>>> [STEP 9] Inspecting Agent Log for Reconnect / Replay Lines:"
if (Test-Path "scratch/agent.log") {
    Get-Content "scratch/agent.log" | Where-Object { 
        $_ -match "Connection lost" -or $_ -match "Reconnect in" -or $_ -match "Authenticated" -or $_ -match "Replayed" -or $_ -match "BATCH_ACK"
    } | Select-Object -Last 12 | ForEach-Object { Write-Host "  [Agent Log] $_" }
}

# Cleanup
Write-Host "`n>>> Cleaning up processes..."
$svcListener.Stop()
Stop-Process -Id $agentProc.Id -Force -ErrorAction SilentlyContinue
Stop-Process -Id $centralProc2.Id -Force -ErrorAction SilentlyContinue

Write-Host "`n================================================================="
Write-Host " DEMONSTRATION COMPLETE"
Write-Host "================================================================="
