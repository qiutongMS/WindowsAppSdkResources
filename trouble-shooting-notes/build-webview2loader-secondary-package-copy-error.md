# WebView2Loader.dll Build Error - Secondary NuGet Package Reference

**Keywords:** webview2 webview2loader build-error secondary-package msb3030 nuget transitive-package  
**Last Updated:** 2026-01-12  
**Status:** ✅ Fixed in WebView2 1.0.2903.40+ (Dec 2024) & WinAppSDK 1.7+

## Related Issues

- microsoft/WindowsAppSDK#4807 - WebView2Loader.dll not found when WindowsAppSDK 1.6 referenced through secondary NuGet package

## Symptom

Build fails with **MSB3030** error when a class library references WindowsAppSDK (which includes WebView2) and is consumed by another project as a NuGet package.

**Scenario:**
- LibraryA.csproj → References WindowsAppSDK 1.6 → Builds NuGet package
- LibraryB.csproj → References LibraryA NuGet package
- AppProject.csproj → References LibraryB.csproj
- **Build fails** on AppProject

## Error Example

```
Error MSB3030: Could not copy the file 
"C:\Users\[user]\.nuget\packages\webview2loaderbuglibrary\1.0.0\lib\net8.0-windows10.0.22621\
WebView2LoaderBug.Lib\runtimes\win-arm64\native\WebView2Loader.dll" 
because it was not found.
```

**Repro:**
1. Create LibraryA (class library) → Add `<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.6.x" />`
2. Build LibraryA as NuGet package
3. Create LibraryB (wrapper library) → Add NuGet reference to LibraryA package
4. Build LibraryB → **Error: WebView2Loader.dll not found**

## Root Cause

**WindowsAppSDK 1.6** includes WebView2 as a **transitive dependency**. When packaged as a NuGet, the generated `.pri` file incorrectly treats `WebView2Loader.dll` as a **resource** instead of a native binary. This causes:
- Incorrect NuGet package structure
- Missing runtime files in transitive packages
- Build system can't find WebView2Loader.dll at expected path

**Why it happens:**
- PRI generation scans `runtimes\*\native\` folders
- WebView2Loader.dll is indexed as resource
- NuGet package metadata points to wrong location
- Consuming projects can't resolve the file

## Solutions

### ✅ Solution 1: Upgrade WebView2 to 1.0.2903.40+ (Recommended - Fixed in WebView2)

**Status:** ✅ **Fix released Dec 2024** in WebView2 package

**Steps:**
1. In **LibraryA.csproj** (the library that packages WindowsAppSDK), add explicit WebView2 reference:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.6.x" />
  <!-- Add this to fix the issue -->
  <PackageReference Include="Microsoft.Web.WebView2" Version="1.0.2903.40" />
</ItemGroup>
```

2. **Clear NuGet cache** (critical step):
```powershell
# Delete the cached buggy version
Remove-Item -Recurse "$env:USERPROFILE\.nuget\packages\[your-library-name]"

# Or clear all NuGet cache
dotnet nuget locals all --clear
```

3. Rebuild LibraryA and republish NuGet package
4. Update LibraryB to use new package version

**Why this works:**
- WebView2 1.0.2903.40+ fixes the PRI indexing issue
- Native binaries no longer treated as resources
- NuGet package structure is correct

### ✅ Solution 2: Upgrade to WindowsAppSDK 1.7+ (Fixed in WinAppSDK)

**Status:** ✅ **Fixed in WinAppSDK 1.7**

If you can upgrade to WindowsAppSDK 1.7 or later, the issue is resolved:

```xml
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.7.250606001" />
```

**Note:** This requires updating **all projects** in your solution to 1.7+.

### Workaround (if stuck on 1.6): Unpackaged Deployment

If you cannot upgrade and need to ship now:

```xml
<!-- In LibraryA.csproj -->
<PropertyGroup>
  <WindowsPackageType>None</WindowsPackageType>
  <!-- This avoids MSIX packaging that triggers the bug -->
</PropertyGroup>
```

**Limitation:** This only works for unpackaged deployment scenarios.

## Verification

After applying fix, verify the NuGet package structure:

```powershell
# Extract your NuGet package
Expand-Archive -Path LibraryA.1.0.0.nupkg -DestinationPath ExtractedPackage

# Check that WebView2Loader.dll is in correct location
Test-Path "ExtractedPackage\runtimes\win-x64\native\WebView2Loader.dll"
# Should return True

# Ensure it's NOT listed in the .pri file
makepri.exe dump /if "ExtractedPackage\lib\*.pri" /of "pri_contents.xml"
# Search pri_contents.xml - WebView2Loader should NOT appear as resource
```

## Prevention

**For library authors who package WindowsAppSDK:**

1. **Always use WebView2 1.0.2903.40+** (even if WindowsAppSDK brings older version)
2. **Test NuGet package structure** before publishing (use verification above)
3. **Document minimum WebView2 version** in package README
4. **Clear NuGet cache** when rebuilding after package changes

**For consumers of affected libraries:**

1. Check if library uses WindowsAppSDK 1.6.x
2. If build fails with WebView2Loader error, ask library author to upgrade WebView2
3. As temporary workaround, add explicit WebView2 reference to your app project

## Version Information

- **Affected versions:** WindowsAppSDK 1.6.x with WebView2 < 1.0.2903.40
- **Fixed in:** 
  - ✅ WebView2 1.0.2903.40+ (Dec 2024)
  - ✅ WindowsAppSDK 1.7.0+ (includes fix)
- **Recommended minimum:** WindowsAppSDK 1.7+ OR WindowsAppSDK 1.6 + WebView2 1.0.2903.40+
- **Workaround required for 1.6:** Yes - add explicit WebView2 package reference
