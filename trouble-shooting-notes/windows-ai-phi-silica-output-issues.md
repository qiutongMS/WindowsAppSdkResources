# Troubleshooting Guide: Phi Silica Output Garbled or Empty

## Symptom
`LanguageModel.GenerateResponseAsync()` produces incorrect or missing output:
- Response text contains only "####" characters
- Output shows garbled characters (Chinese, Japanese, Korean)
- Response is completely empty or blank
- Progress callback never called or always empty

## Affected Components
- **Microsoft.Windows.AI.Generative.LanguageModel** (Phi Silica)
- **Windows App SDK**: 1.8-experimental2 (encoding bug)

## Related Issues
- [#5517](https://github.com/microsoft/WindowsAppSDK/issues/5517) - Double byte content causes garbled output
- [#5551](https://github.com/microsoft/WindowsAppSDK/issues/5551) - Response shows only "####"
- [#5499](https://github.com/microsoft/WindowsAppSDK/issues/5499) - No visible response, delta always blank

## Root Cause

**Windows App SDK 1.8 Experimental 2** has Unicode encoding bug affecting:
- Chinese, Japanese, Korean text → Garbled or "####"
- Empty responses → Progress callbacks not triggered
- SDK version bug in character marshaling

## Solutions

### Solution 1: Upgrade SDK (Recommended)

```xml
<ItemGroup>
  <!-- ❌ AVOID - Has Unicode bugs -->
  <!-- <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.8.*-experimental2" /> -->
  
  <!-- ✅ UPGRADE to experimental3+ -->
  <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.8.*-experimental3" />
</ItemGroup>
```

### Solution 2: Validate Response

```csharp
public static bool IsResponseValid(string response)
{
    if (string.IsNullOrWhiteSpace(response)) return false;
    if (response.All(c => c == '#')) return false; // Encoding bug
    if (response.Contains('\uFFFD')) return false; // Replacement char
    return true;
}

// Usage
var response = await model.GenerateResponseAsync(prompt);
if (!IsResponseValid(response.Text))
{
    throw new Exception("Unicode encoding bug - upgrade SDK to experimental3+");
}
```

### Solution 3: Use Progress Callbacks

```csharp
var output = new StringBuilder();
int callbackCount = 0;

var operation = model.GenerateResponseAsync(prompt);
operation.Progress = (_, delta) =>
{
    callbackCount++;
    if (!string.IsNullOrEmpty(delta))
        output.Append(delta);
};

var response = await operation;

// If Response.Text empty but callbacks fired, use callback output
if (string.IsNullOrEmpty(response.Text) && output.Length > 0)
{
    return output.ToString();
}
```

### Solution 4: Test Language-Specific

```csharp
// Quick diagnostic
var englishOK = await model.GenerateResponseAsync("Hello");
var chineseOK = await model.GenerateResponseAsync("你好");

if (IsResponseValid(englishOK.Text) && !IsResponseValid(chineseOK.Text))
{
    // Unicode encoding bug confirmed - upgrade SDK
}
```

## SDK Version Compatibility

| SDK Version | English | Unicode | Status |
|-------------|---------|---------|--------|
| 1.8-exp2 | ✅ | ❌ Garbled | Buggy |
| 1.8-exp3+ | ✅ | ✅ | Fixed |

## Diagnostic Checklist

- [ ] Test with English prompt - works?
- [ ] Test with Unicode prompt (Chinese/Japanese/Korean) - fails?
- [ ] Check SDK version - is it experimental2?
- [ ] Response shows "####" → Encoding bug
- [ ] Progress callbacks never fire → SDK bug

**If all checks point to encoding bug:** Upgrade to 1.8-experimental3+

## Additional Resources
- [Windows App SDK Release Notes](https://learn.microsoft.com/windows/apps/windows-app-sdk/release-channels)

## Related TSGs
- [Phi Silica Initialization Errors](windows-ai-phi-silica-initialization-errors.md)
