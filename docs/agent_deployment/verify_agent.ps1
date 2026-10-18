[CmdletBinding()]
param([switch]$Publish)

$ErrorActionPreference = 'Stop'
if (-not $IsWindows) { throw 'Run this validation on Windows; the real collector is Windows-specific.' }
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$runDirectory = Join-Path $repoRoot ('scratch/agent-readiness/' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $runDirectory | Out-Null
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'

function Invoke-DotnetCheck {
    param([string[]]$Arguments, [string]$LogName)
    & dotnet @Arguments 2>&1 | Tee-Object -FilePath (Join-Path $runDirectory $LogName)
    if ($LASTEXITCODE -ne 0) { throw "dotnet command failed with exit $LASTEXITCODE. See $LogName." }
}

Push-Location $repoRoot
try {
    & dotnet --list-sdks | Tee-Object -FilePath (Join-Path $runDirectory 'sdks.txt')
    if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect installed SDKs.' }
    Invoke-DotnetCheck -Arguments @('build', 'LabGuard.sln', '--nologo') -LogName 'build.log'
    Invoke-DotnetCheck -Arguments @('test', 'LabGuard.sln', '--no-build', '--no-restore', '--nologo', '--logger', 'trx', '--results-directory', (Join-Path $runDirectory 'tests')) -LogName 'tests.log'

    if ($Publish) {
        $publishDirectory = Join-Path $runDirectory 'win-x64'
        Invoke-DotnetCheck -Arguments @('publish', 'src/LabGuard.Agent/LabGuard.Agent.csproj', '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true', '-p:PublishTrimmed=false', '-o', $publishDirectory) -LogName 'publish.log'
        $agentExe = Join-Path $publishDirectory 'LabGuard.Agent.exe'
        $testConfig = Join-Path $runDirectory 'agentsettings.json'
        $checks = @(
            @{ Name = 'init'; Arguments = @('--init-config', $testConfig, '--central-host', '127.0.0.1'); Expected = 0 },
            @{ Name = 'validate'; Arguments = @('--config', $testConfig, '--validate-config'); Expected = 0 },
            @{ Name = 'collect-local'; Arguments = @('--config', $testConfig, '--collect-once'); Expected = 0 },
            @{ Name = 'monitoring-blocked'; Arguments = @('--config', $testConfig); Expected = 5 }
        )
        foreach ($check in $checks) {
            $agentArguments = $check.Arguments
            & $agentExe @agentArguments 2>&1 | Tee-Object -FilePath (Join-Path $runDirectory ($check.Name + '.log'))
            if ($LASTEXITCODE -ne $check.Expected) { throw "Unexpected exit for $($check.Name): $LASTEXITCODE." }
        }
    }
    Write-Host "Validation completed. Local logs/TRX/publish output: $runDirectory"
    Write-Host 'No ZIP, Git write, Service install, remote deployment or monitoring session is performed.'
}
finally { Pop-Location }
exit 0
