---
id: wcr_phi_silica_not_declared
title: "Phi Silica GetReadyState throws 'Unspecified error Not declared by app'"
area: Runtime
symptoms:
  - "LanguageModel.GetReadyState() throws COMException"
  - "Unspecified error Not declared by app when using Phi Silica"
  - "HResult=0x80004005 on Phi Silica API calls"
errorCodes:
  - "0x80004005"
  - "COMException"
keywords:
  - "Phi Silica"
  - "LanguageModel"
  - "Not declared by app"
  - "GetReadyState"
  - "COMException"
  - "LAF"
  - "Limited Access Feature"
appliesTo:
  windows: ">=10.0.26220"
  winappsdk: "2.0.250930001-experimental1"
projectType: "packaged"
severity: "critical"
lastVerified: "2025-12-23"
references:
  - "https://github.com/microsoft/WindowsAppSDK/issues/6019"
---

# Problem

When calling `LanguageModel.GetReadyState()` for Phi Silica, the application throws a `System.Runtime.InteropServices.COMException` with HResult `0x80004005`, message "Unspecified error", and detail "Not declared by app". This occurs even when following the documented setup steps including LAF unlock.

# Quick Diagnosis

1. Verify you're calling Phi Silica APIs (`LanguageModel.GetReadyState()`, `CreateAsync()`, etc.)
2. Check for `COMException` with message "Unspecified error" and "Not declared by app"
3. Confirm you have the LAF unlock code (TryUnlockFeature) in place
4. Verify you're on Windows Insider build with Phi Silica support
5. Check if `systemAIModels` capability is in your manifest

# Root Cause

This error typically indicates one or more missing configuration requirements for Phi Silica access:

1. **Missing systemAIModels capability** in Package.appxmanifest
2. **LAF (Limited Access Feature) not unlocked** or unlocked incorrectly
3. **Windows version doesn't support** the Phi Silica model
4. **LAF token/publisher ID** issues (for preview versions)

The error message "Not declared by app" is generic and doesn't clearly indicate which requirement is missing.

# Fix / Workaround

**Step 1: Add systemAIModels capability to Package.appxmanifest**

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

**Step 2: Verify LAF unlock code (for experimental releases requiring LAF)**

```csharp
using Microsoft.Windows.AI.Text;
using Windows.Security.Authorization.AppCapabilityAccess;

// For some experimental releases, you may need LAF unlock
var featureId = "com.microsoft.windows.ai.languagemodel";
var demoToken = "YOUR_TOKEN"; // Get from Microsoft
var demoPublisherId = "YOUR_PUBLISHER_ID";

var limitedAccessFeatureResult = LimitedAccessFeatures.TryUnlockFeature(
    featureId,
    demoToken,
    $"{demoPublisherId} has registered their use of {featureId} with Microsoft and agrees to the terms of use.");

if (limitedAccessFeatureResult.Status != LimitedAccessFeatureStatus.Available && 
    limitedAccessFeatureResult.Status != LimitedAccessFeatureStatus.AvailableWithoutToken)
{
    throw new InvalidOperationException($"Phi-Silica is not available: {limitedAccessFeatureResult.Status}");
}

// Now check ready state
var readyState = LanguageModel.GetReadyState();
```

**Step 3: Ensure Windows version supports Phi Silica**

- Requires Windows 11 Insider Build (Dev or Canary channel)
- Build 26220 or higher recommended
- Check: Settings > Windows Update > Windows Insider Program

**Step 4: Rebuild and redeploy**

- Clean solution
- Rebuild project
- Redeploy MSIX package
- Run application

# Verification

1. Apply all fixes above
2. Rebuild and deploy your application
3. Run the app and call `LanguageModel.GetReadyState()`
4. Should return `AIFeatureReadyState` value without exception
5. If returns `NotReady`, call `LanguageModel.EnsureReadyAsync()`
6. Create LanguageModel instance: `var model = await LanguageModel.CreateAsync()`
7. Test text generation

# Deep Dive

## LAF Status in Different SDK Versions

**Windows App SDK 1.7-1.8 (experimental):**
- Some versions required LAF token from Microsoft
- "com.microsoft.windows.ai.languagemodel" LAF
- Status: 3 means LAF not available on this Windows build

**Windows App SDK 2.0+ (experimental):**
- LAF requirements may vary
- Some builds have LAF removed or made available without token
- Check current SDK release notes

## "Not declared by app" Error Sources

This error can mean:

1. **Missing systemAIModels capability** (most common)
2. **LAF not unlocked** (experimental versions)
3. **LAF not available** on this Windows version
4. **Model not installed** or deployment failed
5. **MSIX packaging issue** (see related TSG on MSIX requirement)

## Troubleshooting Steps

If the error persists after applying fixes:

```csharp
// 1. Check LAF status
var lafResult = LimitedAccessFeatures.TryUnlockFeature(
    "com.microsoft.windows.ai.languagemodel", "", "");
Console.WriteLine($"LAF Status: {lafResult.Status}");

// 2. Check GetReadyState without LAF
try 
{
    var state = LanguageModel.GetReadyState();
    Console.WriteLine($"Ready State: {state}");
}
catch (Exception ex)
{
    Console.WriteLine($"GetReadyState Error: {ex.Message}");
}

// 3. Try EnsureReadyAsync
try
{
    var ensureResult = await LanguageModel.EnsureReadyAsync();
    Console.WriteLine($"Ensure Result: {ensureResult.Status}");
    if (ensureResult.Status != AIFeatureReadyResultState.Success)
    {
        Console.WriteLine($"Error: {ensureResult.ExtendedError?.Message}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"EnsureReadyAsync Error: {ex.Message}");
}
```

## Platform Requirements

**Minimum requirements for Phi Silica:**
- Windows 11 24H2 or Windows Insider build 26220+
- NPU (Neural Processing Unit) OR compatible GPU/CPU
- Windows App SDK 1.7 experimental or later
- MSIX packaged application
- systemAIModels capability

**Supported Hardware:**
- Qualcomm Snapdragon X Elite/Plus (optimal - has NPU)
- Intel Core Ultra processors with AI Boost
- AMD Ryzen AI series
- Other devices may run on GPU/CPU fallback

## Related Issues

- Issue #5892: LAF missing from Windows entirely
- Issue #5594: Generic capability errors
- Issue #5982: GetReadyState returns NotReady when available

# References

- [GitHub Issue #6019](https://github.com/microsoft/WindowsAppSDK/issues/6019)
- [Phi Silica Documentation](https://learn.microsoft.com/windows/ai/apis/phi-silica)
- [Limited Access Features](https://learn.microsoft.com/windows/apps/develop/limited-access-features)
- [Get Started with Windows AI APIs](https://learn.microsoft.com/windows/ai/apis/get-started)
