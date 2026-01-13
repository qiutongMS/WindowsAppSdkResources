# Troubleshooting Guide: Authentication and Access Denied Errors in Windows AI APIs

## Symptom
Applications using Windows AI APIs (Phi Silica, OCR, Image Description) encounter authentication or access denied errors:
- `System.UnauthorizedAccessException: Access is denied`
- `COMException: "Not declared by app"`
- `Error 0x80073D3B: The product is not applicable or cannot be found`

## Affected Components
- **Windows Copilot Runtime APIs** (Phi Silica, ImageDescriptionGenerator, TextRecognizer)
- **Windows App SDK**: 1.7-experimental2, 1.7-experimental3, 1.8+
- **Packaging**: Both packaged (MSIX) and unpackaged scenarios

## Related Issues
- #5594 - Generic COMException "Not declared by app" if missing capability
- #5451 - ImageDescriptionGenerator yields UnauthorizedAccessException in unpackaged scenarios
- #5560 - Windows App SDK violates MSIX-optional commitment with AI Foundation APIs
- #5115 - Access Denied with Phi Silica capabilities
- #5185 - MakeAvailableAsync() Access is denied

## Root Causes

### 1. Missing systemAIModels Capability (Most Common)
The new `systemAIModels` capability is required but not documented in all API references.

### 2. MSIX Identity Requirement
As of Windows App SDK 1.8 Experimental 2, Windows AI Foundation APIs require MSIX identity for functionality, even in supposedly "unpackaged" scenarios.

### 3. Missing Limited Access Feature (LAF) Token
Some APIs require LAF unlock for `com.microsoft.windows.ai.languagemodel`.

## Solutions

### Solution 1: Add systemAIModels Capability (Primary Fix)

Update your `Package.appxmanifest` file:

```xml
<Package
  xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
  xmlns:mp="http://schemas.microsoft.com/appx/2014/phone/manifest"
  xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
  xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
  xmlns:systemai="http://schemas.microsoft.com/appx/manifest/systemai/windows10"
  IgnorableNamespaces="uap rescap systemai">

  <!-- ... -->

  <Capabilities>
    <rescap:Capability Name="runFullTrust" />
    <systemai:Capability Name="systemAIModels"/>
  </Capabilities>
</Package>
```

**Note**: This capability was introduced in Windows App SDK 1.8 Experimental 3 but is not consistently documented in API references.

### Solution 2: Verify Limited Access Feature Unlock

For Phi Silica APIs, ensure LAF is properly unlocked:

```csharp
var featureId = "com.microsoft.windows.ai.languagemodel";
var limitedAccessFeatureResult = LimitedAccessFeatures.TryUnlockFeature(
    featureId,
    token,
    $"{publisherId} has registered their use of {featureId} with Microsoft and agrees to the terms of use.");

if (limitedAccessFeatureResult.Status != LimitedAccessFeatureStatus.Available && 
    limitedAccessFeatureResult.Status != LimitedAccessFeatureStatus.AvailableWithoutToken)
{
    throw new InvalidOperationException($"Phi-Silica is not available: {limitedAccessFeatureResult.Status}");
}
```

### Solution 3: For Unpackaged Scenarios

**Current Limitation**: As of Windows App SDK 1.8+, unpackaged applications cannot use Windows AI Foundation APIs. This contradicts the original "MSIX-optional" promise.

**Workarounds**:
1. **Use MSIX packaging** (only reliable option currently)
2. **Wait for future SDK updates** that may restore unpackaged support
3. **Consider alternative ML frameworks** if packaging is not an option

### Solution 4: Add Self-Contained Property

For some scenarios, add to your `.csproj`:

```xml
<PropertyGroup>
  <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
</PropertyGroup>
```

## Verification Steps

1. **Check manifest has systemAIModels capability**
2. **Verify app has MSIX identity** (check with `Get-AppxPackage` in PowerShell)
3. **Test LAF unlock status** before calling AI APIs
4. **Review error messages** for specific missing capabilities

## Expected Outcome
- No access denied exceptions
- `LanguageModel.GetReadyState()` returns expected state
- `ImageDescriptionGenerator.GetReadyState()` succeeds
- `TextRecognizer.CreateAsync()` completes successfully

## Known Limitations

### Windows App SDK 1.7 Experimental 2+
- **Unpackaged apps cannot use Windows AI APIs** - This is a breaking change from previous versions
- Missing capability errors are generic and unhelpful (just "Not declared by app")
- Documentation inconsistencies across API references

### Regional Restrictions
Some AI models may not be available in certain regions, leading to access denied errors that are actually availability issues.

## Additional Resources
- [Windows AI APIs Documentation](https://learn.microsoft.com/windows/ai/apis/)
- [Windows App SDK Release Notes](https://learn.microsoft.com/windows/apps/windows-app-sdk/experimental-channel)
- Issue #5594 for detailed capability requirements
- Issue #5560 for packaging requirement discussion

## Related TSGs
- [Model Availability and Loading Issues](windows-ai-model-availability.md)
- [Phi Silica Initialization Failures](windows-ai-phi-silica-initialization.md)
