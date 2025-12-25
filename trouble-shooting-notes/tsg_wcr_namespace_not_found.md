# "Cannot Find Microsoft.Windows.AI.Generative Namespace" Issues

**Error Codes:** Namespace not found, IntelliSense errors  
**Affected Area:** WCR APIs - SDK/Package discovery  
**Common Platforms:** All platforms, version-dependent

---

## Symptom Overview

When attempting to use WCR Phi Silica APIs, the namespace `Microsoft.Windows.AI.Generative` cannot be found by Visual Studio/VS Code. IntelliSense doesn't show the namespace, and compilation fails with "type or namespace not found" errors.

**You might see:**
- Error CS0246: The type or namespace name 'Microsoft.Windows.AI.Generative' could not be found
- IntelliSense shows no `Microsoft.Windows.AI.*` namespaces
- Documentation references APIs that don't exist in your project
- "Where is this API?" confusion

---

## Related Issues

This troubleshooting guide consolidates multiple related reports:
- [#4816](https://github.com/microsoft/WindowsAppSDK/issues/4816) - Where is Microsoft.Windows.AI.Generative?
- [#4902](https://github.com/microsoft/WindowsAppSDK/issues/4902) - Cannot find the namespace
- [#5181](https://github.com/microsoft/WindowsAppSDK/issues/5181) - Can't find namespace even with correct NuGet
- [#5028](https://github.com/microsoft/WindowsAppSDK/issues/5028) - Phi Silica missing in Experimental 2
- [#5127](https://github.com/microsoft/WindowsAppSDK/issues/5127) - Unable to use Microsoft.Windows.Vision/Imaging

---

## Quick Diagnosis

Run through these checks to identify your specific scenario:

1. **Check Windows App SDK version installed**
   ```xml
   <!-- In .csproj, check PackageReference -->
   <PackageReference Include="Microsoft.WindowsAppSDK" Version="?" />
   ```
   → If version is **< 1.7-experimental3**, see [Scenario 1: SDK Version Too Old](#scenario-1-sdk-version-too-old)

2. **Verify namespace actually exists in your SDK version**
   ```powershell
   # Check NuGet package contents
   $pkgPath = "$env:USERPROFILE\.nuget\packages\microsoft.windowsappsdk\1.7.250127003-experimental3"
   Get-ChildItem "$pkgPath\lib\**\*.dll" -Recurse | Select-String "AI.Generative"
   ```
   → If NO matches found, see [Scenario 1](#scenario-1-sdk-version-too-old)

3. **Check if you're using the metapackage vs split packages**
   ```xml
   <!-- Which are you using? -->
   <PackageReference Include="Microsoft.WindowsAppSDK" />  <!-- Metapackage -->
   <!-- OR -->
   <PackageReference Include="Microsoft.WindowsAppSDK.AI" />  <!-- Split package -->
   ```
   → If using split packages incorrectly, see [Scenario 2: Wrong Package Reference](#scenario-2-wrong-package-reference)

---

## Common Scenarios & Solutions

### Scenario 1: SDK Version Too Old

**Root Cause:** The `Microsoft.Windows.AI.Generative` namespace and Phi Silica APIs were introduced in **Windows App SDK 1.7 Experimental 3** (released February 2025). Earlier versions (1.6, 1.5, stable 1.7, 1.7-exp1, 1.7-exp2) do NOT contain these APIs.

**Related Issue(s):** [#4816](https://github.com/microsoft/WindowsAppSDK/issues/4816), [#4902](https://github.com/microsoft/WindowsAppSDK/issues/4902), [#5028](https://github.com/microsoft/WindowsAppSDK/issues/5028)

**Fix:** Upgrade to Windows App SDK 1.7-experimental3 or later

1. **Update PackageReference in .csproj:**
   ```xml
   <PackageReference Include="Microsoft.WindowsAppSDK" 
                     Version="1.7.250127003-experimental3" />
   
   <!-- Or latest experimental/stable with WCR support -->
   <!-- Check: https://github.com/microsoft/WindowsAppSDK/releases -->
   ```

2. **Enable experimental/preview NuGet feeds if needed:**
   
   Create or edit `nuget.config` in your solution folder:
   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <configuration>
     <packageSources>
       <clear />
       <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
       <add key="WindowsAppSDK-Experimental" 
            value="https://pkgs.dev.azure.com/microsoft/ProjectReunion/_packaging/WindowsAppSDK-Experimental/nuget/v3/index.json" />
     </packageSources>
   </configuration>
   ```

3. **Clean and restore packages:**
   ```powershell
   # Clear local NuGet cache
   dotnet nuget locals all --clear
   
   # Restore with new version
   dotnet restore
   
   # Rebuild
   dotnet build
   ```

4. **Verify the namespace is now available:**
   ```csharp
   using Microsoft.Windows.AI.Generative;
   
   // This should compile now
   var isAvailable = LanguageModel.IsAvailable();
   ```

**Verification:**
```powershell
# Check installed package version
dotnet list package | Select-String "WindowsAppSDK"

# Should show 1.7.250127003-experimental3 or later
```

---

### Scenario 2: Wrong Package Reference

**Root Cause:** The WCR APIs might be in a separate NuGet package depending on SDK version. Using only `Microsoft.WindowsAppSDK.AI` without the main package, or missing references, can cause namespace issues.

**Related Issue(s):** [#5181](https://github.com/microsoft/WindowsAppSDK/issues/5181)

**Fix:** Use correct package combination

**Recommended approach - Use metapackage:**
```xml
<!-- This includes everything (WinUI + WCR + Foundation) -->
<PackageReference Include="Microsoft.WindowsAppSDK" 
                  Version="1.7.250127003-experimental3" />
```

**Alternative - Use split packages correctly:**
```xml
<!-- If you need split packages for size optimization -->
<PackageReference Include="Microsoft.WindowsAppSDK.Foundation" 
                  Version="1.7.250127003-experimental3" />
<PackageReference Include="Microsoft.WindowsAppSDK.AI" 
                  Version="1.7.250127003-experimental3" />

<!-- Note: Versions MUST match exactly -->
```

**Verification:**
```powershell
# List all WindowsAppSDK packages in project
dotnet list package | Select-String "WindowsAppSDK"

# Verify all versions match
```

---

### Scenario 3: Target Framework Mismatch

**Root Cause:** WCR APIs require a specific target framework that supports WinRT APIs. Using generic .NET targets may not expose the WinRT namespaces.

**Related Issue(s):** [#5127](https://github.com/microsoft/WindowsAppSDK/issues/5127)

**Fix:** Use correct target framework

1. **Update TargetFramework in .csproj:**
   ```xml
   <PropertyGroup>
     <!-- ❌ WRONG: Generic .NET target -->
     <!-- <TargetFramework>net8.0</TargetFramework> -->
     
     <!-- ✅ CORRECT: Windows-specific target -->
     <TargetFramework>net8.0-windows10.0.22621.0</TargetFramework>
     
     <!-- Optional: Set minimum supported OS version -->
     <TargetPlatformMinVersion>10.0.19041.0</TargetPlatformMinVersion>
   </PropertyGroup>
   ```

2. **For console apps, ensure Windows support is enabled:**
   ```xml
   <PropertyGroup>
     <TargetFramework>net8.0-windows10.0.22621.0</TargetFramework>
     <OutputType>Exe</OutputType>
     <UseWindowsForms>false</UseWindowsForms>
     <UseWPF>false</UseWPF>
     <!-- Enable WinRT support -->
     <EnableDefaultItems>true</EnableDefaultItems>
   </PropertyGroup>
   ```

3. **Add CsWinRT package if needed (rare):**
   ```xml
   <!-- Usually not needed if using correct SDK version, but some edge cases: -->
   <PackageReference Include="Microsoft.Windows.CsWinRT" 
                     Version="2.0.1" />
   ```

**Verification:**
```csharp
// Test WinRT projection works
using Microsoft.Windows.AI.Generative;
using Windows.Foundation;  // WinRT namespace

// These should both compile
var model = LanguageModel.IsAvailable();
IAsyncOperation<string> asyncOp = null;  // WinRT type
```

---

### Scenario 4: IDE/IntelliSense Cache Issues

**Root Cause:** Sometimes Visual Studio or VS Code caches old metadata and doesn't pick up newly installed packages, even after successful NuGet restore.

**Fix:** Clear IDE caches

**For Visual Studio:**
```
1. Close Visual Studio
2. Delete these folders:
   - .vs folder in solution directory
   - bin and obj folders in all projects
3. Open Visual Studio
4. Tools → Options → NuGet Package Manager → Clear All NuGet Cache(s)
5. Build → Clean Solution
6. Build → Rebuild Solution
```

**For VS Code:**
```powershell
# Close VS Code
# Clear OmniSharp cache
Remove-Item "$env:USERPROFILE\.omnisharp" -Recurse -Force -ErrorAction SilentlyContinue

# Clear .NET build cache
dotnet clean
Remove-Item "obj", "bin" -Recurse -Force

# Reopen VS Code and reload window (Ctrl+Shift+P → Reload Window)
```

**For Rider:**
```
1. File → Invalidate Caches
2. Select all options
3. Invalidate and Restart
```

**Verification:**
- Type `using Microsoft.Windows.` and IntelliSense should show `AI` namespace
- Hover over `LanguageModel` class - should show documentation

---

## Additional Context

### API Availability Timeline

| SDK Version | WCR APIs Available | Namespace |
|-------------|-------------------|-----------|
| 1.6.x and earlier | ❌ No | N/A |
| 1.7.0 (stable) | ❌ No | N/A |
| 1.7-exp1 | ❌ No | Announced but not released |
| 1.7-exp2 | ❌ No | Still in development |
| **1.7-exp3** | ✅ Yes | `Microsoft.Windows.AI.Generative` |
| 2.0-exp1+ | ✅ Yes | Same namespace |

### Documentation vs Reality

**Important:** Microsoft published API documentation for Phi Silica APIs **before they were available** in released SDK packages. This caused confusion as developers saw docs but couldn't find the APIs.

- Documentation published: ~November 2024 (Ignite announcement)
- Actual API availability: February 2025 (1.7-exp3)

### Namespace Structure

When properly configured, you should have access to:
```csharp
// Language model (Phi Silica)
using Microsoft.Windows.AI.Generative;

// OCR APIs
using Microsoft.Windows.Vision;

// Image AI APIs
using Microsoft.Windows.Imaging;

// Base types
using Microsoft.Windows.AI;
```

### Production Readiness

As of December 2025:
- **Experimental channel**: Contains WCR APIs, not for production
- **Stable channel**: Does not yet include WCR APIs
- **Timeline**: WCR APIs expected in stable channel in 2025/2026

---

## References

- [Issue #4816: Where is Microsoft.Windows.AI.Generative](https://github.com/microsoft/WindowsAppSDK/issues/4816)
- [Issue #4902: Cannot find namespace](https://github.com/microsoft/WindowsAppSDK/issues/4902)
- [Issue #5181: Can't find namespace](https://github.com/microsoft/WindowsAppSDK/issues/5181)
- [Issue #5028: Phi Silica missing in Exp 2](https://github.com/microsoft/WindowsAppSDK/issues/5028)
- [Issue #5127: Unable to use Vision/Imaging namespaces](https://github.com/microsoft/WindowsAppSDK/issues/5127)
- [Windows App SDK Releases](https://github.com/microsoft/WindowsAppSDK/releases)
- [Experimental Channel Documentation](https://learn.microsoft.com/windows/apps/windows-app-sdk/experimental-channel)

---

**Last Updated:** 2025-12-25  
**Confidence:** 0.90
