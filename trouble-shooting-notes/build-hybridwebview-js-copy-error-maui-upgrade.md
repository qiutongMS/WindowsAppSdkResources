# Build Fails After 1.7→1.8 Upgrade - "Could not copy HybridWebView.js"

**Keywords:** hybridwebview maui upgrade-error copy-error msb3030 sdk-1.8 maui-controls  
**Last Updated:** 2026-01-12  
**Status:** Open (Root Cause in MAUI, Fix Available)

## Related Issues

- microsoft/WindowsAppSDK#6032 - "Could not copy HybridWebView.js" when upgrading to 1.8 (Nov 2025)
- dotnet/maui#32683 - Related MAUI issue
- dotnet/maui#32783 - PR fix for MAUI package

## Symptom

Upgrading from **Windows App SDK 1.7 to 1.8** causes build failure in projects using **.NET MAUI controls** (specifically HybridWebView).

**Error:** `MSB3030: Could not copy file "HybridWebView.js" because it was not found`

**Trigger:**
- Project uses Microsoft.Maui.Controls package
- Upgrade WindowsAppSDK from 1.7.x → 1.8.x
- Build fails immediately after upgrade

## Error Example

```
Error MSB3030: Could not copy the file "HybridWebView.js" because it was not found.

1>C:\Program Files\Microsoft Visual Studio\2022\...\Microsoft.Common.CurrentVersion.targets(5321,5): 
error MSB3030: Could not copy the file 
"...\packages\microsoft.maui.controls\9.0.71\buildTransitive\net8.0-windows10.0.19041\HybridWebView.js"
because it was not found.
```

**Project structure:**
```xml
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.8.x" />
<PackageReference Include="Microsoft.Maui.Controls" Version="9.0.71" />
<!-- Conflict between these packages -->
```

## Root Cause

**This is a MAUI package issue, not WindowsAppSDK bug.**

**What happens:**
1. MAUI package includes `HybridWebView.js` as **Content** with `<CopyToOutputDirectory>`
2. WindowsAppSDK 1.8 also includes HybridWebView functionality
3. MSBuild tries to copy file from MAUI package location
4. File doesn't exist at expected path → build fails

**Why it worked in 1.7:**
- WindowsAppSDK 1.7 didn't include HybridWebView.js
- MAUI package provided it without conflict
- 1.8 changed how HybridWebView resources are handled

**Root cause identified by Microsoft (Nov 2025):**
- MAUI package incorrectly includes file as Content
- Should be embedded resource instead
- Fix available in MAUI PR #32783

## Solutions

### ✅ Solution 1: Wait for MAUI Package Update (Recommended)

**Status:** Fix merged into MAUI repository (Dec 2025)

**Wait for:**
- Microsoft.Maui.Controls package update (version > 9.0.71)
- Fix is in dotnet/maui PR #32783

**When available, update:**
```xml
<PackageReference Include="Microsoft.Maui.Controls" Version="9.0.80" />
<!-- Or whatever version includes the fix -->
```

### ✅ Solution 2: Clear NuGet Cache and Rebuild

**Temporary workaround (may work for some users):**

```powershell
# Close Visual Studio

# Clear NuGet cache
dotnet nuget locals all --clear

# Delete bin/obj
Remove-Item -Recurse -Force bin, obj

# Restore and rebuild
dotnet restore
dotnet build -c Release
```

**Success rate:** ~30% (works if cache issue, not fundamental conflict)

### ✅ Solution 3: Downgrade to 1.7 Temporarily

If you need to ship and can't wait for MAUI fix:

```xml
<!-- Temporarily revert to 1.7 -->
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.7.250606001" />
<PackageReference Include="Microsoft.Maui.Controls" Version="9.0.71" />
```

**When MAUI package is fixed:**
- Upgrade both packages together
- Test before production deployment

### Workaround: Remove MAUI HybridWebView (if not needed)

If you're not actually using HybridWebView:

```csharp
// Remove any HybridWebView usage from XAML/code
// <maui:HybridWebView /> ← Delete this

// Use regular WebView2 instead
<WebView2 Source="https://example.com" />
```

Then update to 1.8:
```xml
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.8.x" />
```

## Verification

Check if you're affected:

```powershell
# Check if project uses both packages
$csproj = Get-Content "YourProject.csproj" -Raw

if ($csproj -match "Microsoft.Maui.Controls" -and $csproj -match "WindowsAppSDK") {
    Write-Warning "Project uses both MAUI and WinAppSDK - vulnerable to HybridWebView.js issue"
    
    # Check MAUI version
    if ($csproj -match 'Microsoft.Maui.Controls.*Version="([^"]+)"') {
        $mauiVersion = $matches[1]
        Write-Host "MAUI version: $mauiVersion"
        
        if ([version]$mauiVersion -le [version]"9.0.71") {
            Write-Error "MAUI version needs update after fix is released"
        }
    }
}
```

**After applying fix:**
```powershell
# Build should succeed
dotnet build -c Release

# No MSB3030 errors about HybridWebView.js
```

## Prevention

**For projects using both MAUI and WinAppSDK:**

1. **Monitor MAUI releases** - Watch for fix in Microsoft.Maui.Controls package
2. **Test upgrades in branch** - Don't upgrade WinAppSDK to 1.8 on main branch until MAUI is compatible
3. **Pin package versions** - Use exact versions in .csproj to control upgrades
4. **Document dependencies** - Note MAUI + WinAppSDK version compatibility in README

**Compatibility matrix (current):**

| WinAppSDK | MAUI Controls | HybridWebView | Status |
|-----------|---------------|---------------|--------|
| 1.7.x     | 9.0.71        | ✅            | Works  |
| 1.8.x     | 9.0.71        | ❌            | Fails  |
| 1.8.x     | 9.0.80+ (TBD) | ✅ (expected) | Fix pending |

## Related Issues

**Why this affects HybridWebView specifically:**

- HybridWebView is a MAUI component that uses WebView2
- WindowsAppSDK 1.8 changed WebView2 resource handling
- MAUI package didn't update to match new structure
- Conflict only appears when both packages try to provide same file

**Other MAUI + WinAppSDK issues:**
- Watch dotnet/maui repository for compatibility notes
- WindowsAppSDK release notes mention MAUI compatibility

## Reporting

**If you encounter this issue:**

1. ✅ **Already reported** to both teams:
   - microsoft/WindowsAppSDK#6032
   - dotnet/maui#32683

2. ✅ **Fix PR merged:** dotnet/maui#32783

3. **What you can do:**
   - Wait for next MAUI package release
   - Use workaround (Solution 2 or 3) temporarily
   - Monitor MAUI releases for version with fix

## Version Information

- **Affected versions:** 
  - Windows App SDK 1.8.x
  - Microsoft.Maui.Controls ≤ 9.0.71
- **Root cause:** MAUI package issue (not WinAppSDK)
- **Fix status:** ✅ Merged into MAUI (PR #32783, Dec 2025)
- **Expected in:** Microsoft.Maui.Controls 9.0.80+ (TBD)
- **Workaround:** Revert to WinAppSDK 1.7 OR wait for MAUI update
- **Status:** Open (waiting for MAUI package release)
