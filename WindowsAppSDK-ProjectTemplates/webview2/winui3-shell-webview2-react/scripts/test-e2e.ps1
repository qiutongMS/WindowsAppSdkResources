param(
    [ValidateSet('Debug','Release')]
    [string] $Configuration = 'Debug',

    [ValidateSet('x64','ARM64')]
    [string] $Platform = 'x64',

    [ValidateSet('Stable','Beta','Dev','Canary')]
    [string] $Channel = 'Stable',

    [string] $Version,

    [string] $DriverDir
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $DriverDir) {
    $DriverDir = Join-Path $repoRoot "tests/Winshell.E2E.WebDriver/edgedriver_$Platform"
}

$driverExe = Join-Path $DriverDir 'msedgedriver.exe'

if (-not (Test-Path $driverExe)) {
    Write-Host "EdgeDriver not found at $driverExe; downloading..." -ForegroundColor Yellow
    $getScript = Join-Path $PSScriptRoot 'get-edgedriver.ps1'
    $args = @('-Channel', $Channel, '-Architecture', $Platform, '-OutputDir', $DriverDir)
    if ($Version) { $args += @('-Version', $Version) }
    pwsh $getScript @args
}

if (-not (Test-Path $driverExe)) {
    throw "EdgeDriver still missing after download: $driverExe"
}

$env:MSEDGEDRIVER_DIR = $DriverDir
$env:TEST_PLATFORM = $Platform

Write-Host "Running E2E tests with TEST_PLATFORM=$Platform, MSEDGEDRIVER_DIR=$DriverDir" -ForegroundColor Cyan

pwsh -c "dotnet test tests/Winshell.E2E.WebDriver/Winshell.E2E.WebDriver.csproj -c $Configuration"
