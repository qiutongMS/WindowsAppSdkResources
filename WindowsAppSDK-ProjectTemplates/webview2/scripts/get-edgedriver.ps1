param(
    [ValidateSet('Stable','Beta','Dev','Canary')]
    [string] $Channel = 'Stable',

    [ValidateSet('x64','ARM64')]
    [string] $Architecture = 'x64',

    [string] $Version,

    [string] $OutputDir
)

$ErrorActionPreference = 'Stop'

function Get-LatestEdgeDriverVersion {
    param(
        [string] $Channel
    )

    $tag = if ($Channel -eq 'Stable') { 'LATEST_RELEASE' } else { "LATEST_RELEASE_$($Channel.ToUpper())" }

    $primary = "https://msedgedriver.azureedge.net/$tag"
    $fallback = "https://msedgewebdriverstorage.blob.core.windows.net/edgewebdriver/$tag"

    try {
        return (Invoke-WebRequest -UseBasicParsing -Uri $primary).Content.Trim()
    }
    catch {
        Write-Warning "Primary version endpoint failed ($primary). Trying fallback..."
        return (Invoke-WebRequest -UseBasicParsing -Uri $fallback).Content.Trim()
    }
}

function Get-InstalledEdgeVersion {
    $regPaths = @(
        'HKLM:\SOFTWARE\Microsoft\Edge\BLBeacon',
        'HKCU:\SOFTWARE\Microsoft\Edge\BLBeacon'
    )

    foreach ($path in $regPaths) {
        try {
            $val = Get-ItemProperty -Path $path -Name 'version' -ErrorAction Stop
            if ($val.version) { return $val.version }
        }
        catch { }
    }

    $exeCandidates = @(
        "$Env:ProgramFiles (x86)\Microsoft\Edge\Application\msedge.exe",
        "$Env:ProgramFiles\Microsoft\Edge\Application\msedge.exe"
    )

    foreach ($exe in $exeCandidates) {
        if (Test-Path $exe) {
            $info = Get-Item $exe
            return $info.VersionInfo.ProductVersion
        }
    }

    return $null
}

function Resolve-Version {
    param(
        [string] $Channel,
        [string] $Version
    )

    if (-not [string]::IsNullOrWhiteSpace($Version)) {
        return $Version.Trim()
    }

    try {
        return Get-LatestEdgeDriverVersion -Channel $Channel
    }
    catch {
        Write-Warning "Failed to fetch latest version online. Falling back to installed Edge version..."
        $installed = Get-InstalledEdgeVersion
        if ($installed) { return $installed }
        throw "Could not resolve EdgeDriver version (online fetch failed and installed Edge not found)."
    }
}

$resolvedVersion = Resolve-Version -Channel $Channel -Version $Version
$archLower = $Architecture.ToLowerInvariant()

$primaryUrl = "https://msedgedriver.azureedge.net/$resolvedVersion/edgedriver_${archLower}.zip"
$fallbackUrl = "https://msedgewebdriverstorage.blob.core.windows.net/edgewebdriver/$resolvedVersion/edgedriver_${archLower}.zip"

$repoRoot = Split-Path -Parent $PSScriptRoot

if (-not $OutputDir) {
    $OutputDir = Join-Path $repoRoot "tests/Winshell.E2E.WebDriver/edgedriver_$Architecture"
}

$tempZip = [IO.Path]::ChangeExtension([IO.Path]::GetTempFileName(), '.zip')

Write-Host "Resolved EdgeDriver version: $resolvedVersion ($Architecture)" -ForegroundColor Cyan
Write-Host "Downloading from: $primaryUrl" -ForegroundColor Cyan
try {
    Invoke-WebRequest -UseBasicParsing -Uri $primaryUrl -OutFile $tempZip

    if (-not (Test-Path $OutputDir)) {
        New-Item -ItemType Directory -Path $OutputDir | Out-Null
    }

    Expand-Archive -Path $tempZip -DestinationPath $OutputDir -Force
    Write-Host "Done. Driver extracted to $OutputDir" -ForegroundColor Green
}
catch {
    Write-Warning "Primary download failed ($primaryUrl). Trying fallback..."
    try {
        Invoke-WebRequest -UseBasicParsing -Uri $fallbackUrl -OutFile $tempZip

        if (-not (Test-Path $OutputDir)) {
            New-Item -ItemType Directory -Path $OutputDir | Out-Null
        }

        Expand-Archive -Path $tempZip -DestinationPath $OutputDir -Force
        Write-Host "Done (fallback). Driver extracted to $OutputDir" -ForegroundColor Green
    }
    finally {
        if (Test-Path $tempZip) { Remove-Item $tempZip -Force }
    }

    return
}
finally {
    if (Test-Path $tempZip) { Remove-Item $tempZip -Force }
}

# Print helpful env hint
$driverPath = Join-Path $OutputDir 'msedgedriver.exe'
Write-Host "Set MSEDGEDRIVER_DIR to: $OutputDir" -ForegroundColor Yellow
if (Test-Path $driverPath) {
    Write-Host "Driver file: $driverPath" -ForegroundColor Yellow
}
