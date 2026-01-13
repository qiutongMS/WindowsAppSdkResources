# Troubleshooting Guide: Phi Silica Initialization and Registration Errors

## Symptom
Phi Silica language model fails to initialize with COM/registration errors:
- `COMException: Class not registered (0x80040154)`
- `LanguageModel.GetReadyState()` throws exceptions
- `LanguageModel.CreateAsync()` fails with "Failed to find proxy registration"
- `LanguageModel.IsAvailable()` throws instead of returning false

## Affected Components
- **Microsoft.Windows.AI.Generative.LanguageModel** (Phi Silica)
- **Windows App SDK**: 1.7-experimental2+, 1.8+, 2.0-experimental1
- **OS Requirements**: Windows 11 Insider builds, Copilot+ PCs

## Related Issues
- [#5123](https://github.com/microsoft/WindowsAppSDK/issues/5123) - COMException "Class not registered" on LanguageModel.IsAvailable()
- [#5129](https://github.com/microsoft/WindowsAppSDK/issues/5129) - CreateAsync fails with "Failed to find proxy registration"
- [#5156](https://github.com/microsoft/WindowsAppSDK/issues/5156) - CreateAsync throws exception on X Plus

## Root Cause

### Cause 1: Wrong SDK Version

The `Microsoft.Windows.AI.Generative` namespace is **only available in experimental SDK versions**:
- ❌ Stable releases (1.5, 1.6, 1.7, 1.8): No Phi Silica support
- ✅ Experimental releases (1.7-exp3+, 1.8-exp+, 2.0-exp+): Phi Silica included

**Note**: SDK 1.7-experimental2 announcement mentioned Phi Silica, but it wasn't actually included until experimental3.

### Cause 2: Missing OS Requirements

Phi Silica requires:
- **Windows 11 Insider Dev Channel** (Build 26120+)
- **Copilot+ PC** with NPU (or compatible AI hardware)
- **Geographic availability** (limited in some regions like China)

### Cause 3: Model Not Downloaded

The Phi Silica model package must be downloaded before first use.

### Cause 4: Missing Manifest Configuration

App manifest must declare `systemAIModels` capability and LAF unlock.

### Cause 5: Platform Not Supported

x86 architecture is not supported - only x64 and ARM64 work.

## Solutions

### Solution 1: Use Experimental SDK Version

```xml
<ItemGroup>
  <!-- ✅ Use experimental version -->
  <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.7.*-experimental3" />
  <!-- Stable versions do NOT include Phi Silica -->
</ItemGroup>
```

### Solution 2: Verify OS Requirements

**Minimum:** Windows 11 Insider Build 26120+, Copilot+ PC

```powershell
winver  # Check build >= 26120
```

**Not on Insider?** Join Windows Insider Program > Dev Channel

### Solution 3: Configure App Manifest

Add to `Package.appxmanifest`:

```xml
<Package xmlns:systemai="http://schemas.microsoft.com/appx/manifest/systemai/windows10">
  <Capabilities>
    <systemai:Capability Name="systemAIModels"/>
  </Capabilities>
</Package>
```

### Solution 4: Initialization Sequence

```csharp
// Step 1: Unlock LAF
var lafResult = LimitedAccessFeatures.TryUnlockFeature(
    "com.microsoft.windows.ai.languagemodel",
    lafToken,
    attestation);

if (lafResult.Status != LimitedAccessFeatureStatus.Available)
    throw new Exception($"LAF not available: {lafResult.Status}");

// Step 2: Download model if needed
if (!LanguageModel.IsAvailable())
{
    var result = await LanguageModel.MakeAvailableAsync();
    if (result.Status != PackageDeploymentStatus.CompletedSuccess)
        throw new Exception("Model download failed");
}

// Step 3: Create model
using var model = await LanguageModel.CreateAsync();
var response = await model.GenerateResponseAsync("Test prompt");
```

## Diagnostic Checklist

- [ ] SDK version contains "experimental" (not stable release)
- [ ] Windows build >= 26120 (run `winver`)
- [ ] Package.appxmanifest has systemAIModels capability
- [ ] LAF unlock returns Available or AvailableWithoutToken
- [ ] Platform is x64 or ARM64 (not x86)

## Known SDK Issues

| SDK Version | Status |
|-------------|--------|
| 1.7-exp2 | ❌ Broken (announced but not included) |
| 1.7-exp3+ | ✅ Works |
| Stable (1.5-1.8) | ❌ No Phi Silica |

## Additional Resources
- [Phi Silica API Docs](https://learn.microsoft.com/windows/ai/apis/phi-silica)

## Related TSGs
- [Phi Silica Output Issues](windows-ai-phi-silica-output-issues.md)
