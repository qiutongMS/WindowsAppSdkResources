# MSIX Publish Adds UapAppxPackageBuildMode to .csproj.user - Breaks Future Builds

**Keywords:** msix publish visual-studio csproj-user UapAppxPackageBuildMode build-mode unpackaged-publish-fails  
**Last Updated:** 2026-01-12  
**Status:** Open (Visual Studio issue affecting WinAppSDK users)

## Related Issues

- microsoft/WindowsAppSDK#5537 - Publishing MSIX influences future builds (1.8-experimental3 → 1.8 stable)

## Symptom

After publishing an MSIX package from Visual Studio, every subsequent Release build automatically creates an MSIX package. Additionally, publishing as unpackaged fails until the added property is removed.

**User Impact:**
- Slower builds (unnecessary MSIX creation)
- Unpackaged publish failures
- Build behavior changes unexpectedly after MSIX publish

## Error Example

**After MSIX publish, .csproj.user file contains:**
```xml
<PropertyGroup>
  <UapAppxPackageBuildMode>StoreAndSideload</UapAppxPackageBuildMode>
</PropertyGroup>
```

**Result:**
```bash
# Every Release build now creates MSIX unnecessarily
Build succeeded.
Creating MSIX package... (unwanted)

# Unpackaged publish fails
Publish failed: Cannot publish unpackaged when UapAppxPackageBuildMode is set
```

## Root Cause

Visual Studio adds `<UapAppxPackageBuildMode>StoreAndSideload</UapAppxPackageBuildMode>` to the `.csproj.user` file when publishing MSIX. This property persists across builds and overrides project settings, forcing MSIX creation in all subsequent builds.

**Why this happens:**
- `.csproj.user` is a per-user settings file (not committed to source control)
- VS writes publish configuration there to remember last publish mode
- MSBuild reads this file and honors the property, even for non-MSIX builds
- The property is **not removed** when switching to unpackaged publish

## Solutions

### ✅ Immediate Fix: Delete Property from .csproj.user

**Steps:**
1. Locate `[ProjectName].csproj.user` in project directory
2. Open in text editor
3. Remove the `<UapAppxPackageBuildMode>` element or delete entire file
4. Rebuild

```xml
<!-- DELETE this line from .csproj.user -->
<UapAppxPackageBuildMode>StoreAndSideload</UapAppxPackageBuildMode>
```

**Safe to delete entire `.csproj.user`?** 
Yes, if it only contains VS-generated properties. It will be regenerated with defaults.

### Alternative: Change Value to SideloadOnly

If you want to keep MSIX builds but avoid automatic packaging:

```xml
<!-- In .csproj.user, change from: -->
<UapAppxPackageBuildMode>StoreAndSideload</UapAppxPackageBuildMode>

<!-- To: -->
<UapAppxPackageBuildMode>SideloadOnly</UapAppxPackageBuildMode>
```

**Effect:** MSIX will only be created when explicitly requested, not on every build.

## Prevention

**Best Practices:**

1. **Add .csproj.user to .gitignore** - Prevent committing per-user settings:
   ```gitignore
   *.csproj.user
   *.user
   ```

2. **Document the workaround** - Add to team README:
   > After MSIX publish, if builds are slow or unpackaged publish fails, delete `.csproj.user` or remove `UapAppxPackageBuildMode` property.

3. **Use VS publish profiles** - Create separate profiles for MSIX vs unpackaged:
   - `Properties/PublishProfiles/MSIX.pubxml` 
   - `Properties/PublishProfiles/Unpackaged.pubxml`

4. **Report to Visual Studio team** - This is a VS behavior issue. File feedback:
   - **Help → Send Feedback → Report a Problem** in Visual Studio
   - Reference microsoft/WindowsAppSDK#5537

## Workaround for CI/CD

If builds run in CI/CD and encounter this issue:

```powershell
# Pre-build step: Remove problematic property
$userFile = "MyProject.csproj.user"
if (Test-Path $userFile) {
    (Get-Content $userFile) -replace '<UapAppxPackageBuildMode>.*</UapAppxPackageBuildMode>', '' | Set-Content $userFile
}
```

## Version Information

- **Affected versions:** WindowsAppSDK 1.8-experimental3 → 1.8 stable (and likely earlier)
- **Root cause:** Visual Studio publish behavior (not WinAppSDK bug)
- **Status:** Open - workaround required
- **Recommended action:** File VS feedback + use workaround above
