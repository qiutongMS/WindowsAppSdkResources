# Install-Certificate.ps1
# Automatically elevates to admin and installs the development certificate

# Check if running as administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    # Not running as admin - relaunch with elevation
    Write-Host "Requesting administrator privileges..." -ForegroundColor Yellow
    
    $scriptPath = $MyInvocation.MyCommand.Path
    Start-Process pwsh -Verb RunAs -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$scriptPath`"" -Wait
    exit
}

# Running as administrator - proceed with installation
Write-Host "Installing certificate with administrator privileges..." -ForegroundColor Green

$cerFile = Get-ChildItem "$PSScriptRoot\.user" -Filter "*.certificate.sample.cer" | Select-Object -First 1

if (-not $cerFile) {
    Write-Error "Certificate file not found. Build the package first."
    pause
    exit 1
}

try {
    Import-Certificate -FilePath $cerFile.FullName -CertStoreLocation Cert:\LocalMachine\TrustedPeople | Out-Null
    Write-Host "✓ Certificate installed successfully to LocalMachine\TrustedPeople" -ForegroundColor Green
} catch {
    Write-Error "Failed to install certificate: $_"
    pause
    exit 1
}

pause
