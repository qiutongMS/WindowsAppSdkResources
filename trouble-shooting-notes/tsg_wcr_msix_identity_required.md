---
id: wcr_msix_identity_required
title: "Windows Copilot Runtime APIs Require MSIX Packaging"
area: Packaging
symptoms:
  - "WCR APIs fail in unpackaged applications"
  - "UnauthorizedAccessException when calling WCR APIs without MSIX"
  - "AI Foundation APIs not working in traditional Win32 deployment"
errorCodes:
  - "UnauthorizedAccessException"
  - "0x80070005"
keywords:
  - "MSIX"
  - "unpackaged"
  - "WCR"
  - "Windows Copilot Runtime"
  - "packaging"
  - "identity"
  - "AI Foundation"
appliesTo:
  windows: ">=10.0.26120"
  winappsdk: ">=1.8.250515001-experimental2"
projectType: "packaged"
severity: "critical"
lastVerified: "2025-12-23"
references:
  - "https://github.com/microsoft/WindowsAppSDK/issues/5560"
  - "https://github.com/microsoft/WindowsAppSDK/issues/5451"
---

# Problem

Windows Copilot Runtime (WCR) APIs, including ImageDescriptionGenerator, TextRecognizer, Phi Silica, and other AI Foundation APIs, require MSIX packaging and application identity. Unpackaged applications will fail with `UnauthorizedAccessException` or access denied errors. This represents a permanent architectural change that contradicts the Windows App SDK's original principle of MSIX-optional deployment.

# Quick Diagnosis

1. Verify if your application is deployed as unpackaged (traditional Win32 installer, xcopy deployment, etc.)
2. Check if you're calling any WCR/AI Foundation APIs
3. Look for `UnauthorizedAccessException` or "Access is denied" errors
4. Confirm the error occurs when calling `GetReadyState()`, `CreateAsync()`, or other WCR API methods

# Root Cause

Starting with Windows App SDK 1.8 Experimental 2 (1.8.250515001-experimental2), Microsoft made MSIX identity a permanent requirement for Windows Copilot Runtime APIs. This change was confirmed by the WCR team as intentional and is necessary for:
- Privacy controls (Settings > Privacy & Security > Text and image generation)
- User consent management per-application
- Model access authorization through Windows security infrastructure

This requirement applies to ALL Windows Copilot Runtime APIs, including but not limited to:
- ImageDescriptionGenerator
- TextRecognizer (OCR)
- ImageScaler (Super Resolution)
- LanguageModel (Phi Silica)
- ImageSegmenter
- ObjectEraser

# Fix / Workaround

**There is NO workaround for unpackaged applications.** You must adopt MSIX packaging to use WCR APIs.

## Migration Steps:

1. **Convert your application to MSIX packaging:**
   - Add a Windows Application Packaging Project to your solution in Visual Studio
   - OR use MSIX Packaging Tool to package existing Win32 apps
   - OR manually create Package.appxmanifest and packaging structure

2. **Add required capabilities to Package.appxmanifest:**
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

3. **Deploy as MSIX:**
   - Sideload for development/testing
   - Publish to Microsoft Store
   - Enterprise deployment via App Installer or management tools

# Verification

1. Package your application as MSIX
2. Install the MSIX package
3. Run the application
4. Call WCR APIs - they should now work without `UnauthorizedAccessException`
5. Verify in Settings > Privacy & Security > Text and image generation that your app appears in the list

# Deep Dive

## Background

This change represents a significant departure from the Windows App SDK's founding principle of deployment flexibility. The [Windows App SDK README](https://github.com/microsoft/WindowsAppSDK/blob/main/README.md#L31) explicitly states support for both packaged and unpackaged deployment models. However, WCR APIs are an exception to this rule.

## Why MSIX is Required

1. **Privacy Controls:** Windows requires per-app privacy consent for AI model access, which is managed through the Settings app. This requires stable application identity.

2. **Security Model:** The underlying Windows AI platform (formerly Windows Copilot Runtime) uses AppContainer capabilities that require MSIX packaging.

3. **Limited Access Feature (LAF):** Some models (like Phi Silica) use Windows Limited Access Features which require application identity for access control.

## Impact on Existing Applications

- **Win32 Applications:** Must be repackaged as MSIX to use WCR APIs
- **Custom Installers:** Cannot use WCR APIs unless also deploying MSIX
- **Side-by-side Deployment:** Must use MSIX for the AI component or separate the AI functionality into a packaged component

## Alternatives

If MSIX packaging is not feasible for your application:
1. **Use cloud-based AI APIs** (Azure OpenAI, Azure Computer Vision, etc.)
2. **Bundle your own AI models** using ONNX Runtime or DirectML
3. **Create a separate MSIX-packaged component** that exposes WCR functionality via IPC

## Official Confirmation

Microsoft WCR team member confirmed on June 12, 2025:
> "Hi, I work on WCR team and can confirm this is the intended change."

This is a **permanent architectural requirement**, not a temporary limitation.

# References

- [GitHub Issue #5560 - MSIX Optional Commitment Violation](https://github.com/microsoft/WindowsAppSDK/issues/5560)
- [GitHub Issue #5451 - UnauthorizedAccessException in Unpackaged](https://github.com/microsoft/WindowsAppSDK/issues/5451)
- [Windows App SDK Documentation](https://learn.microsoft.com/windows/apps/windows-app-sdk/)
- [MSIX Packaging Tool](https://learn.microsoft.com/windows/msix/packaging-tool/tool-overview)
