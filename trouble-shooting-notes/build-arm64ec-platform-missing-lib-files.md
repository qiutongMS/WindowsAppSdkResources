# ARM64EC Build Fails - Missing Lib Files

**Keywords:** arm64ec build-error missing-lib-files platform-target linker-error lib-not-found  
**Last Updated:** 2026-01-12  
**Status:** ✅ Fixed in Windows App SDK 1.8.251003001+

## Related Issues

- microsoft/WindowsAppSDK#4492 - Lack Support for ARM64EC Build (CLOSED - Fixed Nov 2025)

## Symptom

Building a WinUI 3 project with **ARM64EC** platform target fails with linker errors about missing `.lib` files.

**Error:** Cannot find required library files for ARM64EC architecture.

## Error Example

**Project configuration:**
```xml
<PropertyGroup>
  <Platform>ARM64EC</Platform>
  <!-- Or in VS: Configuration Manager → Platform → ARM64EC -->
</PropertyGroup>
```

**Build output:**
```
Error LNK1104: cannot open file 'Microsoft.UI.Xaml.lib'
Error: Missing lib files for ARM64EC platform

Build failed.
```

**Platforms that work:**
- ✅ x64
- ✅ x86  
- ✅ ARM64
- ❌ ARM64EC ← Fails

## Root Cause

**Windows App SDK versions prior to 1.8** did not include ARM64EC library files in the NuGet package.

**What is ARM64EC:**
- ARM64 Emulation Compatible
- Allows mixing ARM64 and x64 code in same process
- Introduced in Windows 11 for better x64 emulation on ARM

**Why earlier SDKs lacked support:**
- ARM64EC is relatively new (Windows 11 feature)
- WinAppSDK needed to build and package ARM64EC binaries
- Requires Windows SDK 10.0.22621.0+ tooling

## Solutions

### ✅ Solution: Upgrade to Windows App SDK 1.8+

**Status:** ✅ **FIXED in WinAppSDK 1.8.251003001+ (Nov 2025)**

**Steps:**

1. **Update Windows App SDK package:**

```xml
<!-- In .csproj, upgrade to 1.8 or later -->
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.8.251003001" />

<!-- Or latest stable -->
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.8.*" />
```

2. **Ensure Windows SDK is up to date:**

Requires **Windows SDK 10.0.22621.0** or later for ARM64EC support.

**Check installed version:**
```powershell
# Check installed Windows SDKs
Get-ChildItem "HKLM:\SOFTWARE\Microsoft\Windows Kits\Installed Roots"
```

**Install if needed:**
- Visual Studio Installer → Modify → Individual Components → "Windows 11 SDK (10.0.22621.0)"

3. **Rebuild for ARM64EC:**

```powershell
dotnet restore
dotnet build -c Release -p:Platform=ARM64EC
```

**Verification:**
```powershell
# Check output contains ARM64EC binaries
Test-Path "bin\ARM64EC\Release\net8.0-windows10.0.22621.0\MyApp.exe"
# Should return True
```

### Workaround (if stuck on older SDK): Use ARM64 Instead

If you cannot upgrade to 1.8+ immediately:

```xml
<!-- Use regular ARM64 instead of ARM64EC -->
<PropertyGroup>
  <Platform>ARM64</Platform>
</PropertyGroup>
```

**Limitation:**
- Cannot mix x64 code in same process
- Most apps don't need ARM64EC specifically

## Verification

After upgrading to 1.8+, verify ARM64EC support:

```powershell
# Build for ARM64EC
dotnet build -c Release -p:Platform=ARM64EC

# Check binary architecture
dumpbin /headers "bin\ARM64EC\Release\net8.0-windows10.0.22621.0\MyApp.exe" | Select-String "machine"

# Expected output:
# ARM64EC machine (AA64)
```

**Test matrix:**

| Platform | SDK 1.6 | SDK 1.7 | SDK 1.8+ |
|----------|---------|---------|----------|
| x64      | ✅      | ✅      | ✅       |
| x86      | ✅      | ✅      | ✅       |
| ARM64    | ✅      | ✅      | ✅       |
| ARM64EC  | ❌      | ❌      | ✅       |

## Prevention

**For new projects targeting ARM devices:**

1. **Use latest SDK** - Windows App SDK 1.8+ includes ARM64EC support
2. **Target ARM64 by default** - Unless you specifically need EC (emulation compatibility)
3. **Test on ARM64 devices** - Verify builds work on actual ARM hardware
4. **Document platform requirements** - Clarify which architectures are supported

**When do you need ARM64EC:**

✅ **Use ARM64EC if:**
- Mixing x64 native DLLs with ARM code
- Using x64 COM components in ARM app
- Need x64 plugin support

❌ **Don't need ARM64EC if:**
- Pure ARM64 app (most common)
- All dependencies support ARM64 natively
- Only targeting ARM devices (no x64 emulation needed)

## Additional Context

**Microsoft confirmation (Nov 2025):**

> "Thanks for reporting this. ARM64EC builds of WinUI3 Apps have been working for some time. I confirmed it works in the latest 1.8 Stable release."

**Timeline:**
- Prior to 1.8: ARM64EC not supported
- 1.8.251003001 (Nov 2025): ARM64EC fully supported
- Issue closed as "completed"

## Version Information

- **Affected versions:** Windows App SDK < 1.8
- **Fixed in:** ✅ Windows App SDK 1.8.251003001+ (Nov 2025)
- **Requires:** Windows SDK 10.0.22621.0+ for ARM64EC tooling
- **Workaround for older SDKs:** Use ARM64 platform instead
- **Status:** Issue closed - upgrade to 1.8+ to resolve
