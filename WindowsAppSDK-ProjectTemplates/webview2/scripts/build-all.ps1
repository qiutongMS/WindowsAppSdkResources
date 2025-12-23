param(
  [ValidateSet('Debug','Release')]
  [string] $Configuration = 'Debug',

  [ValidateSet('x64','ARM64')]
  [string] $Platform = 'x64',

  [switch] $SkipWebBuild
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$webui = Join-Path $repoRoot 'webui'

if (-not $SkipWebBuild) {
  Push-Location $webui
  try {
    if (-not (Test-Path (Join-Path $webui 'node_modules'))) {
      Write-Host 'node_modules not found, running npm install ...'
      npm install
    } else {
      Write-Host 'node_modules present; running npm install to ensure deps are up to date...'
      npm install
    }

    npm run build
  }
  finally {
    Pop-Location
  }
}

# Build native host
& (Join-Path $PSScriptRoot 'build.ps1') -Configuration $Configuration -Platform $Platform

exit $LASTEXITCODE
