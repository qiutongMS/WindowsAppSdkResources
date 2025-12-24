---
id: wcr_x86_not_supported
title: "WCR APIs throw REGDB_E_CLASSNOTREG on x86 architecture"
area: Runtime
symptoms:
  - "Class not registered error when calling WCR APIs on x86"
  - "ImageDescriptionGenerator.GetReadyState() throws 0x80040154"
  - "WCR APIs work on x64/ARM64 but fail on x86"
errorCodes:
  - "0x80040154"
  - "REGDB_E_CLASSNOTREG"
keywords:
  - "x86"
  - "architecture"
  - "class not registered"
  - "REGDB_E_CLASSNOTREG"
  - "ImageDescriptionGenerator"
  - "WCR"
  - "platform target"
appliesTo:
  windows: ">=10.0.26120"
  winappsdk: ">=1.7.0-experimental"
projectType: "packaged"
severity: "common"
lastVerified: "2025-12-23"
references:
  - "https://github.com/microsoft/WindowsAppSDK/issues/5613"
---

# Problem

When calling `ImageDescriptionGenerator.GetReadyState()` or other Windows Copilot Runtime APIs from an x86 application running on x64 or ARM64 Windows, the application throws `Class not registered (0x80040154 (REGDB_E_CLASSNOTREG))`. The same code works fine when compiled for x64 or ARM64 native architectures.

# Quick Diagnosis

1. Check your project's Platform Target setting in Visual Studio
2. Verify if it's set to "x86" or "Any CPU" (which may default to x86)
3. Confirm the error code: `0x80040154` or `REGDB_E_CLASSNOTREG`
4. Test if the application works when recompiled for x64 or ARM64

# Root Cause

Windows Copilot Runtime (WCR) APIs and AI models are **only available for x64 and ARM64 architectures**. The x86 (32-bit) platform is not supported. When an x86 application attempts to call WCR APIs, Windows cannot locate the required COM classes because they are only registered for 64-bit platforms.

This is an architectural limitation - the underlying AI models and DirectML infrastructure require 64-bit addressing and are not available in 32-bit binaries.

# Fix / Workaround

**Change your application's Platform Target to x64 or ARM64:**

## Visual Studio Steps:

1. Open your project in Visual Studio
2. Go to **Project Properties** > **Build** (or **Application** tab in some templates)
3. Change **Platform Target** from "x86" or "Any CPU" to **"x64"**
4. For ARM64 devices, select **"ARM64"** instead
5. Rebuild your application

## .csproj Manual Edit:

```xml
<PropertyGroup>
  <PlatformTarget>x64</PlatformTarget>
  <!-- Or for ARM64: -->
  <!-- <PlatformTarget>ARM64</PlatformTarget> -->
</PropertyGroup>
```

## Multi-Platform Support:

If you need to support multiple architectures, create separate build configurations:
- x64 for Intel/AMD machines
- ARM64 for ARM-based devices (Snapdragon X, etc.)
- Do NOT create x86 builds if using WCR APIs

# Verification

1. Change Platform Target to x64 (or ARM64 if on ARM device)
2. Rebuild your application
3. Deploy and run
4. Call `ImageDescriptionGenerator.GetReadyState()` or other WCR API
5. The API should now execute without `REGDB_E_CLASSNOTREG` error

# Deep Dive

## Supported Architectures

Windows Copilot Runtime APIs support:
- ✅ **x64** (AMD64): Full support on Intel and AMD processors
- ✅ **ARM64**: Full support on Snapdragon X Elite/Plus and other ARM64 devices
- ❌ **x86** (32-bit): NOT supported

## Why x86 is Not Supported

1. **DirectML Requirements:** The underlying DirectML infrastructure requires 64-bit architecture for GPU acceleration
2. **Model Size:** AI models are optimized for 64-bit memory addressing
3. **NPU Access:** Neural Processing Unit (NPU) drivers and interfaces are 64-bit only
4. **Platform Direction:** Microsoft is not investing in 32-bit Windows development

## Common Scenarios Where This Occurs

- Legacy projects defaulting to "x86" platform target
- "Any CPU" projects running as x86 on x64 Windows
- Compatibility settings forcing 32-bit execution
- Side-by-side with 32-bit components requiring x86 process

## Alternative Approaches

If you must support x86 for other reasons:

1. **Out-of-Process Architecture:**
   - Build your WCR AI component as a separate x64/ARM64 executable
   - Communicate from x86 main app via IPC (named pipes, WCF, gRPC, etc.)

2. **Conditional Features:**
   - Detect architecture at runtime
   - Disable AI features on x86, enable on x64/ARM64
   
```csharp
if (RuntimeInformation.ProcessArchitecture == Architecture.X64 || 
    RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
{
    // Enable WCR AI features
}
else
{
    // Fallback or disable AI features
}
```

3. **Cloud Fallback:**
   - Use Azure AI Services for x86 builds
   - Use local WCR APIs for x64/ARM64 builds

## Documentation Status

As of the original issue report, the WCR documentation does not prominently state the x86 limitation. Developers encounter this through runtime errors rather than build-time warnings or clear documentation.

# References

- [GitHub Issue #5613](https://github.com/microsoft/WindowsAppSDK/issues/5613)
- [DirectML System Requirements](https://learn.microsoft.com/windows/ai/directml/dml)
- [Windows on ARM Documentation](https://learn.microsoft.com/windows/arm/)
