# Build Error: "Could not copy WebView2Loader.dll" - MSB3030

**Keywords:** MSB3030, WebView2Loader.dll, secondary NuGet package, Could not copy, runtimes\win-arm64\native, WindowsAppSDK 1.6, NuGet transitive dependency

**Error Example:**
```
Error MSB3030: Could not copy "WebView2LoaderBug.Lib\runtimes\win-arm64\native\WebView2Loader.dll" 
because it was not found.

Error MSB3030: Could not copy the file "packages\Microsoft.Web.WebView2.1.0.xxxx\runtimes\win-arm64
\native\WebView2Loader.dll" to the destination file "bin\Release\runtimes\win-arm64\native\
WebView2Loader.dll", because it was not found.
```

**Occurs when:**
- Building a project that references a NuGet package
- That NuGet package itself references WindowsAppSDK 1.6.x
- Building for ARM64 platform (most common), but can affect other platforms

---

## Quick Match

**You're seeing this if:**
- Error contains `WebView2Loader.dll` and `Could not copy`
- Using WindowsAppSDK 1.6.x (via direct or transitive dependency)
- Building consuming application that references a library NuGet package
- Error appeared after upgrading to WindowsAppSDK 1.6 from 1.5

→ ✅ **Fixed in Microsoft.Web.WebView2 1.0.2903.40+ (December 2024)**

---

## Related Issues

- [#4807](https://github.com/microsoft/WindowsAppSDK/issues/4807) - Failure to build when WindowsAppSDK 1.6 referenced through secondary NuGet package ✅ Closed

---

## Root Cause

**Why it happens:**

WindowsAppSDK 1.6 introduced a packaging change that incorrectly treats `WebView2Loader.dll` as a resource file to be embedded in `.pri` (Package Resource Index) files when the SDK is consumed as a transitive NuGet dependency.

**Build chain breakdown:**
1. You create `MyApp.exe` (main application)
2. `MyApp.exe` references `MyLibrary` (NuGet package)
3. `MyLibrary` references `WindowsAppSDK 1.6.x`
4. MSBuild tries to copy native `WebView2Loader.dll` from MyLibrary's package cache
5. ❌ File path is incorrect because it was bundled into .pri instead of being in `runtimes\` folder

**This was a regression from WindowsAppSDK 1.5** where WebView2Loader.dll was correctly copied as a native dependency.

**Affected versions:**
- WindowsAppSDK 1.6.0 through 1.6.240923002
- Fixed in Microsoft.Web.WebView2 1.0.2903.40+ (December 2024)

---

## Solutions

### ✅ Solution 1: Upgrade WebView2 (Recommended - Permanent Fix)

**Upgrade to fixed WebView2 version:**

```powershell
# Update Microsoft.Web.WebView2 to 1.0.2903.40 or later
dotnet add package Microsoft.Web.WebView2 --version 1.0.2903.40
```

**Or edit `.csproj` directly:**
```xml
<PackageReference Include="Microsoft.Web.WebView2" Version="1.0.2903.40" />
```

**Why this works:**
- WebView2 team fixed packaging to correctly place WebView2Loader.dll
- No longer incorrectly embedded in .pri file
- Works with WindowsAppSDK 1.6.x

**Verify fix:**
```powershell
# Clean build and verify no errors
dotnet clean
dotnet build -c Release
```

✅ **Status:** This is the permanent fix as of December 2024

---

### Solution 2: Add Explicit WebView2 Reference to Library

**If you maintain the library NuGet package:**

Add explicit `Microsoft.Web.WebView2` reference to your library's `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.6.240923002" />
  <!-- ADD THIS: -->
  <PackageReference Include="Microsoft.Web.WebView2" Version="1.0.2903.40" />
</ItemGroup>
```

**Why this works:**
- Ensures correct WebView2 version is referenced
- Overrides transitive dependency resolution

**When to use:** You control the library package source

---

### Solution 3: Clear NuGet Cache

**If upgrade doesn't immediately fix the issue:**

```powershell
# Clear NuGet package cache
dotnet nuget locals all --clear

# Or manually delete specific packages
Remove-Item "$env:USERPROFILE\.nuget\packages\microsoft.web.webview2" -Recurse -Force
Remove-Item "$env:USERPROFILE\.nuget\packages\microsoft.windowsappsdk" -Recurse -Force

# Restore packages
dotnet restore
dotnet build
```

**Why this helps:**
- Removes cached corrupted package state
- Forces download of fixed versions

---

### ⚠️ Workaround 4: Downgrade to WindowsAppSDK 1.5 (Not Recommended)

**Only if you cannot upgrade WebView2:**

```xml
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.5.240311000" />
```

**Cons:**
- Lose WindowsAppSDK 1.6+ features
- Not a long-term solution
- May have other compatibility issues

**When to use:** Temporary workaround while waiting for approval to update dependencies

---

## Prevention Best Practices

1. **Pin WebView2 version** - Always specify explicit WebView2 version ≥1.0.2903.40
2. **Update regularly** - Microsoft.Web.WebView2 receives regular fixes
3. **Test with Release builds** - ARM64 issues often only appear in Release configuration
4. **Document dependencies** - Make transitive WindowsAppSDK dependencies explicit

**Recommended `.csproj` pattern:**
```xml
<ItemGroup>
  <!-- Explicit versions prevent transitive dependency issues -->
  <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.6.240923002" />
  <PackageReference Include="Microsoft.Web.WebView2" Version="1.0.2903.40" />
</ItemGroup>
```

---

## Diagnostic Commands

**Check current WebView2 version:**
```powershell
# List all WebView2 versions in cache
Get-ChildItem "$env:USERPROFILE\.nuget\packages\microsoft.web.webview2"

# Check project's WebView2 version
dotnet list package | Select-String "WebView2"
```

**Check if WebView2Loader.dll exists in package:**
```powershell
# Navigate to package cache (adjust version)
$pkg = "$env:USERPROFILE\.nuget\packages\microsoft.web.webview2\1.0.xxxx"
Test-Path "$pkg\runtimes\win-arm64\native\WebView2Loader.dll"
# Should return: True
```

**Verify build output:**
```powershell
# After build, check if dll was copied
Test-Path "bin\Release\net8.0-windows10.0.19041.0\win-arm64\runtimes\win-arm64\native\WebView2Loader.dll"
```

---

## Resolution Status

✅ **Fixed** - December 2024
- Microsoft.Web.WebView2 1.0.2903.40+ resolves the issue
- No changes needed to WindowsAppSDK
- Upgrade path available for all affected projects

**Migration timeline:**
- **Immediate:** Upgrade WebView2 to 1.0.2903.40+
- **Legacy projects:** Add explicit WebView2 reference to library packages
- **CI/CD:** Clear NuGet cache in build pipelines

---

**Updated:** 2026-01-12 | **Confidence:** 0.98
