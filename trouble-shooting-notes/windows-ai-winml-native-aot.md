# Troubleshooting Guide: WinML Native AOT and IR Version Limitations

## Symptom
WinML incompatibility issues:
- `PublishAot` fails with WinML
- ONNX models with IR version 10+ won't load
- Runtime errors combining Native AOT + WinML

## Affected Components
- **Microsoft.Windows.AI.MachineLearning**
- **ONNX Runtime** (built into SDK)

## Related Issues
- [#5882](https://github.com/microsoft/WindowsAppSDK/issues/5882) - WinML doesn't support Native AOT
- [#5706](https://github.com/microsoft/WindowsAppSDK/issues/5706) - Model IR version 10 not supported

## Root Cause

**WinML incompatible with Native AOT:**
- Uses WinRT COM interop (needs runtime type info)
- Dynamic provider loading
- ONNX Runtime uses reflection

**Built-in ONNX Runtime outdated:**
- Supports **IR version 9 max**
- Modern models use **IR version 10+**
- Cannot upgrade (tied to SDK version)

## Solutions

### Solution 1: Disable Native AOT

```xml
<PropertyGroup>
  <!-- ❌ DO NOT use with WinML -->
  <!-- <PublishAot>true</PublishAot> -->
  
  <!-- ✅ Use these instead -->
  <PublishReadyToRun>true</PublishReadyToRun>
  <SelfContained>true</SelfContained>
</PropertyGroup>
```

**Trade-off:** Slower startup (~100-300ms), but WinML works

### Solution 2: Use Standalone ONNX Runtime (for IR 10+)

```xml
<ItemGroup>
  <!-- ✅ Supports IR version 10+ -->
  <PackageReference Include="Microsoft.ML.OnnxRuntime" Version="1.18.0" />
  
  <!-- ❌ Don't use WinML for IR 10 models -->
  <!-- <PackageReference Include="Microsoft.WindowsAppSDK.ML" /> -->
</ItemGroup>
```

**Code:**
```csharp
using Microsoft.ML.OnnxRuntime;

// Works with IR 10+ models
var session = new InferenceSession("model_ir10.onnx");
```

### Solution 3: Downgrade Model to IR 9

```python
import onnx

model = onnx.load("model_ir10.onnx")
model.ir_version = 9
onnx.save(model, "model_ir9.onnx")
```

**Verify:**
```csharp
using Microsoft.Windows.AI.MachineLearning;

var model = LearningModel.LoadFromFilePath("model_ir9.onnx"); // Now works
```

## Diagnostic Steps

1. **Check model IR version:**
   ```python
   import onnx
   model = onnx.load("model.onnx")
   print(f"IR version: {model.ir_version}")
   ```

2. **Check Native AOT:**
   ```xml
   <!-- Search .csproj for: -->
   <PublishAot>true</PublishAot>
   ```

3. **Test with standalone ONNX Runtime:**
   - If works → IR version problem
   - If fails → Model issue

## Expected Outcomes

| Solution | Startup Time | IR Support |
|----------|-------------|-----------|
| Disable AOT | +100-300ms | IR 9 only |
| Standalone ONNX | +100-300ms | IR 10+ |
| Downgrade model | Normal | IR 9 only |

## Related TSGs
- [WinML Package Size](windows-ai-winml-package-size.md)
- [WinML Execution Provider Download](windows-ai-winml-execution-provider-download.md)
