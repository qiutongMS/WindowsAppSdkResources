# MSIX Publishing Adds Persistent Build Property to .csproj.user

**Keywords:** UapAppxPackageBuildMode, StoreAndSideload, .csproj.user, MSIX publish, unpackaged publish fails, Release build creates MSIX

**Symptom Example:**
After publishing MSIX package from Visual Studio once, every subsequent Release build generates MSIX packages even when not needed. Unpackaged publish operations fail.

Project file `.csproj.user` contains:
```xml
<UapAppxPackageBuildMode>StoreAndSideload</UapAppxPackageBuildMode>
```

---

## Quick Match

**You're seeing this if:**
- Published MSIX once, now Release builds always create MSIX packages
- Unpackaged publish operations fail with packaging errors
- `.csproj.user` file contains `UapAppxPackageBuildMode` property
- Trying to publish without MSIX but build system forces packaging

→ Root cause: VS persists MSIX publish setting in user file

---

## Related Issues

- [#5537](https://github.com/microsoft/WindowsAppSDK/issues/5537) - Publishing an MSIX package influences future builds

---

## Root Cause

**Why it happens:**

When you publish an MSIX package through Visual Studio (Right-click → Publish → MSIX), Visual Studio adds the property `<UapAppxPackageBuildMode>StoreAndSideload</UapAppxPackageBuildMode>` to your `.csproj.user` file to remember the last publish configuration.

This property **persists across builds** and instructs MSBuild to:
1. Generate MSIX packages on every Release build
2. Apply MSIX packaging rules even when publishing unpackaged apps
3. Override project-level packaging settings

**The persistence is by design** from Visual Studio's perspective (to remember user preferences), but creates confusion when:
- Users want to switch between packaged and unpackaged publishing
- CI/CD pipelines run Release builds without needing MSIX
- Developers forget they published MSIX weeks ago

**Affected versions:**
- WindowsAppSDK 1.8-experimental3
- WindowsAppSDK 1.8 stable and later
- All versions using Visual Studio 2022+

---

## Solution

### Option 1: Delete .csproj.user File (Recommended)

**Delete the entire user-specific file:**

```powershell
# Navigate to project directory
Remove-Item "*.csproj.user" -Force
```

**Pros:**
- Removes all user-specific settings (clean slate)
- Forces VS to regenerate with current project settings

**Cons:**
- Loses other user preferences (startup project, debug settings)

**When to use:** Clean builds, CI/CD environments, or when switching developers

---

### Option 2: Remove Specific Property

**Edit `.csproj.user` and remove only the problematic property:**

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <!-- DELETE THIS LINE: -->
    <UapAppxPackageBuildMode>StoreAndSideload</UapAppxPackageBuildMode>
  </PropertyGroup>
</Project>
```

**Pros:**
- Preserves other user settings
- Surgical fix

**Cons:**
- Manual edit required
- Property may reappear after next MSIX publish

**When to use:** Want to keep debug configurations and other user preferences

---

### Option 3: Add to .gitignore

**Prevent `.csproj.user` from being committed:**

Add to `.gitignore`:
```
*.csproj.user
*.user
```

**Why this helps:**
- Each developer can have their own publish preferences
- CI/CD builds won't inherit local publish settings
- Prevents team-wide impact from one developer's MSIX publish

**Note:** This is **preventive**, not a fix for existing builds

---

### Option 4: Conditional Build Logic

**Add to `.csproj` to override user file when needed:**

```xml
<PropertyGroup Condition="'$(Configuration)'=='Release' AND '$(CI)'=='true'">
  <!-- Force unpackaged on CI builds -->
  <UapAppxPackageBuildMode></UapAppxPackageBuildMode>
  <GenerateAppxPackageOnBuild>false</GenerateAppxPackageOnBuild>
</PropertyGroup>
```

**Set environment variable in CI pipeline:**
```bash
# Azure Pipelines / GitHub Actions
export CI=true
```

**When to use:** CI/CD pipelines that need unpackaged builds

---

## Prevention Best Practices

1. **Add .csproj.user to .gitignore** - Prevent accidental commits
2. **Document publishing workflows** - Team should know which builds create MSIX
3. **Use publish profiles** - Create separate `.pubxml` files for MSIX vs unpackaged publishes
4. **CI/CD explicit settings** - Always set packaging properties explicitly in pipeline YAML

**Example publish profile structure:**
```
Properties/
  PublishProfiles/
    MSIX-Store.pubxml         # For MSIX publishing
    Unpackaged-Release.pubxml # For unpackaged publishing
```

---

## Verification

**Check if property is affecting your builds:**

```powershell
# Inspect .csproj.user
Get-Content "*.csproj.user" | Select-String "UapAppxPackageBuildMode"

# Expected output if property exists:
# <UapAppxPackageBuildMode>StoreAndSideload</UapAppxPackageBuildMode>
```

**After fix, verify Release build behavior:**

```powershell
# Clean and rebuild
dotnet clean
dotnet build -c Release

# Check if MSIX was generated (should NOT exist if unpackaged)
Test-Path "bin\Release\**\*.msix"
```

---

## Workaround Status

- ✅ **Workarounds available** - Multiple options depending on use case
- ⚠️ **Root cause remains** - Visual Studio team issue, behavior is by design
- 📝 **Status:** Open (as of 2024) - VS behavior unlikely to change

**Best long-term approach:**
- Use Option 3 (.gitignore) + Option 2 (manual cleanup when needed)
- Switch to publish profiles for explicit control

---

**Updated:** 2026-01-12 | **Confidence:** 0.95
