---
id: wcr_laf_missing
title: "Limited Access Feature com.microsoft.windows.ai.languagemodel is missing"
area: Runtime
symptoms:
  - "Limited Access Feature is not available: com.microsoft.windows.ai.languagemodel. Status: 3"
  - "Phi Silica generative samples do not work"
  - "AI Dev Gallery generative features fail"
errorCodes:
  - "Status: 3"
keywords:
  - "Limited Access Feature"
  - "LAF"
  - "com.microsoft.windows.ai.languagemodel"
  - "Status 3"
  - "Phi Silica"
  - "missing"
appliesTo:
  windows: ">=10.0.26120"
  winappsdk: ">=1.8.250916003"
projectType: "packaged"
severity: "critical"
lastVerified: "2025-12-23"
references:
  - "https://github.com/microsoft/WindowsAppSDK/issues/5892"
---

# Problem

When attempting to use Phi Silica APIs or running the AI Dev Gallery, an exception is thrown indicating "Limited Access Feature is not available: com.microsoft.windows.ai.languagemodel. Status: 3". This renders all Phi Silica/text generation APIs completely unusable. The issue affects both Windows Insider and Production builds where the LAF component appears to be missing from Windows itself.

# Quick Diagnosis

1. Verify you're trying to use Phi Silica/LanguageModel APIs
2. Check for exception mentioning "Limited Access Feature is not available"
3. Look for "com.microsoft.windows.ai.languagemodel" and "Status: 3"
4. Try running AI Dev Gallery - generative samples will fail
5. Verify Windows version: Check Settings > System > About

# Root Cause

The Limited Access Feature (LAF) "com.microsoft.windows.ai.languagemodel" is not present in certain Windows builds, even Windows Insider builds that should support Phi Silica. Status code 3 indicates "NotAvailable" - the LAF is not registered or accessible on the Windows installation.

This can occur due to:
1. **Windows build doesn't include Phi Silica** - not all Insider builds have the feature
2. **Windows Update incomplete** - Phi Silica components not installed
3. **Windows region restrictions** - some regions may not have access initially
4. **Windows build rolled back** - after an update that removed the feature

# Fix / Workaround

**Solution 1: Update to Latest Windows Insider Build (Dev/Canary Channel)**

1. Open Settings > Windows Update > Windows Insider Program
2. Ensure you're enrolled in **Dev Channel** or **Canary Channel** (not Beta/Release Preview)
3. Check for and install all available Windows Updates
4. Restart your computer
5. Verify build number is **26220 or higher**

**Solution 2: Reinstall Windows Insider Build**

If updates don't resolve the issue:

1. Settings > Windows Update > Windows Insider Program
2. **Unenroll** from Windows Insider Program
3. **Re-enroll** in Dev or Canary Channel
4. Check for updates and install
5. May require multiple update cycles to get all Phi Silica components

**Solution 3: Check for Model Updates**

1. Open Settings > Apps > Optional features
2. Look for "Windows AI Model" or similar components
3. Install any available AI-related optional features
4. Restart

**Solution 4: Use winget to Install Phi Silica Model (if available)**

```powershell
# Check if Windows AI packages are available
winget search "windows AI"

# Install if found
winget install "Microsoft.Windows.AI.Phi"
```

**Solution 5: Wait for Windows Update Push**

The LAF may be pushed via Windows Update over time. Check for updates regularly.

# Verification

After applying fixes:

1. Open Windows Terminal (PowerShell)
2. Check Windows build version:
   ```powershell
   [System.Environment]::OSVersion.Version
   ```
3. Open AI Dev Gallery application
4. Navigate to Samples > Phi Silica > Generate Text
5. Try generating text - it should work
6. In your application, test LAF unlock:
   ```csharp
   var result = LimitedAccessFeatures.TryUnlockFeature(
       "com.microsoft.windows.ai.languagemodel", 
       "", 
       "");
   Console.WriteLine($"LAF Status: {result.Status}");
   // Should show: Available or AvailableWithoutToken
   ```

# Deep Dive

## LAF Status Codes

| Status | Value | Meaning |
|--------|-------|---------|
| Unknown | 0 | LAF status cannot be determined |
| Available | 1 | LAF is available and unlocked successfully |
| AvailableWithoutToken | 2 | LAF is available, no token needed |
| **NotAvailable** | **3** | **LAF is not present/accessible (this error)** |
| DisabledByPolicy | 4 | LAF blocked by enterprise policy |
| UnlockingNotApproved | 5 | LAF requires approval/token |

**Status 3 (NotAvailable)** specifically means the LAF infrastructure cannot find the feature on the system.

## Windows Build Requirements

Phi Silica LAF has been available in:
- Windows 11 Insider builds **26120+** (variable availability)
- Windows 11 Insider builds **26220+** (more consistent)
- Certain Windows 11 24H2 builds (limited)

NOT available in:
- Windows 11 23H2 or earlier production builds
- Windows 10
- Windows 11 Beta/Release Preview channels (generally)

## Affected Components

When LAF is missing, these fail:
- ✗ Phi Silica text generation (`LanguageModel` API)
- ✗ Phi Silica embeddings generation
- ✗ AI Dev Gallery generative samples
- ✗ Any application using Phi Silica

These may still work (not LAF-dependent):
- ✓ Text Recognition (OCR) - `TextRecognizer`
- ✓ Image Description - `ImageDescriptionGenerator`
- ✓ Image Super Resolution - `ImageScaler`
- ✓ Other WCR vision APIs

## Regional Considerations

Some reports indicate Phi Silica availability varies by:
- Windows region/locale settings
- Microsoft account region
- Initial Windows installation region

Try changing region to United States if in another region:
1. Settings > Time & Language > Language & Region
2. Set Country or region to "United States"
3. Restart
4. Check for Windows Updates

## Known Issues Timeline

- **October 2025:** Issue #5892 reported - LAF missing from Insider builds
- **Ongoing:** Variable LAF availability across different Insider builds
- **Status:** Microsoft aware, LAF availability tied to Windows feature rollout

## Alternative Solutions

If you cannot get LAF working:

1. **Use cloud-based LLMs** instead of Phi Silica
   - Azure OpenAI Service
   - Other cloud API providers

2. **Use local ONNX models** with ONNX Runtime
   - Phi-3 ONNX models from HuggingFace
   - Run locally without WCR infrastructure

3. **Wait for stable Windows 11 release** with Phi Silica
   - Feature may be more reliable in future stable builds
   - Insider builds are experimental and variable

# References

- [GitHub Issue #5892](https://github.com/microsoft/WindowsAppSDK/issues/5892)
- [Phi Silica Documentation](https://learn.microsoft.com/windows/ai/apis/phi-silica)
- [Windows Insider Program](https://insider.windows.com/)
- [Limited Access Features Overview](https://learn.microsoft.com/windows/apps/develop/limited-access-features)
