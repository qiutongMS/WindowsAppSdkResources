param (
    [string]$DistDir = "dist"
)

$ErrorActionPreference = "Stop"

# Configuration
$PythonVersion = "3.12"
$PackageDir = Resolve-Path $DistDir -ErrorAction SilentlyContinue
if ($null -eq $PackageDir) { $PackageDir = Join-Path $PWD $DistDir }
else { $PackageDir = $PackageDir.Path }

# 1. Clean and Create Dist Directory
Write-Host "Cleaning dist directory..."
if (Test-Path $PackageDir) { Remove-Item $PackageDir -Recurse -Force }
New-Item -ItemType Directory -Path $PackageDir | Out-Null

# 2. Install Standalone Python directly to dist
Write-Host "Installing Python $PythonVersion to $PackageDir..."
uv python install $PythonVersion --install-dir $PackageDir --no-bin --no-registry

# Find the actual installation folder (uv creates a subfolder like cpython-3.12.x...)
$InstalledFolder = Get-ChildItem -Path $PackageDir -Directory | Where-Object { $_.Name -like "cpython-*" } | Select-Object -First 1
if ($null -eq $InstalledFolder) { throw "Failed to find installed Python folder" }

$PythonTargetDir = $InstalledFolder.FullName
$PythonExe = Join-Path $PythonTargetDir "python.exe"

# Remove problematic DLLs that cause signing issues (Bad EXE format 0x800700C1)
# These are Tcl/Tk libraries which are often corrupted or have issues in some distributions
Write-Host "Removing problematic Tcl/Tk DLLs..."
Remove-Item (Join-Path $PythonTargetDir "DLLs\tcl86t.dll") -ErrorAction SilentlyContinue
Remove-Item (Join-Path $PythonTargetDir "DLLs\tk86t.dll") -ErrorAction SilentlyContinue

# 3. Install the Package into the Bundled Python
Write-Host "Building package wheels..."
$BuildDir = Join-Path $PWD "build"
if (Test-Path $BuildDir) { Remove-Item $BuildDir -Recurse -Force }
uv build --wheel --out-dir $BuildDir

Write-Host "Installing package and dependencies into bundled Python..."
$SitePackages = Join-Path $PythonTargetDir "Lib\site-packages"
New-Item -ItemType Directory -Path $SitePackages -Force | Out-Null

# Install the built wheel and its dependencies
$WheelFile = (Get-ChildItem "$BuildDir\*.whl" | Select-Object -First 1).FullName
uv pip install $WheelFile --python $PythonExe --target $SitePackages

# # 4. Copy Assets and Manifest
# Write-Host "Copying assets and manifest..."
# if (Test-Path "Assets") {
#     Copy-Item -Path "Assets" -Destination "$PackageDir\Assets" -Recurse -Force
# }
# Copy-Item "appxmanifest.xml" -Destination "$PackageDir\appxmanifest.xml"
