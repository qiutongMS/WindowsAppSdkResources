# WCR GetReadyState False Negative - Model Available But Reports Not Ready

**Error Codes:** None (behavior issue)  
**Affected Area:** WCR APIs - State management false negatives  
**Common Platforms:** All platforms (Intel Core Ultra, Snapdragon X, AMD Ryzen AI)

---

## Symptom Overview

`GetReadyState()` and `IsAvailable()` incorrectly report that WCR models are not ready or unavailable, but calling `CreateAsync()` succeeds and the model works perfectly.

**You might see:**
- `LanguageModel.GetReadyState()` returns `NotReady`
- `LanguageModel.IsAvailable()` returns `false`
- BUT `var model = await LanguageModel.CreateAsync()` works fine
- State check doesn't update even after `EnsureReadyAsync()` completes successfully

---

## Related Issues

- [#5982](https://github.com/microsoft/WindowsAppSDK/issues/5982) - GetReadyState returns NotReady but CreateAsync works
- [#5352](https://github.com/microsoft/WindowsAppSDK/issues/5352) - GetReadyState not updating after EnsureReadyAsync

---

## Root Cause

Known SDK bug where state management APIs have synchronization or caching issues. The internal state check doesn't accurately reflect actual model availability. This affects **all SDK versions including 2.0-exp1** (as of Jan 2026).

**Technical Details:**
- Appears to be state cache not invalidating properly
- May be related to Windows AI stack initialization timing
- State check queries different system component than CreateAsync
- Issue reproduced across Intel, AMD, and Qualcomm platforms

---

## Solution

### Recommended: Optimistic Creation Pattern

**Don't check state** - just try to create and handle exceptions:

```csharp
public async Task<string> GenerateTextAsync(string prompt)
{
    try
    {
        // Skip IsAvailable/GetReadyState checks entirely
        var model = await LanguageModel.CreateAsync();
        var response = await model.GenerateResponseAsync(prompt);
        return response.Text;
    }
    catch (COMException ex) when (ex.HResult == unchecked((int)0x80040154))
    {
        // Model genuinely unavailable (CLASS_E_CLASSNOTAVAILABLE)
        Console.WriteLine("WCR not available on this device");
        throw new PlatformNotSupportedException("WCR language model unavailable");
    }
    catch (UnauthorizedAccessException)
    {
        // Missing systemAIModels capability or MSIX identity
        throw new InvalidOperationException("Missing required app capabilities - see Access Denied TSG");
    }
}
```

---

### Alternative: EnsureReadyAsync Then Create

If you must check state, use `EnsureReadyAsync` (more reliable than `GetReadyState`):

```csharp
public async Task<LanguageModel?> GetLanguageModelAsync()
{
    try
    {
        // EnsureReadyAsync is more reliable
        var ensureResult = await LanguageModel.EnsureReadyAsync();
        
        // ⚠️ WARNING: Don't check GetReadyState() here - it may still be wrong!
        // Just proceed to create:
        
        var model = await LanguageModel.CreateAsync();
        return model;
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"Model creation failed: {ex.Message}");
        return null;
    }
}
```

---

### Pattern for Feature Detection

If you need to enable/disable UI based on availability:

```csharp
private bool _wcrAvailable = false;

public async Task InitializeAsync()
{
    // Probe once at startup
    try
    {
        var model = await LanguageModel.CreateAsync();
        model.Dispose();
        _wcrAvailable = true;
        
        // Enable WCR features in UI
        EnableWCRFeatures();
    }
    catch
    {
        _wcrAvailable = false;
        
        // Show "WCR not available" message
        ShowWCRUnavailableMessage();
    }
}

private void OnGenerateButtonClick()
{
    if (!_wcrAvailable)
    {
        ShowError("WCR not available on this device");
        return;
    }
    
    // Don't check state again - just use it
    await GenerateTextAsync(userPrompt);
}
```

---

### Cached Model Instance

Create once and reuse (avoids repeated state checks):

```csharp
private LanguageModel? _cachedModel;
private readonly SemaphoreSlim _modelLock = new(1, 1);

public async Task<LanguageModel> GetOrCreateModelAsync()
{
    if (_cachedModel != null)
        return _cachedModel;
    
    await _modelLock.WaitAsync();
    try
    {
        if (_cachedModel != null)
            return _cachedModel;
        
        // No state check - just create
        _cachedModel = await LanguageModel.CreateAsync();
        return _cachedModel;
    }
    finally
    {
        _modelLock.Release();
    }
}

// Usage:
var model = await GetOrCreateModelAsync();
var response = await model.GenerateResponseAsync(prompt);
```

---

## Verification

### Test That State Check Is Wrong

```csharp
// 1. Check state
var readyState = LanguageModel.GetReadyState();
var isAvailable = LanguageModel.IsAvailable();

Console.WriteLine($"GetReadyState: {readyState}");  // May say NotReady
Console.WriteLine($"IsAvailable: {isAvailable}");   // May say false

// 2. Try creating anyway
try
{
    var model = await LanguageModel.CreateAsync();
    Console.WriteLine("✅ Model created successfully despite state checks!");
    
    var test = await model.GenerateResponseAsync("Test");
    Console.WriteLine($"✅ Model generated response: {test.Text}");
    
    // This proves state check is unreliable
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Model actually unavailable: {ex.Message}");
}
```

Expected output showing the bug:
```
GetReadyState: NotReady
IsAvailable: False
✅ Model created successfully despite state checks!
✅ Model generated response: This is a test response...
```

---

## Common Mistakes to Avoid

### ❌ Mistake 1: Blocking on State Check

```csharp
// WRONG - blocks feature if state check is wrong
if (!LanguageModel.IsAvailable())
{
    ShowError("WCR not available");
    return;  // May be wrong!
}
```

### ❌ Mistake 2: Looping Until State Is Ready

```csharp
// WRONG - may loop forever due to bug
while (LanguageModel.GetReadyState() != AIFeatureReadyState.Ready)
{
    await Task.Delay(1000);
}
```

### ❌ Mistake 3: Checking State Before Every Use

```csharp
// WRONG - checking state repeatedly
private async Task GenerateAsync(string prompt)
{
    if (LanguageModel.GetReadyState() != AIFeatureReadyState.Ready)
        throw new Exception("Not ready");
    
    var model = await LanguageModel.CreateAsync();
    // ...
}
```

---

### ✅ Correct Patterns

```csharp
// CORRECT - optimistic creation
private async Task GenerateAsync(string prompt)
{
    var model = await LanguageModel.CreateAsync();
    var response = await model.GenerateResponseAsync(prompt);
    return response.Text;
}

// CORRECT - probe once, cache result
private bool? _isAvailable;

private async Task<bool> IsWCRAvailable()
{
    if (_isAvailable.HasValue)
        return _isAvailable.Value;
    
    try
    {
        var model = await LanguageModel.CreateAsync();
        model.Dispose();
        _isAvailable = true;
    }
    catch
    {
        _isAvailable = false;
    }
    
    return _isAvailable.Value;
}
```

---

## Logging for Bug Reports

If you want to report this issue, include:

```csharp
var sb = new StringBuilder();
sb.AppendLine($"SDK Version: {typeof(LanguageModel).Assembly.GetName().Version}");
sb.AppendLine($"OS Build: {Environment.OSVersion}");

var sw = Stopwatch.StartNew();
var readyState = LanguageModel.GetReadyState();
sw.Stop();

sb.AppendLine($"GetReadyState: {readyState} (took {sw.ElapsedMilliseconds}ms)");
sb.AppendLine($"IsAvailable: {LanguageModel.IsAvailable()}");

try
{
    var model = await LanguageModel.CreateAsync();
    sb.AppendLine("CreateAsync: ✅ SUCCESS (despite state check)");
    
    var test = await model.GenerateResponseAsync("Test");
    sb.AppendLine($"GenerateResponseAsync: ✅ SUCCESS ({test.Text.Length} chars)");
}
catch (Exception ex)
{
    sb.AppendLine($"CreateAsync: ❌ FAILED - {ex.GetType().Name}: {ex.Message}");
}

Debug.WriteLine(sb.ToString());
// Copy this to GitHub issue
```

---

## SDK Version Status

| SDK Version | GetReadyState Bug Status |
|-------------|--------------------------|
| 1.7-exp2 | ⚠️ Bug present |
| 1.7-exp3 | ⚠️ Bug present |
| 2.0-exp1 | ⚠️ Bug still present (as of Jan 2026) |
| Future | Awaiting fix from Microsoft |

**Workaround:** Use optimistic creation pattern indefinitely until Microsoft fixes the SDK.

---

## When to Report This

You should report/upvote this issue if:
- GetReadyState consistently returns NotReady
- But CreateAsync consistently succeeds
- On same device/app without changes
- Across multiple app launches

Don't report if:
- State check occasionally wrong (timing issue)
- CreateAsync sometimes fails too (actual unavailability)

---

## References

- [Issue #5982: GetReadyState returns NotReady when model available](https://github.com/microsoft/WindowsAppSDK/issues/5982)
- [Issue #5352: GetReadyState not updating after EnsureReadyAsync](https://github.com/microsoft/WindowsAppSDK/issues/5352)
- [Windows AI APIs Documentation](https://learn.microsoft.com/windows/ai/apis/)

---

**Last Updated:** 2026-01-04  
**Confidence:** 0.88

## Changelog

**2026-01-04:**
- Split from state_management_issues.md
- Enhanced optimistic creation pattern examples
- Added logging template for bug reports
