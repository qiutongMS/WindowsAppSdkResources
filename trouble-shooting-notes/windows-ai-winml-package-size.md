# Troubleshooting Guide: WinML Package Size Issues

## Symptom
App package bloat after adding WinML:
- onnxruntime.dll (~21 MB) included when not using ML
- DirectML.dll (~18 MB) included when not using GPU
- Cannot exclude ML DLLs from build
- .NET trimming doesn't remove native libraries

## Affected Components
- **Microsoft.Windows.AI.MachineLearning**
- **Native DLLs**: onnxruntime.dll, DirectML.dll

## Related Issues
- [#6015](https://github.com/microsoft/WindowsAppSDK/issues/6015) - Native DLLs not trimmed
- [#5969](https://github.com/microsoft/WindowsAppSDK/issues/5969) - Cannot remove ML libraries

## Root Cause

**.NET trimming only affects managed code**, not native DLLs:
- ✅ Managed assemblies can be trimmed
- ❌ Native DLLs (onnxruntime, DirectML) always included

**Full SDK includes all components:**
- `Microsoft.WindowsAppSDK` metapackage includes WinML by default
- Even if you don't call ML APIs, DLLs are copied to output

## Solutions

### Solution 1: Use Granular Package References

Reference only needed components:

```xml
<ItemGroup>
  <!-- ❌ DON'T use full SDK if not using ML -->
  <!-- <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.8.*" /> -->
  
  <!-- ✅ Reference specific components -->
  <PackageReference Include="Microsoft.WindowsAppSDK.Foundation" Version="1.8.*" />
  <PackageReference Include="Microsoft.WindowsAppSDK.WinUI" Version="1.8.*" />
  
  <!-- ⚠️ Only add if using ML -->
  <!-- <PackageReference Include="Microsoft.WindowsAppSDK.ML" Version="1.8.*" /> -->
</ItemGroup>
```

**Saves:** ~40 MB

### Solution 2: Exclude ML DLLs from Output

```xml
<Target Name="RemoveMLDLLs" AfterTargets="Build">
  <ItemGroup>
    <MLDLLs Include="$(OutDir)**\onnxruntime.dll" />
    <MLDLLs Include="$(OutDir)**\DirectML.dll" />
    <MLDLLs Include="$(OutDir)**\onnxruntime_providers_*.dll" />
  </ItemGroup>
  
  <Delete Files="@(MLDLLs)" />
</Target>
```

### Solution 3: Verify Removal

```powershell
# Check DLL sizes in output
Get-ChildItem -Recurse -Filter "*.dll" | 
  Sort-Object Length -Descending | 
  Select-Object Name, @{N='MB';E={[math]::Round($_.Length/1MB,2)}} -First 10

# Verify ML DLLs removed
Get-ChildItem -Recurse | Where-Object { $_.Name -like "*onnx*" -or $_.Name -like "*DirectML*" }
```

## Diagnostic Steps

1. **Check package references:** Look for `Microsoft.WindowsAppSDK` vs granular packages
2. **Verify output directory:** ML DLLs present even if not used
3. **Test Solution 1:** Switch to granular references, rebuild
4. **Test Solution 2:** Add removal target if Solution 1 fails

## Expected Outcomes

| Solution | Size Reduction |
|----------|---------------|
| Granular packages | -40 MB |
| DLL exclusion | -40 MB |
| Both | -40 MB |

## Related TSGs
- [WinML Execution Provider Download](windows-ai-winml-execution-provider-download.md)
- [WinML Native AOT](windows-ai-winml-native-aot.md)
