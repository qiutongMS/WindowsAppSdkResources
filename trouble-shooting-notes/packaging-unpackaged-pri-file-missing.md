# Crash on Launch: Missing .pri File After Removing EnableMsixTooling

**Keywords:** EnableMsixTooling, pri file missing, published app crashes, resources.pri, unpackaged app crash, dotnet publish, resource loading failure

**Error Example:**
Application crashes immediately on startup after publishing. No visible error in Release build, but app fails to load resources.

**Publish output missing:**
```
Expected: bin\Release\publish\MyApp.pri
Actual:   File does not exist
```

**Runtime behavior:**
- App window never appears
- Process starts then exits immediately
- Event Viewer may show resource loading failure

---

## Quick Match

**You're seeing this if:**
- Removed `<EnableMsixTooling>` from .csproj
- Published app from Visual Studio (Right-click → Publish)
- Published app crashes on launch
- `.pri` file is NOT in publish output directory
- Issue started after removing MSIX packaging properties

→ Root cause: EnableMsixTooling handles pri file copy; without it, file not copied

---

## Related Issues

- [#3718](https://github.com/microsoft/WindowsAppSDK/issues/3718) - Removing EnableMsixTooling produces crashing published app (Reopened)

---

## Root Cause

**Why it happens:**

The `<EnableMsixTooling>` property in .csproj controls **two critical behaviors**:

1. **PRI file naming:**
   - `EnableMsixTooling=true` → Renames to `resources.pri` (MSIX standard)
   - `EnableMsixTooling=false` or absent → Uses `[ProjectName].pri`

2. **PRI file deployment:**
   - `EnableMsixTooling=true` → Copies pri file to publish directory
   - `EnableMsixTooling=false` or absent → ❌ **Does NOT copy pri file**

**The problem:**

When you remove `<EnableMsixTooling>` to create an unpackaged app:
- Build still generates `MyApp.pri` in `bin\Release\`
- Visual Studio Publish does NOT copy it to publish folder
- App launches and tries to load resources from `MyApp.pri`
- ❌ File not found → Crash

**This is a build tooling gap** - The pri file is needed for unpackaged apps (for resource loading), but the tooling assumes unpackaged apps don't need automatic deployment.

**Affected versions:**
- WindowsAppSDK 1.3+
- Still present in 1.8, 2.0
- Affects both Visual Studio Publish and `dotnet publish`

---

## Solutions

### Solution 1: Keep EnableMsixTooling (Recommended)

**Keep the property in your .csproj:**

```xml
<PropertyGroup>
  <EnableMsixTooling>true</EnableMsixTooling>
  <!-- You can still create unpackaged apps with this enabled -->
  <WindowsPackageType>None</WindowsPackageType>
</PropertyGroup>
```

**Why this works:**
- EnableMsixTooling handles pri file copy automatically
- `WindowsPackageType=None` ensures unpackaged app (no MSIX)
- Renames to `resources.pri` which is the expected standard name

**Pros:**
- ✅ Automated, no manual steps
- ✅ Works with dotnet publish
- ✅ CI/CD friendly

**Cons:**
- Uses "resources.pri" name instead of "[ProjectName].pri"

**Verification:**
```powershell
dotnet publish -c Release
Test-Path "bin\Release\net8.0-windows10.0.19041.0\publish\resources.pri"
# Should return: True
```

---

### Solution 2: Manual Copy with Post-Build Event

**Add post-build event to .csproj to copy pri file:**

```xml
<Target Name="CopyPriFile" AfterTargets="Build">
  <PropertyGroup>
    <PriFileName>$(ProjectName).pri</PriFileName>
    <PriSourcePath>$(OutDir)$(PriFileName)</PriSourcePath>
  </PropertyGroup>
  <Copy SourceFiles="$(PriSourcePath)" 
        DestinationFolder="$(PublishDir)" 
        Condition="Exists('$(PriSourcePath)') AND '$(PublishDir)' != ''" />
</Target>
```

**For dotnet publish, add to publish profile (.pubxml):**

```xml
<Target Name="CopyPriFileOnPublish" AfterTargets="Publish">
  <PropertyGroup>
    <PriSource>$(OutDir)$(ProjectName).pri</PriSource>
    <PriDest>$(PublishDir)$(ProjectName).pri</PriDest>
  </PropertyGroup>
  <Copy SourceFiles="$(PriSource)" DestinationFiles="$(PriDest)" />
</Target>
```

**Pros:**
- ✅ Keeps original "[ProjectName].pri" name
- ✅ Works without EnableMsixTooling

**Cons:**
- Manual setup required
- Must maintain custom build logic

---

### Solution 3: Manual Copy After Publish

**Copy pri file manually after each publish:**

```powershell
# After Visual Studio Publish or dotnet publish
$priFile = "bin\Release\net8.0-windows10.0.19041.0\MyApp.pri"
$publishDir = "bin\Release\net8.0-windows10.0.19041.0\publish\"

Copy-Item $priFile $publishDir -Force
```

**Pros:**
- ✅ Simple, no project changes

**Cons:**
- ❌ Manual step every time
- ❌ Easy to forget
- ❌ Breaks CI/CD automation

**When to use:** One-time debugging or emergency fix

---

### Solution 4: Automate in CI/CD Pipeline

**Add to GitHub Actions / Azure Pipelines:**

```yaml
- name: Publish WinAppSDK App
  run: dotnet publish -c Release -o publish

- name: Copy PRI file
  run: |
    $priFile = Get-ChildItem -Path "bin\Release" -Filter "*.pri" -Recurse | Select-Object -First 1
    Copy-Item $priFile.FullName "publish\" -Force
  shell: pwsh
```

**Pros:**
- ✅ Automated in pipeline
- ✅ No local developer impact

**Cons:**
- Must remember to copy on local publish too

---

## Prevention Best Practices

1. **Default to EnableMsixTooling=true** - Even for unpackaged apps
2. **Document publish steps** - If using manual copy, add to README
3. **Test published apps** - Always run published exe before distributing
4. **Use publish profiles** - Create profiles with proper post-publish steps

**Recommended .csproj setup for unpackaged apps:**

```xml
<PropertyGroup>
  <!-- Unpackaged app but with MSIX tooling for pri file handling -->
  <EnableMsixTooling>true</EnableMsixTooling>
  <WindowsPackageType>None</WindowsPackageType>
  <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
</PropertyGroup>
```

---

## Diagnostic Steps

**Check if pri file was generated:**
```powershell
# Look in build output
Get-ChildItem -Path "bin\Release" -Filter "*.pri" -Recurse
```

**Check if pri file is in publish folder:**
```powershell
# After publish
Get-ChildItem -Path "bin\Release\**\publish" -Filter "*.pri" -Recurse
```

**Verify app resource loading:**
```powershell
# Check app manifest references
$publishExe = "bin\Release\net8.0-windows10.0.19041.0\publish\MyApp.exe"
# Run app and check Event Viewer > Windows Logs > Application for resource errors
```

**Expected publish folder contents for working app:**
```
publish\
  MyApp.exe
  MyApp.pri  (or resources.pri)
  Microsoft.*.dll
  ... other dependencies
```

---

## Workaround Status

- ⚠️ **Issue reopened** - Long-standing tooling gap
- ✅ **Reliable workarounds exist** - Solution 1 is stable
- 📝 **Team-Build** - Needs MSBuild/SDK tooling fix

**Recommended approach (by priority):**
1. Use EnableMsixTooling=true + WindowsPackageType=None
2. Add post-build event for pri copy
3. Manual copy (only for testing/debugging)

---

**Updated:** 2026-01-12 | **Confidence:** 0.96
