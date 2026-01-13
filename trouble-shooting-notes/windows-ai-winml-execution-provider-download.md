# Troubleshooting Guide: WinML Execution Provider Download Failures

## Symptom
Execution providers fail to download automatically:
- `ensure_and_register_certified_async().get()` fails
- Error: `0x80073D3B The product is not applicable`
- TensorRT provider not downloaded
- Corporate network blocks downloads

## Affected Components
- **Microsoft.Windows.AI.MachineLearning**
- **ONNX Runtime** execution providers

## Related Issues
- [#6072](https://github.com/microsoft/WindowsAppSDK/issues/6072) - TensorRT not downloaded automatically
- [#5862](https://github.com/microsoft/WindowsAppSDK/issues/5862) - "Product not applicable" error

## Root Cause

**Corporate/Enterprise networks** block Microsoft CDN downloads:
- Firewall/proxy restrictions
- Package Manager service disabled
- VPN interference

## Solutions

### Solution 1: Manual TensorRT Installation

**Download** from NVIDIA:
- CUDA Toolkit: https://developer.nvidia.com/cuda-downloads
- TensorRT: https://developer.nvidia.com/tensorrt

**Place DLLs** in app directory or add to PATH

**Verify:**
```csharp
var providers = LearningModelSessionOptions.FindAllAvailableExecutionProviders();
// Should include "TensorRT" after manual install
```

### Solution 2: Use Built-in Providers

```csharp
// ✅ DirectML is built into Windows 10 1903+
var session = new LearningModelSession(model, new LearningModelSessionOptions
{
    ExecutionProvider = "DirectML"  // No download needed
});
```

**Built-in (no download):**
- DirectML - GPU acceleration on Windows
- CPU - Always available

**Requires download:**
- TensorRT - NVIDIA GPUs (may be blocked)

### Solution 3: Fallback Strategy

```csharp
var preferredProviders = new[] { "TensorRT", "DirectML", "CPU" };

foreach (var providerName in preferredProviders)
{
    try
    {
        var session = new LearningModelSession(model, new LearningModelSessionOptions
        {
            ExecutionProvider = providerName
        });
        Debug.WriteLine($"✅ Using: {providerName}");
        break;
    }
    catch
    {
        Debug.WriteLine($"❌ {providerName} unavailable");
    }
}
```

### Solution 4: Fix Network (If Possible)

```powershell
# Allow package deployment through firewall
New-NetFirewallRule -DisplayName "Windows Package Manager" `
    -Program "%SystemRoot%\\System32\\svchost.exe" `
    -Service "InstallService" -Action Allow

# Or set proxy
netsh winhttp set proxy proxy.company.com:8080
```

## Diagnostic Steps

1. Check available providers: `FindAllAvailableExecutionProviders()`
2. If empty or missing TensorRT → Download blocked
3. Try DirectML (built-in) instead
4. If DirectML works → Corporate network issue confirmed

## Additional Resources
- [NVIDIA TensorRT](https://developer.nvidia.com/tensorrt)
- [DirectML Docs](https://learn.microsoft.com/windows/ai/directml/dml)

## Related TSGs
- [WinML Package Size](windows-ai-winml-package-size.md)
- [WinML Native AOT](windows-ai-winml-native-aot.md)
