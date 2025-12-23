
param(
  [ValidateSet('Debug','Release')]
  [string] $Configuration = 'Debug',

  [ValidateSet('x64','ARM64')]
  [string] $Platform = 'x64'
)

$ErrorActionPreference = 'Stop'

& (Join-Path $PSScriptRoot 'build.ps1') -Configuration $Configuration -Platform $Platform

$repoRoot = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $repoRoot "$Platform\$Configuration\Winshell.exe"
if (-not (Test-Path $exe)) {
  throw "Built exe not found: $exe"
}

Start-Process -FilePath $exe
