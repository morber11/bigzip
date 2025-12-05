[CmdletBinding()]
param(
    [string]$CmdOutput = "dist\bz.exe",
    [string]$UiOutput = "dist\bigzip.exe",
    [switch]$Clean = $true
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent

if ($Clean) {
    if (Test-Path "dist") {
        Write-Host "Cleaning dist directory..." -ForegroundColor Yellow
        Remove-Item -Recurse -Force "dist\*"
    }
}

Write-Host "Starting full build process..." -ForegroundColor Cyan

# build CLI
Write-Host "Building command-line tool..." -ForegroundColor Yellow
& "$scriptDir\build-cmd.ps1" -Output $CmdOutput -Clean:$false

# build UI
Write-Host "Building UI..." -ForegroundColor Yellow
& "$scriptDir\build-ui.ps1" -Output $UiOutput

Write-Host "Creating zip archive..." -ForegroundColor Yellow
Get-ChildItem -Path "dist" | Where-Object { $_.Name -ne "example.png" } | Compress-Archive -DestinationPath "dist\bigzip.zip" -Force

Write-Host "Full build completed successfully!" -ForegroundColor Green
Write-Host "Zip archive created: dist\bigzip.zip" -ForegroundColor Green