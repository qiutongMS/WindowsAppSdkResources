# WinML Model IR Version Compatibility Issues

**Error Codes:** N/A (Unsupported IR Version)  
**Affected Area:** Windows ML, ONNX Model Loading  
**Common Platforms:** All platforms using Windows ML

---

## Symptom Overview

When attempting to load an ONNX model with a newer Intermediate Representation (IR) version, the model fails to load with an error indicating the IR version is not supported. This prevents newer ONNX models from being used with Windows ML.

**You might see:**
- Error: "Unsupported model IR version: 10, max supported IR version: 9"
- Crash or exception when calling `LearningModelSession` constructor
- Error location: `\core\graph\model.cc:180 onnxruntime::Model::Model`
- Model loads in other ONNX runtimes but not in Windows ML

---

## Related Issues

This troubleshooting guide consolidates:
- [#5706](https://github.com/microsoft/WindowsAppSDK/issues/5706) - Windows::AI::MachineLearning::LearningModel support for model IR version 10

---

## Quick Diagnosis

1. **Check your ONNX model's IR version**
   ```python
   import onnx
   
   model = onnx.load("your_model.onnx")
   print(f"IR Version: {model.ir_version}")
   # If output is 10 or higher, Windows ML doesn't support it yet
   ```

2. **Check Windows App SDK version**
   ```xml
   <!-- In .csproj or check installed version -->
   <PackageReference Include="Microsoft.WindowsAppSDK" Version="?" />
   ```
   → Windows App SDK 1.7 and earlier support up to IR version 9

3. **Verify the error message**
   ```
   Exception: Unsupported model IR version: 10, max supported IR version: 9
   ```

---

## Common Scenarios & Solutions

### Scenario 1: ONNX Model with IR Version 10+

**Root Cause:** Windows ML in Windows App SDK 1.7 and earlier uses an older version of ONNX Runtime that only supports IR version 9 and below. Newer models exported with recent ONNX versions default to IR version 10.

**Related Issue(s):** [#5706](https://github.com/microsoft/WindowsAppSDK/issues/5706)

**Environment:**
- Windows App SDK 1.7 or earlier
- ONNX model with IR version 10
- Windows 11 24H2
- Any hardware platform

**Fix Option 1: Convert model to IR version 9**

1. **Using Python ONNX tools**
   ```python
   import onnx
   from onnx import version_converter
   
   # Load the original model
   model = onnx.load("ir10_model.onnx")
   
   # Convert to IR version 9
   converted_model = version_converter.convert_version(model, 9)
   
   # Save the converted model
   onnx.save(converted_model, "ir9_model.onnx")
   
   # Verify the conversion
   verify_model = onnx.load("ir9_model.onnx")
   print(f"Converted IR Version: {verify_model.ir_version}")
   ```

2. **Verify compatibility**
   ```python
   # Check if model is valid after conversion
   onnx.checker.check_model(converted_model)
   print("Model is valid!")
   ```

3. **Use the converted model in Windows ML**
   ```cpp
   // C++/WinRT
   auto model = Windows::AI::MachineLearning::LearningModel::LoadFromFilePath(
       L"ir9_model.onnx"
   );
   
   auto device = Windows::AI::MachineLearning::LearningModelDevice(
       Windows::AI::MachineLearning::LearningModelDeviceKind::Default
   );
   
   auto session = Windows::AI::MachineLearning::LearningModelSession(model, device);
   // Should succeed now
   ```

**Verification:**
```cpp
// Verify model loaded successfully
auto modelDescription = model.Description();
auto modelAuthor = model.Author();
std::wcout << L"Model loaded: " << modelDescription.c_str() << std::endl;
```

---

**Fix Option 2: Re-export model with IR version 9**

If you have access to the original model training code:

1. **Using PyTorch**
   ```python
   import torch
   import torch.onnx
   
   # Your PyTorch model
   model = YourModel()
   model.eval()
   
   # Dummy input
   dummy_input = torch.randn(1, 3, 224, 224)
   
   # Export with specific opset version that maps to IR 9
   torch.onnx.export(
       model,
       dummy_input,
       "model_ir9.onnx",
       export_params=True,
       opset_version=17,  # Opset 17 uses IR version 9
       do_constant_folding=True,
       input_names=['input'],
       output_names=['output']
   )
   ```

2. **Using TensorFlow**
   ```python
   import tensorflow as tf
   import tf2onnx
   
   # Your TensorFlow model
   model = tf.keras.models.load_model("model.h5")
   
   # Convert to ONNX with specific opset
   spec = (tf.TensorSpec((None, 224, 224, 3), tf.float32, name="input"),)
   model_proto, _ = tf2onnx.convert.from_keras(
       model,
       input_signature=spec,
       opset=17  # Maps to IR version 9
   )
   
   # Save
   with open("model_ir9.onnx", "wb") as f:
       f.write(model_proto.SerializeToString())
   ```

**Verification:**
```python
# Verify the exported model
model = onnx.load("model_ir9.onnx")
print(f"IR Version: {model.ir_version}")  # Should be 9 or less
print(f"Opset Version: {model.opset_import[0].version}")
```

---

**Fix Option 3: Wait for Windows App SDK update (Future)**

**Current Status:** Under investigation - Windows App SDK needs to update its bundled ONNX Runtime version to support IR version 10+.

Monitor these for updates:
- [Windows App SDK Release Notes](https://learn.microsoft.com/windows/apps/windows-app-sdk/release-notes)
- [GitHub Issue #5706](https://github.com/microsoft/WindowsAppSDK/issues/5706)

---

## Understanding ONNX IR Versions

### IR Version to Opset Mapping

| IR Version | Max Opset | ONNX Version | Supported in WinML |
|------------|-----------|--------------|-------------------|
| 9          | 19        | 1.14.x       | ✅ Yes (SDK 1.7+) |
| 10         | 21        | 1.16.x       | ❌ No (as of SDK 1.8) |

### What Changed in IR Version 10

IR version 10 introduced:
- Support for new operators in Opset 20 and 21
- Enhanced model metadata capabilities
- Improved type system for tensor shapes
- Better support for training models (not just inference)

### Compatibility Considerations

**Note:** Converting from IR 10 → IR 9 may cause issues if:
- Your model uses operators introduced in Opset 20 or 21
- The model relies on IR 10-specific features
- Complex custom operators are present

**Always test converted models thoroughly** to ensure accuracy is maintained.

---

## Workaround Checklist

Before loading a model in Windows ML:

- [ ] Check model IR version (`model.ir_version`)
- [ ] If IR ≥ 10, convert to IR 9 or re-export
- [ ] Validate converted model with `onnx.checker.check_model()`
- [ ] Test converted model accuracy against original
- [ ] Update model documentation with IR version info
- [ ] Consider model quantization or optimization during re-export

---

## Additional Context

### Tools for Model Conversion

1. **ONNX Python Package**
   ```bash
   pip install onnx onnx-simplifier
   ```

2. **Netron** (Visual model inspector)
   - [https://netron.app/](https://netron.app/)
   - Can view IR version and model structure

3. **ONNX Model Zoo**
   - Pre-converted models: [https://github.com/onnx/models](https://github.com/onnx/models)

### Performance Considerations

Converting to older IR versions may:
- ✅ Enable compatibility with Windows ML
- ✅ Maintain model accuracy (in most cases)
- ⚠️ Prevent use of latest optimizations
- ⚠️ Require testing for operator compatibility

---

## Related Documentation

- [ONNX Versioning](https://github.com/onnx/onnx/blob/main/docs/Versioning.md)
- [ONNX Operator Schemas](https://github.com/onnx/onnx/blob/main/docs/Operators.md)
- [Windows ML Model Compatibility](https://learn.microsoft.com/windows/ai/windows-ml/onnx-versions)
- [ONNX Model Conversion Guide](https://github.com/onnx/tutorials)

---

**Last Updated:** January 5, 2026  
**Confidence Score:** 0.90  
**Status:** Workaround available (model conversion); Awaiting Windows App SDK update for native IR 10 support
