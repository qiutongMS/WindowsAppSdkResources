param(
  [ValidateSet('Debug','Release')]
  [string] $Configuration = 'Debug',

  [ValidateSet('x64','ARM64')]
  [string] $Platform = 'x64'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'

if (-not (Test-Path $vswhere)) {
  throw "vswhere not found at: $vswhere"
}

$vsPath = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
if (-not $vsPath) {
  throw 'Visual Studio with MSBuild not found. Install VS 2022 (or Build Tools) + MSBuild.'
}

$msbuild = Join-Path $vsPath 'MSBuild\Current\Bin\MSBuild.exe'
if (-not (Test-Path $msbuild)) {
  throw "MSBuild.exe not found at: $msbuild"
}

$sln = Join-Path $repoRoot 'winshell.slnx'
& $msbuild $sln /restore /t:Build /p:Configuration=$Configuration /p:Platform=$Platform /v:minimal
exit $LASTEXITCODE
