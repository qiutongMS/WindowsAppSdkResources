# Access Denied / Unauthorized Access Errors with WCR APIs

**Error Codes:** `0x80070005`, `E_ACCESSDENIED`, `System.UnauthorizedAccessException`  
**Affected Area:** WCR APIs (Phi Silica, OCR, Image Description)  
**Common Platforms:** All platforms, both packaged and unpackaged scenarios

---

## Symptom Overview

When attempting to use Windows Copilot Runtime (WCR) APIs such as Phi Silica, OCR, or Image Description, you encounter "Access Denied" or "Unauthorized Access" exceptions. These errors can occur at different stages: during initialization, when calling `GetReadyState()`, `MakeAvailableAsync()`, or when creating model instances.

**You might see:**
- Error message: "Access is denied" or "Denied access"
- Exception type: `System.UnauthorizedAccessException`
- Generic COMException with message: "Unspecified error\nNot declared by app"
- HRESULT: 0x80070005

---

## Related Issues

This troubleshooting guide consolidates multiple related reports:
- [#5594](https://github.com/microsoft/WindowsAppSDK/issues/5594) - Generic COMException "Not declared by app" when missing systemAIModels capability
- [#5560](https://github.com/microsoft/WindowsAppSDK/issues/5560) - MSIX identity now required for WCR APIs (by design)
- [#5451](https://github.com/microsoft/WindowsAppSDK/issues/5451) - UnauthorizedAccessException in unpackaged scenarios (Experimental 2+)
- [#5115](https://github.com/microsoft/WindowsAppSDK/issues/5115) - "Access Denied" with console app using WCR
- [#5185](https://github.com/microsoft/WindowsAppSDK/issues/5185) - MakeAvailableAsync() Access Denied

---

## Quick Diagnosis

Run through these checks to identify your specific scenario:

1. **Check if you're using MSIX packaging**
   ```powershell
   # In your project, check if you have Package.appxmanifest
   Test-Path "Package.appxmanifest"
   ```
   → If FALSE (unpackaged), see [Scenario 1: Unpackaged Application](#scenario-1-unpackaged-application-not-supported)

2. **Check for systemAIModels capability in manifest**
   ```xml
   <!-- Look for this in Package.appxmanifest -->
   <systemai:Capability Name="systemAIModels"/>
   ```
   → If MISSING, see [Scenario 2: Missing systemAIModels Capability](#scenario-2-missing-systemaimodels-capability)

3. **Check self-contained deployment setting**
   ```xml
   <!-- In .csproj file -->
   <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
   ```
   → If missing in console/unpackaged app, see [Scenario 3: Self-Contained Configuration](#scenario-3-self-contained-configuration-issue)

---

## Common Scenarios & Solutions

### Scenario 1: Unpackaged Application (Not Supported)

**Root Cause:** Starting with Windows App SDK 1.7 Experimental 2+, WCR APIs **require MSIX identity**. This is a permanent architectural change, not a bug. Unpackaged applications can no longer access WCR features.

**Related Issue(s):** [#5560](https://github.com/microsoft/WindowsAppSDK/issues/5560), [#5451](https://github.com/microsoft/WindowsAppSDK/issues/5451)

**Fix:** Convert your application to use MSIX packaging

1. **Add Windows Application Packaging Project to your solution**
   - In Visual Studio: Add New Project → Windows Application Packaging Project
   - Add your existing project as a dependency

2. **Or enable single-project MSIX** (for .NET apps)
   ```xml
   <!-- Add to your .csproj -->
   <PropertyGroup>
     <WindowsPackageType>MSIX</WindowsPackageType>
     <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
   </PropertyGroup>
   ```

3. **Create Package.appxmanifest** if not auto-generated
   - Right-click project → Add → New Item → Application Manifest File

**Verification:**
```powershell
# After packaging, verify identity
(Get-AppxPackage -Name "YourAppName").PackageFullName
# Should return your package identity
```

---

### Scenario 2: Missing systemAIModels Capability

**Root Cause:** WCR APIs require the `systemAIModels` capability to be declared in the app manifest. Without this, you'll get a generic COM error or "Not declared by app" message.

**Related Issue(s):** [#5594](https://github.com/microsoft/WindowsAppSDK/issues/5594)

**Fix:** Add the capability to your manifest

1. **Open Package.appxmanifest** in a text editor

2. **Add namespace declaration** at the top:
   ```xml
   <Package
     xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
     xmlns:mp="http://schemas.microsoft.com/appx/2014/phone/manifest"
     xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
     xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
     xmlns:systemai="http://schemas.microsoft.com/appx/manifest/systemai/windows10"
     IgnorableNamespaces="uap rescap systemai">
   ```

3. **Add capability** in the `<Capabilities>` section:
   ```xml
   <Capabilities>
     <rescap:Capability Name="runFullTrust" />
     <systemai:Capability Name="systemAIModels"/>
   </Capabilities>
   ```

**Verification:**
```csharp
// This should now work without exception
var readyState = LanguageModel.GetReadyState();
```

---

### Scenario 3: Self-Contained Configuration Issue

**Root Cause:** Console apps or certain project configurations need explicit self-contained deployment settings to properly locate WCR runtime components.

**Related Issue(s):** [#5115](https://github.com/microsoft/WindowsAppSDK/issues/5115)

**Fix:** Update project configuration

1. **Add to your .csproj file:**
   ```xml
   <PropertyGroup>
     <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
   </PropertyGroup>
   ```

2. **Ensure you're using the full metapackage:**
   ```xml
   <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.7.250127003-experimental3" />
   ```
   (or latest stable version)

3. **Clean and rebuild:**
   ```powershell
   dotnet clean
   dotnet build -c Release
   ```

**Verification:**
```powershell
# Check that WCR DLLs are in output directory
Get-ChildItem "bin\Release\net8.0-windows10.0.22621.0\win-x64" -Filter "*Windows.AI*"
```

---

## Additional Context

### Important Architectural Changes

- **Experimental 1**: WCR APIs worked in unpackaged scenarios
- **Experimental 2+**: MSIX identity became mandatory
- **This is permanent**: Microsoft confirmed this is by design for privacy/security requirements

### Why MSIX is Required

WCR APIs access system-level AI models that require:
1. User consent through app capabilities
2. Privacy controls in Windows Settings
3. AppContainer isolation for security

These features are only available to apps with MSIX identity.

### Migration Path

If you have an existing unpackaged WCR app:
1. Package it with MSIX (preferred)
2. Or consider using cloud-based AI APIs as alternative
3. Note: Future WCR features will continue to require MSIX

---

## References

- [Issue #5594: Generic COMException "Not declared by app"](https://github.com/microsoft/WindowsAppSDK/issues/5594)
- [Issue #5560: Windows App SDK violates MSIX-optional commitment](https://github.com/microsoft/WindowsAppSDK/issues/5560)
- [Issue #5451: UnauthorizedAccessException in unpackaged scenarios](https://github.com/microsoft/WindowsAppSDK/issues/5451)
- [Issue #5115: Access Denied with latest Phi Silica capabilities](https://github.com/microsoft/WindowsAppSDK/issues/5115)
- [Issue #5185: MakeAvailableAsync Access Denied](https://github.com/microsoft/WindowsAppSDK/issues/5185)
- [Official Documentation: Get Started with Windows AI APIs](https://learn.microsoft.com/windows/ai/apis/get-started)
- [MSIX Packaging Documentation](https://learn.microsoft.com/windows/msix/)

---

**Last Updated:** 2025-12-25  
**Confidence:** 0.92
