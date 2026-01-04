# WCR Namespace Not Found - Wrong NuGet Package Reference

**Error Code:** CS0246 (Namespace not found)  
**Affected Area:** WCR APIs - NuGet package configuration  
**Common Platforms:** All platforms

---

## Symptom Overview

You have the correct SDK version (1.7-exp3+) but still can't find WCR namespaces because you're using wrong or missing package references.

**You might see:**
- Some Windows App SDK features work, but WCR namespaces are missing
- Using split packages (`Microsoft.WindowsAppSDK.AI`) without other required packages
- Version mismatches between multiple WindowsAppSDK packages
- IntelliSense shows some `Microsoft.Windows.*` but not `AI.Generative`

---

## Related Issues

- [#5181](https://github.com/microsoft/WindowsAppSDK/issues/5181) - Can't find namespace even with correct NuGet

---

## Root Cause

The WCR APIs may be distributed across multiple NuGet packages depending on how you reference the SDK. Using only partial packages or mismatched versions causes namespace resolution issues.

---

## Solution

### Recommended: Use Metapackage (Simplest)

The metapackage includes everything:

```xml
<!-- Single package includes: WinUI + WCR + Foundation -->
<PackageReference Include="Microsoft.WindowsAppSDK" 
                  Version="1.7.250127003-experimental3" />

<!-- Remove any split package references: -->
<!-- DELETE these if present: -->
<!-- <PackageReference Include="Microsoft.WindowsAppSDK.Foundation" /> -->
<!-- <PackageReference Include="Microsoft.WindowsAppSDK.AI" /> -->
<!-- <PackageReference Include="Microsoft.WindowsAppSDK.WinUI" /> -->
```

After changing:
```powershell
dotnet clean
dotnet restore
dotnet build
```

---

### Alternative: Use Split Packages Correctly

If you need split packages for size optimization:

```xml
<!-- ALL split packages must have MATCHING versions -->
<PackageReference Include="Microsoft.WindowsAppSDK.Foundation" 
                  Version="1.7.250127003-experimental3" />
<PackageReference Include="Microsoft.WindowsAppSDK.AI" 
                  Version="1.7.250127003-experimental3" />
<!-- Add WinUI if needed for UI features -->
<PackageReference Include="Microsoft.WindowsAppSDK.WinUI" 
                  Version="1.7.250127003-experimental3" />
```

**Critical:** All versions MUST be identical.

---

## Verification

### Check All WindowsAppSDK Packages

```powershell
# List all WindowsAppSDK-related packages
dotnet list package | Select-String "WindowsAppSDK"

# Verify:
# 1. All versions match exactly
# 2. No duplicate/conflicting packages
```

Expected output (metapackage):
```
Microsoft.WindowsAppSDK  1.7.250127003-experimental3
```

Expected output (split packages):
```
Microsoft.WindowsAppSDK.Foundation  1.7.250127003-experimental3
Microsoft.WindowsAppSDK.AI          1.7.250127003-experimental3
Microsoft.WindowsAppSDK.WinUI       1.7.250127003-experimental3
```

### Test Namespace Resolution

```csharp
using Microsoft.Windows.AI.Generative;
using Microsoft.Windows.Vision;
using Microsoft.Windows.Imaging;

// All should compile
var langModel = LanguageModel.IsAvailable();
var ocrState = TextRecognizer.GetReadyState();
```

---

## Common Mistakes

### ❌ Mistake 1: Only Including AI Package

```xml
<!-- WRONG: Missing Foundation package -->
<PackageReference Include="Microsoft.WindowsAppSDK.AI" 
                  Version="1.7.250127003-experimental3" />
```

**Fix:** Add Foundation or use metapackage.

### ❌ Mistake 2: Version Mismatch

```xml
<!-- WRONG: Different versions -->
<PackageReference Include="Microsoft.WindowsAppSDK.Foundation" 
                  Version="1.6.240923002" />
<PackageReference Include="Microsoft.WindowsAppSDK.AI" 
                  Version="1.7.250127003-experimental3" />
```

**Fix:** Align all versions.

### ❌ Mistake 3: Both Metapackage and Split Packages

```xml
<!-- WRONG: Mixing both -->
<PackageReference Include="Microsoft.WindowsAppSDK" 
                  Version="1.7.250127003-experimental3" />
<PackageReference Include="Microsoft.WindowsAppSDK.AI" 
                  Version="1.7.250127003-experimental3" />
```

**Fix:** Choose one approach, remove the other.

---

## Cleanup Steps

If packages are inconsistent:

```powershell
# 1. Clean all build outputs
dotnet clean
Remove-Item bin, obj -Recurse -Force

# 2. Clear NuGet cache
dotnet nuget locals all --clear

# 3. Edit .csproj - use metapackage only
# <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.7.250127003-experimental3" />

# 4. Restore
dotnet restore

# 5. Rebuild
dotnet build
```

---

## References

- [Issue #5181: Can't find namespace](https://github.com/microsoft/WindowsAppSDK/issues/5181)
- [Windows App SDK Package Structure](https://learn.microsoft.com/windows/apps/windows-app-sdk/deploy-packaged-apps)

---

**Last Updated:** 2026-01-04  
**Confidence:** 0.88

## Changelog

**2026-01-04:**
- Split from namespace_not_found.md
- Added common mistakes section
