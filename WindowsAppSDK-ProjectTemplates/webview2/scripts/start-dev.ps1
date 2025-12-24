param(
  [int] $Port = 5173,

  [ValidateSet('Debug','Release')]
  [string] $Configuration = 'Debug',

  [ValidateSet('x64','ARM64','arm64')]
  [string] $Platform = 'x64'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$webui = Join-Path $repoRoot 'webui'
$nativeRun = Join-Path $repoRoot 'scripts\run.ps1'

if (-not (Test-Path (Join-Path $webui 'node_modules'))) {
  Write-Host "node_modules not found, running npm install ..."
  Push-Location $webui
  npm install
  Pop-Location
}

$devUrl = "http://localhost:${Port}/"

# Reuse existing dev server if already running
$reuse = $false
try {
  Invoke-WebRequest -Uri $devUrl -UseBasicParsing -Method Head -TimeoutSec 2 | Out-Null
  $reuse = $true
  Write-Host "Detected running dev server at $devUrl, reusing it." -ForegroundColor Green
} catch {
  $reuse = $false
}

$vite = $null
$viteLogOut = Join-Path $webui "vite-dev.out.log"
$viteLogErr = Join-Path $webui "vite-dev.err.log"

try {
  if (-not $reuse) {
    if (Test-Path $viteLogOut) { Remove-Item $viteLogOut -Force }
    if (Test-Path $viteLogErr) { Remove-Item $viteLogErr -Force }
    $npmCmd = (Get-Command npm.cmd -ErrorAction SilentlyContinue)?.Source
    if (-not $npmCmd) { $npmCmd = "npm.cmd" }

    $vite = Start-Process -FilePath $npmCmd `
      -ArgumentList @("run", "dev", "--", "--host", "--port", "$Port", "--", "--clearScreen", "false") `
      -WorkingDirectory $webui `
      -RedirectStandardOutput $viteLogOut `
      -RedirectStandardError $viteLogErr `
      -WindowStyle Hidden `
      -PassThru
    Write-Host "Started Vite dev server on $devUrl (PID=$($vite.Id)). Logs: $viteLogOut, $viteLogErr"

    # Wait until dev server responds (up to 30s)
    $ready = $false
    for ($i = 0; $i -lt 30; $i++) {
      Start-Sleep -Seconds 1
      try {
        Invoke-WebRequest -Uri $devUrl -UseBasicParsing -Method Head -TimeoutSec 2 | Out-Null
        $ready = $true
        break
      } catch {
        if ($vite.HasExited) {
          Write-Host "Vite dev server log (stdout):" -ForegroundColor Yellow
          if (Test-Path $viteLogOut) { Get-Content $viteLogOut | Write-Host }
          Write-Host "Vite dev server log (stderr):" -ForegroundColor Yellow
          if (Test-Path $viteLogErr) { Get-Content $viteLogErr | Write-Host }
          throw "Vite dev server (PID $($vite.Id)) exited early."
        }
      }
    }

    if (-not $ready) {
      Write-Warning "Vite dev server did not respond on $devUrl within 30s. The shell may show a blank page."
    }
  }

  # Point WinUI WebView to the dev server
  $env:WINSHELL_DEV_URL = $devUrl

  # Start native host (will build if needed)
  & $nativeRun -Configuration $Configuration -Platform $Platform
}
finally {
  # If we started Vite in this script, stop it when script exits
  if ($vite -and (-not $vite.HasExited)) {
    try { Stop-Process -Id $vite.Id -Force } catch { }
  }
}
