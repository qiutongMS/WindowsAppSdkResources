---
id: wcr_selfcontained_deployment_error
title: "Application Error with WindowsAppSDKSelfContained and WCR APIs"
area: Deployment
symptoms:
  - "Application Error when using WindowsAppSDKSelfContained property"
  - "WCR APIs throw exceptions with self-contained deployment"
  - "LanguageModel.IsAvailable() throws Application Error"
errorCodes:
  - "Application Error"
keywords:
  - "WindowsAppSDKSelfContained"
  - "self-contained"
  - "deployment"
  - "WCR"
  - "unpackaged"
  - "csproj"
  - "Phi Silica"
appliesTo:
  windows: ">=10.0.26120"
  winappsdk: "1.7.250127003-experimental3"
projectType: "unpackaged"
severity: "common"
lastVerified: "2025-12-23"
references:
  - "https://github.com/microsoft/WindowsAppSDK/issues/5280"
---

# Problem

When using the `WindowsAppSDKSelfContained` property set to `true` in an unpackaged WCR application's .csproj file, the application throws an "Application Error" exception when calling WCR APIs like `LanguageModel.IsAvailable()` or `TextRecognizer` APIs. The same code works fine when `WindowsAppSDKSelfContained` is not used or set to `false`.

# Quick Diagnosis

1. Check your .csproj file for `<WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>`
2. Verify you're calling WCR APIs (Phi Silica, TextRecognizer, etc.)
3. Confirm the deployment model is **unpackaged**
4. Look for generic "Application Error" exception

# Root Cause

The `WindowsAppSDKSelfContained` property attempts to bundle Windows App SDK runtime components directly with the application to avoid requiring the WindowsAppRuntimeInstall.exe dependency. However, in version 1.7.250127003-experimental3, this caused conflicts with WCR API initialization, likely due to incorrect DLL loading paths or missing runtime components specific to the self-contained deployment model.

This was a bug in the Windows App SDK experimental release that has since been fixed.

# Fix / Workaround

**Option 1: Remove WindowsAppSDKSelfContained property (Recommended)**

Remove or set to `false` in your .csproj:

```xml
<PropertyGroup>
  <!-- Remove this line: -->
  <!-- <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained> -->
</PropertyGroup>
```

With this approach, users will need to install Windows App Runtime separately, or your installer should bundle/deploy it.

**Option 2: Upgrade to fixed Windows App SDK version**

This issue was reported as fixed in later releases. Upgrade to Windows App SDK 1.8 or later experimental/stable releases:

```xml
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.8.250515001-experimental2" />
```

**Option 3: Use MSIX Packaging (Best Long-term Solution)**

As noted in other TSG entries, WCR APIs now **require MSIX packaging**. Self-contained deployment is primarily for unpackaged scenarios, which are no longer supported for WCR APIs starting with SDK 1.8 experimental 2.

Convert to MSIX packaging:
- Removes need for WindowsAppRuntimeInstall.exe
- Required for WCR APIs in newer SDK versions
- Provides better deployment and update experience

# Verification

1. Remove `WindowsAppSDKSelfContained` from .csproj OR upgrade SDK version
2. Rebuild your application
3. Deploy and run
4. Call `LanguageModel.IsAvailable()` or other WCR APIs
5. APIs should work without Application Error

# Deep Dive

## Background on WindowsAppSDKSelfContained

The `WindowsAppSDKSelfContained` property was designed to:
- Bundle Windows App Runtime DLLs with your application
- Eliminate dependency on separate WindowsAppRuntimeInstall.exe
- Enable simpler xcopy deployment for unpackaged apps

However, it introduced complexity with:
- Larger application package size
- DLL versioning and loading challenges
- Compatibility issues with certain SDK features (like WCR)

## Why This Issue Occurred

WCR APIs have additional runtime dependencies:
- AI model binaries
- DirectML components
- NPU drivers and abstractions
- Windows platform services

The self-contained deployment model in experimental3 did not properly account for these dependencies, leading to initialization failures.

## Current Recommendation

Given that:
1. WCR APIs now require MSIX packaging (SDK 1.8+)
2. Self-contained deployment is primarily for unpackaged scenarios
3. This specific issue is fixed in newer versions

**The best path forward is to use MSIX packaging**, which:
- Eliminates need for WindowsAppSDKSelfContained
- Properly handles all WCR dependencies
- Is required for WCR APIs anyway
- Provides superior deployment experience

## If You Must Stay Unpackaged

If you cannot use MSIX and need WCR functionality:
- Stick with Windows App SDK 1.7 or earlier where unpackaged was supported
- Do NOT use WindowsAppSDKSelfContained with WCR APIs
- Deploy Windows App Runtime separately
- **Note:** This is not a supported long-term solution

## Sample Repro

Original issue provided minimal repro at: https://github.com/ziyuanguo1998/BugRepro_WinAppSDK_ConsoleApp

# References

- [GitHub Issue #5280](https://github.com/microsoft/WindowsAppSDK/issues/5280)
- [Windows App SDK Deployment Documentation](https://learn.microsoft.com/windows/apps/windows-app-sdk/deploy-overview)
- [Self-Contained Deployment Guide](https://learn.microsoft.com/windows/apps/windows-app-sdk/deploy-unpackaged-apps)
