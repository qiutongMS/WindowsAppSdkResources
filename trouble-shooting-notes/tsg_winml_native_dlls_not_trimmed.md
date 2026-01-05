# WinML Native DLLs Not Trimmed in Self-Contained Builds

**Error Codes:** N/A (Build/Packaging Issue)  
**Affected Area:** Windows ML, Build & Packaging  
**Common Platforms:** Packaged (MSIX), Unpackaged, Self-Contained deployments

---

## Symptom Overview

When publishing a WinUI 3 application with `WindowsAppSDKSelfContained` and `PublishTrimmed` enabled, large unused native ML dependencies—specifically `onnxruntime.dll` (21 MB) and `DirectML.dll` (18 MB)—are included in the final output, significantly increasing application bundle size by approximately 39-40 MB despite not being utilized.

**You might see:**
- `onnxruntime.dll` (21 MB) in output folder
- `DirectML.dll` (18 MB) in output folder
- `Microsoft.Web.WebView2.Core.dll` (800 KB) in output folder
- Total unnecessary size increase of ~40 MB compared to SDK v1.7
- Trimming settings have no effect on these native DLLs

---

## Related Issues

This troubleshooting guide consolidates multiple related reports:
- [#6015](https://github.com/microsoft/WindowsAppSDK/issues/6015) - Huge native DLLs not trimmed in self-contained mode (closed)
- [#5969](https://github.com/microsoft/WindowsAppSDK/issues/5969) - Failed to get rid of ML libraries in app build
- [#5882](https://github.com/microsoft/WindowsAppSDK/issues/5882) - Microsoft.Windows.AI.MachineLearning does not support native AOT (closed)

---

## Quick Diagnosis

Run through these checks to identify your specific scenario:

1. **Check your Windows App SDK version**
   ```xml
   <!-- In .csproj file -->
   <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.8.*" />
   ```
   → If using 1.8+, this issue is present

2. **Check if using self-contained deployment**
   ```xml
   <!-- In .csproj file -->
   <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
   <PublishTrimmed>true</PublishTrimmed>
   ```
   → If TRUE, see scenarios below

3. **Verify which package references you're using**
   ```xml
   <PackageReference Include="Microsoft.WindowsAppSDK" />
   <!-- vs -->
   <PackageReference Include="Microsoft.WindowsAppSDK.Runtime" />
   <PackageReference Include="Microsoft.WindowsAppSDK.WinUI" />
   ```
   → The individual packages still include ML libs via Runtime package

---

## Common Scenarios & Solutions

### Scenario 1: Using Windows App SDK 1.8+ Metapackage

**Root Cause:** Windows App SDK 1.8 introduced Windows ML components that are bundled with the runtime, and native DLLs are not subject to managed code trimming.

**Related Issue(s):** [#6015](https://github.com/microsoft/WindowsAppSDK/issues/6015), [#5969](https://github.com/microsoft/WindowsAppSDK/issues/5969)

**Current Workaround:** Downgrade to SDK 1.7 if you don't need WinML features

1. **Downgrade to Windows App SDK 1.7**
   ```xml
   <!-- Change in .csproj -->
   <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.7.251014001" />
   ```

2. **Clean build artifacts**
   ```powershell
   # Delete bin and obj folders
   Remove-Item -Path "bin", "obj" -Recurse -Force
   ```

3. **Rebuild solution**
   ```powershell
   dotnet build -c Release
   ```

**Verification:**
```powershell
# Check output folder for ML DLLs
Get-ChildItem -Path "bin\Release" -Recurse -Filter "onnxruntime.dll"
Get-ChildItem -Path "bin\Release" -Recurse -Filter "DirectML.dll"
# Should return nothing if workaround successful
```

---

### Scenario 2: Using Individual Runtime Packages

**Root Cause:** Even when using individual packages (`Microsoft.WindowsAppSDK.Runtime` + `Microsoft.WindowsAppSDK.WinUI`), the ML libraries are included because they're part of the Runtime package dependency chain.

**Related Issue(s):** [#5969](https://github.com/microsoft/WindowsAppSDK/issues/5969)

**Current Status:** No official solution - trimming doesn't affect native DLLs

**Attempted Solutions (Don't Work):**
```xml
<!-- These don't help with native DLLs -->
<PublishTrimmed>true</PublishTrimmed>
<TrimMode>link</TrimMode>

<!-- Aggressive trimming attempt -->
<Target Name="ConfigureTrimming" BeforeTargets="PrepareForILLink">
  <ItemGroup>
    <ManagedAssemblyToLink Condition="'%(Filename)' == 'Microsoft.Windows.SDK.NET'">
      <IsTrimmable>true</IsTrimmable>
    </ManagedAssemblyToLink>
  </ItemGroup>
</Target>
```

**Best Workaround:** Use SDK 1.7 or wait for official fix

---

### Scenario 3: Native AOT Deployment with Managed ML DLLs

**Root Cause:** When using Native AOT compilation, managed ML assemblies (`Microsoft.Windows.AI.MachineLearning.dll`) remain in the output directory instead of being compiled to native code, causing deployment inconsistencies.

**Related Issue(s):** [#5882](https://github.com/microsoft/WindowsAppSDK/issues/5882)

**Current Status:** Closed - Microsoft.Windows.AI.MachineLearning now supports Native AOT in later releases

**Fix (if using latest SDK):** Update to latest Windows App SDK version

```xml
<!-- Ensure latest SDK version -->
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.8.251106002" />

<!-- Enable Native AOT -->
<PropertyGroup>
  <PublishAot>true</PublishAot>
</PropertyGroup>
```

**Verification:**
```powershell
# After AOT publish, check for managed DLLs
Get-ChildItem -Path "bin\Release\net8.0-windows10.*\win-x64\publish" -Filter "*.dll" | 
  Where-Object { (Get-Item $_.FullName).Length -gt 100KB }
# Managed ML DLLs should no longer appear
```

---

## Additional Context

### Why This Happens

1. **SDK 1.8 Architecture Change:** Windows ML was integrated as a core component in SDK 1.8, changing the dependency structure
2. **Native vs Managed Trimming:** `PublishTrimmed` only affects managed (.NET) assemblies, not native DLLs
3. **Runtime Package Dependencies:** The `Microsoft.WindowsAppSDK.Runtime` package includes ML components as mandatory dependencies

### Impact

- **Package Size:** 39-40 MB increase in MSIX packages or self-contained deployments
- **Download/Install Time:** Longer download and installation times for end users
- **Storage:** Increased storage requirements on user devices
- **Affects:** All deployment types (Packaged, Unpackaged, SingleFile)

### Official Tracking

These issues are being tracked by Microsoft. Monitor the related GitHub issues for updates on official fixes.

---

## Related Documentation

- [Windows App SDK Release Notes](https://learn.microsoft.com/windows/apps/windows-app-sdk/release-notes)
- [.NET Application Trimming](https://learn.microsoft.com/dotnet/core/deploying/trimming/trimming-options)
- [Native AOT Deployment](https://learn.microsoft.com/dotnet/core/deploying/native-aot)

---

**Last Updated:** January 5, 2026  
**Confidence Score:** 0.95  
**Status:** Active workaround available (downgrade to SDK 1.7)
