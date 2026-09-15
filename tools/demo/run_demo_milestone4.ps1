Get-Process -Name "LabGuard.Central", "LabGuard.Agent", "LabGuard.Admin" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

$dir = "docs/screenshots"
if (-not (Test-Path $dir)) {
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
}

$central = Start-Process -FilePath "src/LabGuard.Central/bin/Debug/net9.0/LabGuard.Central.exe" -PassThru
Start-Sleep -Seconds 3

$admin = Start-Process -FilePath "src/LabGuard.Admin/bin/Debug/net9.0-windows/LabGuard.Admin.exe" -ArgumentList "--screenshot", "docs/screenshots/dashboard_milestone4.png" -PassThru -Wait

Start-Sleep -Seconds 2
Stop-Process -Id $central.Id -Force -ErrorAction SilentlyContinue

if (Test-Path "docs/screenshots/dashboard_milestone4.png") {
    $len = (Get-Item "docs/screenshots/dashboard_milestone4.png").Length
    Write-Host "SUCCESS: Screenshot size = $len bytes"
} else {
    Write-Host "FAILED: Screenshot not generated"
}
