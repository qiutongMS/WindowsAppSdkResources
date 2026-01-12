# Published Unpackaged App Crashes - Missing [ProjectName].pri File

**Keywords:** unpackaged publish-crash pri-file enablemsixtoling resource-loading deployment folder-publish  
**Last Updated:** 2026-01-12  
**Status:** Open (Reopened - Long-standing issue)

## Related Issues

- microsoft/WindowsAppSDK#3718 - Removing EnableMsixTooling produces crashing published app (1.3+ → 1.8)
- microsoft/WindowsAppSDK#4603 - Related PRI file publishing issue

## Symptom

Publishing an unpackaged WinUI 3 app from Visual Studio succeeds, but the app **crashes immediately on launch**. The `[ProjectName].pri` file is missing from the publish directory, causing resource loading failures.

**Trigger:**
- Set `<WindowsPackageType>None</WindowsPackageType>` (unpackaged)
- Remove or set `<EnableMsixTooling>false</EnableMsixTooling>`
- Publish from Visual Studio (right-click → Publish → Folder)

**Result:**
- Build succeeds without errors
- `[ProjectName].pri` exists in `bin\Release\` but **NOT** in `publish\` folder
- App crashes on startup (cannot load resources)

## Error Example

**Project file:**
```xml
<PropertyGroup>
  <WindowsPackageType>None</WindowsPackageType>
  <!-- EnableMsixTooling removed or set to false -->
</PropertyGroup>
```

**Publish output:**
```
Published folder structure:
publish/
├── MyApp.exe
├── MyApp.dll
├── Microsoft.UI.Xaml.dll
├── ... (other DLLs)
└── ❌ MyApp.pri MISSING (should be here but isn't)

bin/Release/net8.0-windows10.0.19041.0/
├── MyApp.exe
└── ✅ MyApp.pri (exists here but not copied to publish)
```

**Runtime crash:**
```
App starts → Blank window → Crash
(Resources like XAML, images, strings cannot load)
```

## Root Cause

`<EnableMsixTooling>` serves two purposes:
1. **Renames** `[ProjectName].pri` to `resources.pri` (for MSIX packaging)
2. **Triggers copy** of `.pri` file to publish directory

When `EnableMsixTooling` is removed/false:
- ❌ PRI file is **generated** during build (`makepri.exe` runs)
- ❌ PRI file exists in `bin\Release\`
- ❌ But publish targets **don't copy it** to publish folder
- ✅ For packaged apps, MSIX packaging handles this
- ❌ For unpackaged apps, **no copy happens → crash**

**Why Visual Studio doesn't warn:**
- Build succeeds (`.pri` is generated in bin)
- Publish succeeds (copies .exe and .dll files)
- Only fails at **runtime** when app tries to load resources

## Solutions

### ✅ Solution 1: Keep EnableMsixTooling (Easiest)

Even for unpackaged apps, **keep** `EnableMsixTooling`:

```xml
<PropertyGroup>
  <WindowsPackageType>None</WindowsPackageType>
  <EnableMsixTooling>true</EnableMsixTooling> <!-- Keep this -->
  <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
  <SelfContained>true</SelfContained>
</PropertyGroup>
```

**Why this works:**
- `EnableMsixTooling` triggers PRI file copy to publish folder
- It renames to `resources.pri`, which unpackaged apps can load
- No side effects for unpackaged deployment

### ✅ Solution 2: Manual Post-Publish Copy

Add post-publish target to `.csproj`:

```xml
<Target Name="CopyPriToPublish" AfterTargets="Publish">
  <PropertyGroup>
    <PriFileName>$(AssemblyName).pri</PriFileName>
    <PriSourcePath>$(OutDir)$(PriFileName)</PriSourcePath>
    <PriDestPath>$(PublishDir)$(PriFileName)</PriDestPath>
  </PropertyGroup>
  
  <Message Text="Copying PRI file: $(PriSourcePath) → $(PriDestPath)" Importance="high" />
  
  <Copy SourceFiles="$(PriSourcePath)" 
        DestinationFiles="$(PriDestPath)" 
        SkipUnchangedFiles="true" 
        Condition="Exists('$(PriSourcePath)')" />
</Target>
```

**Verification:**
```powershell
# After publish, check for PRI file
Test-Path "publish\MyApp.pri"  # Should return True
```

### ✅ Solution 3: Automated Post-Build Script

For CI/CD or command-line builds:

```powershell
# publish-unpackaged.ps1
param(
    [string]$ProjectPath = "MyApp.csproj",
    [string]$Configuration = "Release",
    [string]$OutputPath = "publish"
)

# Publish
dotnet publish $ProjectPath -c $Configuration -o $OutputPath

# Copy PRI file
$projectName = [System.IO.Path]::GetFileNameWithoutExtension($ProjectPath)
$priFile = "bin\$Configuration\net8.0-windows10.0.19041.0\$projectName.pri"

if (Test-Path $priFile) {
    Copy-Item $priFile -Destination "$OutputPath\$projectName.pri" -Force
    Write-Host "✅ Copied $projectName.pri to publish folder" -ForegroundColor Green
} else {
    Write-Error "❌ PRI file not found: $priFile"
    exit 1
}
```

### Workaround: Manual Copy After Each Publish

If you can't modify project files:

```powershell
# After Visual Studio publish
Copy-Item "bin\Release\net8.0-windows10.0.19041.0\MyApp.pri" `
          -Destination "publish\MyApp.pri"
```

## Verification

Before distributing your published app, verify PRI file exists:

```powershell
# Check publish folder
$publishFolder = "bin\Release\net8.0-windows10.0.19041.0\publish"
$priFile = Get-ChildItem -Path $publishFolder -Filter "*.pri" -Recurse

if ($priFile) {
    Write-Host "✅ PRI file found: $($priFile.FullName)" -ForegroundColor Green
    
    # Optionally dump contents to verify resources
    makepri.exe dump /if $priFile.FullName /of "pri_verification.xml" /dt detailed
} else {
    Write-Error "❌ No PRI file in publish folder - app will crash!"
}
```

## Prevention

**For new unpackaged projects:**

1. **Use template with EnableMsixTooling** - Don't remove it even for unpackaged
2. **Test publish early** - Run published .exe immediately after first publish
3. **Add verification step** - Check for `.pri` file in publish folder
4. **Document in README** - Add note: "Keep EnableMsixTooling=true for PRI file copy"

**For CI/CD pipelines:**

```yaml
# Azure DevOps / GitHub Actions
- task: DotNetCoreCLI@2
  inputs:
    command: 'publish'
    projects: '**/*.csproj'
    arguments: '-c Release -o $(Build.ArtifactStagingDirectory)'

# Verification step
- powershell: |
    $priFiles = Get-ChildItem -Path $(Build.ArtifactStagingDirectory) -Filter "*.pri" -Recurse
    if ($priFiles.Count -eq 0) {
        Write-Error "PRI file missing - publish will fail at runtime!"
        exit 1
    }
  displayName: 'Verify PRI File Exists'
```

## Related: Automated dotnet publish Issues

**Note:** This issue also affects `dotnet publish` from command line (not just VS):

```powershell
# This will have same issue if EnableMsixTooling is false
dotnet publish -c Release
# ❌ PRI file not copied to publish folder
```

**Solution:** Use same workarounds above (keep EnableMsixTooling or add post-publish target).

## Version Information

- **Affected versions:** WindowsAppSDK 1.3+ → 1.8 (still open)
- **Status:** Reopened - no official fix yet
- **Workaround required:** Yes - use Solution 1 (simplest) or Solution 2
- **Impact:** All unpackaged apps without EnableMsixTooling
- **Related:** Issue also affects ResourceLoader API failures in unpackaged apps
