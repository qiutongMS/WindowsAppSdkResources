# WinML Execution Provider Download and Registration Errors

**Error Codes:** `0x80073D3B`, `5005`  
**Affected Area:** Windows ML, Execution Providers (NPU/GPU)  
**Common Platforms:** All platforms, particularly Snapdragon/Intel/AMD hardware

---

## Symptom Overview

When attempting to download or register Windows ML execution providers (such as OpenVINO, TensorRT, or QNN), you encounter errors preventing the automatic download via `ensure_and_register_certified_async().get()` or runtime failures with resource errors during model execution.

**You might see:**
- Error: "The product is not applicable or cannot be found" (0x80073D3B)
- Error: "Out of resources" (5005) when executing Large Language Models
- Execution providers found by `find_all_providers()` but not downloaded
- NvTensorRTRTXExecutionProvider not downloaded automatically
- Deployment operations failing on specific execution provider packages

---

## Related Issues

This troubleshooting guide consolidates multiple related reports:
- [#6072](https://github.com/microsoft/WindowsAppSDK/issues/6072) - NvTensorRTRTXExecutionProvider not downloaded automatically
- [#5862](https://github.com/microsoft/WindowsAppSDK/issues/5862) - Fetching Execution provider ends with "product not found" error
- [#5858](https://github.com/microsoft/WindowsAppSDK/issues/5858) - Known issue: WinML QNN hits 5005 out of resources error with LLMs

---

## Quick Diagnosis

Run through these checks to identify your specific scenario:

1. **Check if execution provider appears in available list**
   ```python
   # In Python
   from onnxruntime import find_all_providers
   print(find_all_providers())
   ```
   → If provider IS listed but not downloaded, see [Scenario 1](#scenario-1-execution-provider-found-but-not-downloaded)

2. **Check for 0x80073D3B error during registration**
   ```
   Error: Deployment Add operation...failed with error 0x80073D3B
   PackageUri: uup://Product/Windows.Workload.ExecutionProvider.*
   ```
   → See [Scenario 2](#scenario-2-product-not-applicable-or-cannot-be-found-error)

3. **Check for 5005 resource error during LLM execution**
   ```
   Error 5005: Out of resources when executing model
   ```
   → See [Scenario 3](#scenario-3-qnn-out-of-resources-error-with-llms)

4. **Verify your hardware and driver configuration**
   ```powershell
   # Check GPU driver version
   Get-WmiObject Win32_VideoController | Select-Object Name, DriverVersion
   ```

---

## Common Scenarios & Solutions

### Scenario 1: Execution Provider Found but Not Downloaded (NVIDIA TensorRT)

**Root Cause:** The `ensure_and_register_certified_async().get()` function may not automatically download certain execution providers (like NvTensorRTRTXExecutionProvider) even when CUDA and appropriate drivers are installed, though the provider appears in `find_all_providers()`.

**Related Issue(s):** [#6072](https://github.com/microsoft/WindowsAppSDK/issues/6072)

**Environment:**
- RTX 4090 (or other NVIDIA GPUs)
- CUDA 12.9 installed
- Driver 576.88 or similar
- Windows 11 24H2

**Current Status:** Under investigation - provider appears available but download doesn't trigger

**Potential Workaround:** Manual installation

1. **Verify CUDA installation**
   ```powershell
   nvcc --version
   # Should show CUDA 12.9
   ```

2. **Check if provider is already available locally**
   ```python
   from onnxruntime import get_available_providers
   print(get_available_providers())
   # Check if 'TensorrtExecutionProvider' appears
   ```

3. **Try explicit provider specification**
   ```python
   import onnxruntime as ort
   
   # Create session with explicit TensorRT provider
   providers = [
       ('TensorrtExecutionProvider', {
           'trt_max_workspace_size': 2147483648,
           'trt_fp16_enable': True,
       }),
       'CUDAExecutionProvider',
       'CPUExecutionProvider'
   ]
   
   session = ort.InferenceSession('model.onnx', providers=providers)
   ```

**Verification:**
```python
# Check which providers are actually used
print(session.get_providers())
# Should show TensorrtExecutionProvider if successful
```

---

### Scenario 2: "Product Not Applicable or Cannot be Found" Error (0x80073D3B)

**Root Cause:** The UUP (Unified Update Platform) package deployment for execution providers (particularly OpenVINO) fails due to enterprise domain restrictions, network policies, or Windows Update service issues.

**Related Issue(s):** [#5862](https://github.com/microsoft/WindowsAppSDK/issues/5862)

**Environment:**
- Enterprise domain-joined machines
- Windows 11 24H2
- Unpackaged or Framework-Dependent deployment
- Attempting to download OpenVINO EP

**Error Details:**
```
C:\__w\1\s\dev\PackageManager\API\M.W.M.D.PackageDeploymentManager.cpp(1989):
ReturnHr(1) tid(7858) 80073D3B The product is not applicable or cannot be found.

ExtendedError:0x80073D3B 
PackageFamilyName:MicrosoftCorporationII.WinML.Intel.OpenVINO.EP.1.8_8wekyb3d8bbwe 
PackageUri:uup://Product/Windows.Workload.ExecutionProvider.OpenVino.amd64
```

**Fix Option 1: Check Windows Update and Store services**

1. **Ensure Windows Update service is running**
   ```powershell
   Get-Service -Name wuauserv | Start-Service
   Get-Service -Name wuauserv | Select-Object Name, Status
   ```

2. **Check Microsoft Store connectivity**
   ```powershell
   # Run WSReset to reset Store cache
   wsreset.exe
   ```

3. **Verify Group Policy restrictions**
   ```powershell
   # Check if Windows Update is restricted
   Get-ItemProperty -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate" -ErrorAction SilentlyContinue
   ```

**Fix Option 2: Use packaged deployment**

Convert application to MSIX packaging which may have better UUP access:
```xml
<!-- Add to .csproj -->
<PropertyGroup>
  <WindowsPackageType>MSIX</WindowsPackageType>
  <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
</PropertyGroup>
```

**Fix Option 3: Check network and proxy settings**

If on enterprise network:
1. Verify proxy settings allow access to Windows Update endpoints
2. Check firewall rules for UUP protocol
3. Contact IT administrator for Windows Store access permissions

---

### Scenario 3: QNN Out of Resources Error with LLMs (5005)

**Root Cause:** When executing large language models using the Qualcomm QNN execution provider on NPU hardware, the system runs out of resources due to driver limitations. This is a known issue affecting WinAppSDK 1.8.1.

**Related Issue(s):** [#5858](https://github.com/microsoft/WindowsAppSDK/issues/5858)

**Environment:**
- Qualcomm Snapdragon processors with NPU
- Large language models (LLMs)
- Windows App SDK 1.8.1

**Fix:** Install latest Qualcomm NPU driver

1. **Download latest universal NPU driver**
   - Visit [developer.qualcomm.com](https://developer.qualcomm.com/)
   - Navigate to Snapdragon drivers section
   - Download the latest universal NPU driver package

2. **Install the driver**
   ```powershell
   # Run the downloaded installer with admin privileges
   # Follow Qualcomm's installation instructions
   ```

3. **Restart the system**
   ```powershell
   Restart-Computer
   ```

4. **Verify driver version**
   ```powershell
   Get-WmiObject Win32_PnPSignedDriver | 
     Where-Object { $_.DeviceName -like "*Qualcomm*" -or $_.DeviceName -like "*NPU*" } |
     Select-Object DeviceName, DriverVersion, DriverDate
   ```

**Alternative Workaround:** Use different execution provider

If NPU driver update doesn't resolve the issue:
```python
# Fall back to CPU or GPU execution provider
providers = [
    'DmlExecutionProvider',  # DirectML for GPU
    'CPUExecutionProvider'
]

session = ort.InferenceSession('llm_model.onnx', providers=providers)
```

**Verification:**
```python
# Test model execution
result = session.run(None, {input_name: input_data})
# Should complete without 5005 error
```

---

## Additional Context

### Execution Provider Download Mechanism

Windows ML uses the Windows Package Deployment Manager to download execution provider packages from Microsoft's UUP infrastructure. This requires:
- Active internet connection
- Access to Windows Update endpoints
- Appropriate permissions (may require admin or specific capabilities)
- Windows 11 24H2 or later for certain providers

### Supported Execution Providers

- **CPU**: Always available, no download required
- **DirectML (DML)**: GPU acceleration, included with Windows
- **CUDA**: NVIDIA GPUs, requires CUDA toolkit
- **TensorRT**: NVIDIA GPUs, optimized inference
- **OpenVINO**: Intel hardware acceleration
- **QNN**: Qualcomm NPU acceleration

### Enterprise Environment Considerations

In enterprise/domain environments:
- UUP access may be restricted by Group Policy
- Windows Store may be disabled
- Network proxies may block update endpoints
- Consider pre-deployment of execution provider packages

---

## Related Documentation

- [Windows ML Documentation](https://learn.microsoft.com/windows/ai/windows-ml/)
- [ONNX Runtime Execution Providers](https://onnxruntime.ai/docs/execution-providers/)
- [Qualcomm Developer Portal](https://developer.qualcomm.com/)
- [NVIDIA TensorRT Documentation](https://developer.nvidia.com/tensorrt)

---

**Last Updated:** January 5, 2026  
**Confidence Score:** 0.85  
**Status:** Known issue for QNN (5005); Investigation ongoing for TensorRT download; Enterprise restrictions for OpenVINO
