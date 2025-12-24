---
id: wcr_missing_systemaimodels_capability
title: "COMException 'Not declared by app' when calling WCR AI APIs"
area: WinUI
symptoms:
  - "System.Runtime.InteropServices.COMException with 'Unspecified error' and 'Not declared by app'"
  - "ImageDescriptionGenerator.GetReadyState() throws exception"
  - "TextRecognizer API throws 'Not declared by app' error"
  - "All Windows AI APIs fail with capability error"
errorCodes:
  - "0x80004005"
  - "COMException"
keywords:
  - "systemAIModels"
  - "capability"
  - "Not declared by app"
  - "WCR"
  - "Windows Copilot Runtime"
  - "AI APIs"
  - "manifest"
  - "ImageDescriptionGenerator"
  - "TextRecognizer"
  - "Phi Silica"
appliesTo:
  windows: ">=10.0.26120"
  winappsdk: ">=1.7.250513003-experimental"
projectType: "packaged"
severity: "critical"
lastVerified: "2025-12-23"
references:
  - "https://github.com/microsoft/WindowsAppSDK/issues/5594"
  - "https://github.com/microsoft/WindowsAppSDK/issues/6019"
---

# Problem

When calling Windows Copilot Runtime (WCR) AI APIs such as `ImageDescriptionGenerator.GetReadyState()`, `TextRecognizer`, or Phi Silica `LanguageModel` APIs, the application throws a `System.Runtime.InteropServices.COMException` with the message "Unspecified error" and "Not declared by app". This error provides no indication of which capability is missing from the app manifest.

# Quick Diagnosis

1. Check if you're calling any WCR AI APIs (ImageDescriptionGenerator, TextRecognizer, LanguageModel, ImageScaler, etc.)
2. Look for the specific error: `COMException` with "Not declared by app" or `HResult=0x80004005`
3. Verify your Package.appxmanifest for the `systemAIModels` capability
4. Check if the `systemai` namespace is declared in your manifest

# Root Cause

Windows Copilot Runtime APIs require a new restricted capability called `systemAIModels` in the app manifest. This capability was introduced in Windows App SDK 1.7+ experimental releases and must be explicitly declared for packaged applications. The error message is generic and does not indicate which capability is missing, making this a common pitfall for developers.

# Fix / Workaround

1. **Add the systemai namespace to your Package.appxmanifest:**

```xml
<Package
  xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
  xmlns:mp="http://schemas.microsoft.com/appx/2014/phone/manifest"
  xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
  xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
  xmlns:systemai="http://schemas.microsoft.com/appx/manifest/systemai/windows10"
  IgnorableNamespaces="uap rescap systemai">
```

2. **Add the systemAIModels capability to your Capabilities section:**

```xml
<Capabilities>
  <rescap:Capability Name="runFullTrust" />
  <systemai:Capability Name="systemAIModels"/>
</Capabilities>
```

3. **Rebuild and redeploy your application**

# Verification

1. Clean and rebuild your project
2. Redeploy the MSIX package
3. Run your application and call the WCR API (e.g., `ImageDescriptionGenerator.GetReadyState()`)
4. The API should now execute without throwing the "Not declared by app" exception

# Deep Dive

The `systemAIModels` capability is a restricted capability that grants access to Windows Copilot Runtime AI models and APIs including:
- **Text Recognition (OCR):** TextRecognizer API
- **Image Description:** ImageDescriptionGenerator API  
- **Image Processing:** ImageScaler (Super Resolution), ImageSegmenter
- **Phi Silica:** LanguageModel API for on-device AI text generation
- **Object Erase:** ObjectEraser API

This capability requirement was introduced in experimental releases starting with version 1.7.250127003-experimental3 and carried forward into version 1.8+ releases.

**Important Notes:**
- This capability is only available for **packaged (MSIX)** applications
- The capability must be declared even if using `EnsureReadyAsync()` or `GetReadyState()` APIs
- The error message "Unspecified error - Not declared by app" is intentionally vague and does not indicate the specific missing capability
- The capability requirement is documented in the getting started guide but not prominently featured in API documentation

**Related Issues:**
- Applications deployed without this capability will fail at runtime
- The error can occur even before checking model availability with `GetReadyState()`
- Unpackaged applications cannot use WCR APIs (see related TSG for MSIX requirements)

# References

- [GitHub Issue #5594](https://github.com/microsoft/WindowsAppSDK/issues/5594)
- [GitHub Issue #6019](https://github.com/microsoft/WindowsAppSDK/issues/6019)
- [Get started with Windows AI APIs](https://learn.microsoft.com/windows/ai/apis/get-started)
- [Windows App SDK Experimental Channel](https://learn.microsoft.com/windows/apps/windows-app-sdk/experimental-channel)
