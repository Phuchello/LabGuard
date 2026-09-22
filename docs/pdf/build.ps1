# Build script tu dong bien dich bo 5 cuon PDF LabGuard NT106
param (
    [switch]$CleanOnly
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourceDir = Join-Path $scriptDir "source"
$outputDir = $scriptDir

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   LABGUARD NT106 - BO BIEN DICH TAI LIEU PDF CHUYEN NGHIEP" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "Thu muc nguon : $sourceDir" -ForegroundColor Gray
Write-Host "Thu muc dich  : $outputDir" -ForegroundColor Gray
Write-Host ""

$documents = @(
    @{
        Source = "01_LabGuard_Tong_Quan_A-Z.tex"
        Output = "LabGuard_Tong_Quan_A-Z.pdf"
        Title  = "Tap 1: So tay Tong quan Kien truc A-Z (Master Book)"
    },
    @{
        Source = "02_LabGuard_Huong_Dan_Thanh_Vien_A.tex"
        Output = "LabGuard_Huong_Dan_Thanh_Vien_A.pdf"
        Title  = "Tap 2: So tay Huong dan Thanh vien A (Agent & Protocol)"
    },
    @{
        Source = "03_LabGuard_Huong_Dan_Thanh_Vien_B.tex"
        Output = "LabGuard_Huong_Dan_Thanh_Vien_B.pdf"
        Title  = "Tap 3: So tay Huong dan Thanh vien B (Central Server & Database)"
    },
    @{
        Source = "04_LabGuard_Huong_Dan_Thanh_Vien_C.tex"
        Output = "LabGuard_Huong_Dan_Thanh_Vien_C.pdf"
        Title  = "Tap 4: So tay Huong dan Thanh vien C (Admin Client & WinForms)"
    },
    @{
        Source = "05_LabGuard_Huong_Dan_Thanh_Vien_D.tex"
        Output = "LabGuard_Huong_Dan_Thanh_Vien_D.pdf"
        Title  = "Tap 5: So tay Huong dan Thanh vien D (Gateway / Collector & Reliability)"
    }
)

function Clean-Intermediates {
    param ([string]$dir)
    $exts = @("*.aux", "*.log", "*.out", "*.toc", "*.lof", "*.lot", "*.synctex.gz")
    foreach ($ext in $exts) {
        Get-ChildItem -Path $dir -Filter $ext -ErrorAction SilentlyContinue | Remove-Item -Force
    }
}

if ($CleanOnly) {
    Write-Host "[DON DEP] Dang xoa cac tep trung gian..." -ForegroundColor Yellow
    Clean-Intermediates -dir $sourceDir
    Clean-Intermediates -dir $outputDir
    Write-Host "[XONG] Da don dep sach se!" -ForegroundColor Green
    exit 0
}

$xelatexCmd = Get-Command xelatex -ErrorAction SilentlyContinue
if (-not $xelatexCmd) {
    Write-Error "Khong tim thay lenh 'xelatex' trong PATH he thong!"
}

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
$results = @()

foreach ($doc in $documents) {
    $srcPath = Join-Path $sourceDir $doc.Source
    $rawPdfName = [System.IO.Path]::ChangeExtension($doc.Source, ".pdf")
    $compiledPdfPath = Join-Path $sourceDir $rawPdfName
    $finalPdfPath = Join-Path $outputDir $doc.Output

    Write-Host "----------------------------------------------------------" -ForegroundColor DarkGray
    Write-Host "Dang bien dich: $($doc.Title)" -ForegroundColor Yellow
    Write-Host "Tep nguon: $($doc.Source) -> Dich: $($doc.Output)" -ForegroundColor Gray

    if (-not (Test-Path $srcPath)) {
        Write-Error "Khong tim thay tep nguon: $srcPath"
    }

    Push-Location $sourceDir
    try {
        Write-Host "  [1/2] Luot chay 1: Khoi tao nhan va phan tich cau truc..." -ForegroundColor DarkCyan
        & xelatex -interaction=nonstopmode $doc.Source | Out-Null

        Write-Host "  [2/2] Luot chay 2: Dong bo muc luc va lien ket..." -ForegroundColor DarkCyan
        & xelatex -interaction=nonstopmode $doc.Source | Out-Null
    }
    finally {
        Pop-Location
    }

    if (Test-Path $compiledPdfPath) {
        Move-Item -Path $compiledPdfPath -Destination $finalPdfPath -Force
        $fileInfo = Get-Item $finalPdfPath
        $sizeKb = [Math]::Round($fileInfo.Length / 1KB, 1)

        $pageCount = "N/A"
        try {
            $escapedPath = $finalPdfPath.Replace("\", "/")
            $pyCode = "import pymupdf; doc=pymupdf.open('$escapedPath'); print(len(doc))"
            $pageCount = (python -c $pyCode).Trim()
        }
        catch { }

        Write-Host "  [THANH CONG] Da tao: $($doc.Output) - $sizeKb KB, $pageCount trang" -ForegroundColor Green
        $results += [PSCustomObject]@{
            Document = $doc.Output
            Pages    = $pageCount
            SizeKB   = $sizeKb
            Status   = "PASS"
        }
    }
    else {
        Write-Error "Khong tim thay tep PDF ket qua: $compiledPdfPath"
    }
}

Write-Host ""
Write-Host "Dang don dep cac tep trung gian (.aux, .log, .out, .toc)..." -ForegroundColor Gray
Clean-Intermediates -dir $sourceDir

$stopwatch.Stop()
$totalSec = [Math]::Round($stopwatch.Elapsed.TotalSeconds, 1)

Write-Host "==========================================================" -ForegroundColor Green
Write-Host "   TONG KET KET QUA BIEN DICH PDF LABGUARD NT106" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
$results | Format-Table -AutoSize
Write-Host "Tong thoi gian thuc thi: $totalSec giay." -ForegroundColor Cyan
Write-Host "Toan bo 5 tep PDF da san sang tai: $outputDir" -ForegroundColor Yellow
Write-Host "==========================================================" -ForegroundColor Green
