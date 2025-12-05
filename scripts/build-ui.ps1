param(
    [string]$Output = "dist\bigzip.exe"
)

$ErrorActionPreference = 'Stop'

Push-Location (Join-Path $PSScriptRoot "..") | Out-Null

try {
    Write-Host "Building BigZip UI -> $Output" -ForegroundColor Cyan

    $outputDir = Split-Path -Path $Output -Parent
    if (-not (Test-Path $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir | Out-Null
    }

    dotnet publish ui/ui.csproj -c Release -o (Resolve-Path $outputDir)

    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet build failed"
        exit $LASTEXITCODE
    }

    $builtExe = Join-Path $outputDir "ui.exe"
    if (Test-Path $builtExe) {
        Move-Item -Force $builtExe $Output
        Write-Host "Renamed ui.exe to $(Split-Path $Output -Leaf)" -ForegroundColor Green
    }

    Write-Host "UI build succeeded: $Output" -ForegroundColor Green
}
finally {
    Pop-Location
}