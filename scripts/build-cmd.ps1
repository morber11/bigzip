[CmdletBinding()]
param(
    [string]$Output = "dist\bz.exe",
    [switch]$Clean = $true
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
Push-Location (Join-Path $scriptDir "..") | Out-Null

try {
    $Output = [System.IO.Path]::GetFullPath($Output)
    $outDir = Split-Path -Path $Output -Parent

    if ($Clean -and (Test-Path $Output)) {
        Write-Host "Removing existing output: $Output"
        Remove-Item -Force $Output -ErrorAction Stop
        Remove-Item -Force "dist\example.png" -ErrorAction Stop
    }

    if (-not (Test-Path $outDir)) {
        Write-Host "Creating output directory: $outDir"
        New-Item -ItemType Directory -Path $outDir | Out-Null
    }

    Write-Host "Building bigzip -> $Output" -ForegroundColor Cyan

    $ldflags = "-s -w"
    $goArgs = @("build", "-ldflags", $ldflags, "-o", $Output, "./cmd/bigzip")

    # direct invocation avoids Start-Process quoting/interpolation issues
    Write-Host "Running: go $($goArgs -join ' ')" -ForegroundColor DarkGray
    & go @goArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Error "go build failed with exit code $LASTEXITCODE"
        exit $LASTEXITCODE
    }

    Write-Host "Build succeeded: $Output" -ForegroundColor Green

    Write-Host "Copying example.png to dist folder" -ForegroundColor Cyan
    Copy-Item -Path "assets\example.png" -Destination "dist\" -Force
}
finally {
    Pop-Location | Out-Null
}
