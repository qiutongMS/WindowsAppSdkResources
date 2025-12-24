---
id: wcr_unauthorized_unpackaged
title: "UnauthorizedAccessException calling WCR APIs in unpackaged app"
area: Activation
symptoms:
  - "System.UnauthorizedAccessException when calling ImageDescriptionGenerator"
  - "Access is denied / Denied access error message"
  - "WCR APIs fail after upgrading from experimental1 to experimental2"
errorCodes:
  - "UnauthorizedAccessException"
  - "0x80070005"
keywords:
  - "UnauthorizedAccessException"
  - "unpackaged"
  - "ImageDescriptionGenerator"
  - "access denied"
  - "WCR"
  - "experimental2"
appliesTo:
  windows: ">=10.0.26120"
  winappsdk: ">=1.8.250515001-experimental2"
projectType: "unpackaged"
severity: "critical"
lastVerified: "2025-12-23"
references:
  - "https://github.com/microsoft/WindowsAppSDK/issues/5451"
  - "https://github.com/microsoft/WindowsAppSDK/issues/5560"
---

# Problem

After upgrading from Windows App SDK 1.8 Experimental 1 to Experimental 2 or later, calling WCR `ImageDescriptionGenerator.GetReadyState()` or other WCR APIs throws `System.UnauthorizedAccessException` with the message "Access is denied" followed by "Denied access". This occurs specifically in unpackaged application scenarios.

# Quick Diagnosis

1. Confirm your application is deployed as unpackaged (not MSIX)
2. Verify you upgraded from experimental1 to experimental2 or later
3. Check the exact error: `System.UnauthorizedAccessException` in System.Private.CoreLib.dll
4. Confirm the error message includes "Access is denied" or "Denied access"

# Root Cause

Starting with Windows App SDK 1.8 Experimental 2 (version 1.8.250515001-experimental2), Microsoft changed Windows Copilot Runtime APIs to **require MSIX identity**. This is a permanent, intentional change confirmed by the WCR team. Unpackaged applications can no longer access WCR APIs regardless of configuration or capabilities.

# Fix / Workaround

**You must convert your application to MSIX packaging.** There is no workaround for unpackaged deployment.

## Solution Steps:

1. **Package your application as MSIX** (see detailed guide in `tsg_wcr_msix_identity_required.md`)

2. **Add the systemAIModels capability to Package.appxmanifest:**

```xml
<Package
  xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
  xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
  xmlns:systemai="http://schemas.microsoft.com/appx/manifest/systemai/windows10"
  IgnorableNamespaces="rescap systemai">
  
  <Capabilities>
    <rescap:Capability Name="runFullTrust" />
    <systemai:Capability Name="systemAIModels"/>
  </Capabilities>
</Package>
```

3. **Deploy and test as MSIX package**

# Verification

1. Package your application as MSIX
2. Install the MSIX package (sideload or through Store)
3. Run your application
4. Call `ImageDescriptionGenerator.GetReadyState()` or other WCR APIs
5. APIs should execute without `UnauthorizedAccessException`

# Deep Dive

## Change Timeline

- **1.8 Experimental 1:** WCR APIs worked in unpackaged scenarios
- **1.8 Experimental 2+:** MSIX identity became required, breaking unpackaged apps

## Why This Changed

Microsoft WCR team confirmed this is an intentional, permanent change. The requirement exists because:
- Privacy controls in Windows Settings require stable app identity
- Model access uses Windows security features that require AppContainer
- Limited Access Features (LAF) framework requires application identity

## Error Message Analysis

The error message is unhelpful:
```
Exception thrown: 'System.UnauthorizedAccessException' in System.Private.CoreLib.dll
Access is denied.

Denied access
```

This generic message provides no indication that MSIX packaging is required. Developers must consult documentation or community resources to understand the root cause.

## Affected APIs

ALL Windows Copilot Runtime APIs require MSIX:
- ImageDescriptionGenerator
- TextRecognizer  
- ImageScaler
- LanguageModel (Phi Silica)
- ImageSegmenter
- ObjectEraser

## Migration Considerations

If you cannot adopt MSIX packaging:
1. **Use cloud-based alternatives** (Azure AI Services)
2. **Bundle your own models** (ONNX Runtime, DirectML)
3. **Split your app:** Create separate MSIX component for AI features with IPC communication

## Official Statement

From GitHub issue #5451, Microsoft WCR team member:
> "Hi, I work on WCR team and can confirm this is the intended change."

# References

- [GitHub Issue #5451](https://github.com/microsoft/WindowsAppSDK/issues/5451)
- [GitHub Issue #5560 - MSIX Requirement Discussion](https://github.com/microsoft/WindowsAppSDK/issues/5560)
- [MSIX Packaging Documentation](https://learn.microsoft.com/windows/msix/)
- [Windows AI APIs Documentation](https://learn.microsoft.com/windows/ai/apis/)
