# Compile handbook candidates; review visually before replacing tracked PDFs.
[CmdletBinding()]
param(
    [string]$OutputDirectory,
    [ValidateSet('Overview', 'A', 'B', 'C', 'D')]
    [string[]]$Handbook = @('Overview', 'A', 'B', 'C', 'D')
)
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$sourceDirectory = Join-Path $PSScriptRoot 'source'
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot ('scratch/handbook-build/' + [Guid]::NewGuid().ToString('N'))
}
else { $OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory, $repoRoot) }
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$workDirectory = Join-Path $OutputDirectory ('.work-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $workDirectory | Out-Null
# MiKTeX can prefer stale aux/toc files in its current directory even when
# -output-directory is set. Compile from a fresh directory with source only.
Get-ChildItem -LiteralPath $sourceDirectory -File |
    Where-Object { $_.Extension -in @('.tex', '.sty') } |
    ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $workDirectory }
$xelatex = if ($env:LABGUARD_XELATEX) { $env:LABGUARD_XELATEX } else { (Get-Command xelatex -ErrorAction Stop).Source }
$documents = @(
    @('01_LabGuard_Tong_Quan_A-Z.tex', 'LabGuard_Tong_Quan_A-Z.pdf', 'Overview'),
    @('02_LabGuard_Huong_Dan_Thanh_Vien_A.tex', 'LabGuard_Huong_Dan_Thanh_Vien_A.pdf', 'A'),
    @('03_LabGuard_Huong_Dan_Thanh_Vien_B.tex', 'LabGuard_Huong_Dan_Thanh_Vien_B.pdf', 'B'),
    @('04_LabGuard_Huong_Dan_Thanh_Vien_C.tex', 'LabGuard_Huong_Dan_Thanh_Vien_C.pdf', 'C'),
    @('05_LabGuard_Huong_Dan_Thanh_Vien_D.tex', 'LabGuard_Huong_Dan_Thanh_Vien_D.pdf', 'D')
)
$compiled = @()
Push-Location $workDirectory
try {
    foreach ($document in $documents) {
        if ($document[2] -notin $Handbook) { continue }
        $sourceName = $document[0]
        Write-Host "Compiling $sourceName (three passes)..."
        for ($pass=1; $pass -le 3; $pass++) {
            $compilerArgs = @('-halt-on-error', '-file-line-error', '-interaction=nonstopmode', "-output-directory=$workDirectory", $sourceName)
            & $xelatex @compilerArgs *> (Join-Path $workDirectory ($sourceName + ".pass$pass.stdout.txt"))
            if ($LASTEXITCODE -ne 0) { throw "XeLaTeX failed: $sourceName pass $pass, exit $LASTEXITCODE. See $workDirectory" }
        }
        $candidate = Join-Path $workDirectory ([IO.Path]::ChangeExtension($sourceName, '.pdf'))
        if (-not (Test-Path -LiteralPath $candidate) -or (Get-Item -LiteralPath $candidate).Length -eq 0) { throw "Missing fresh PDF: $sourceName" }
        $compiled += @{ Candidate=$candidate; Output=(Join-Path $OutputDirectory $document[1]) }
    }
}
finally { Pop-Location }
# No old source-directory PDF can be mistaken for a successful new compilation.
foreach ($item in $compiled) { Copy-Item -LiteralPath $item.Candidate -Destination $item.Output -Force }
Write-Host "Compiled $($compiled.Count) fresh PDF candidates: $OutputDirectory"
Write-Host 'Inspect rendered pages before copying candidates into docs/pdf. Compilation alone is not visual acceptance.'
Write-Host "Compiler logs retained under $workDirectory; no automatic deletion or tracked-PDF replacement."
exit 0
