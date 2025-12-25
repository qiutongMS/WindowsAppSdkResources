# GetReadyState / MakeAvailableAsync State and Performance Issues

**Error Codes:** None (behavior/state issues)  
**Affected Area:** WCR APIs - Model availability and state management  
**Common Platforms:** Intel Core Ultra, various NPU configurations

---

## Symptom Overview

Issues related to WCR API state management methods that check or ensure model availability. These include unexpected return values, hanging/timeout behaviors, and slow performance.

**You might see:**
- `GetReadyState()` returns `NotReady` even when model is actually available
- `MakeAvailableAsync()` hangs indefinitely or times out
- `GetReadyState()` doesn't update after `EnsureReadyAsync()` completes
- Extremely slow execution (20-100+ seconds) for state check calls
- Model works fine if you skip availability checks

---

## Related Issues

This troubleshooting guide consolidates multiple related reports:
- [#5982](https://github.com/microsoft/WindowsAppSDK/issues/5982) - GetReadyState returns NotReady but CreateAsync works
- [#5352](https://github.com/microsoft/WindowsAppSDK/issues/5352) - GetReadyState not updating after EnsureReadyAsync
- [#5200](https://github.com/microsoft/WindowsAppSDK/issues/5200) - MakeAvailableAsync hanging on Intel Core Ultra 5
- [#5487](https://github.com/microsoft/WindowsAppSDK/issues/5487) - GetReadyState takes 20-100 seconds on Intel

---

## Quick Diagnosis

Run through these checks to identify your specific scenario:

1. **Test if model actually works despite state check issues**
   ```csharp
   try {
       // Skip state checks and try creating directly
       var model = await LanguageModel.CreateAsync();
       var result = await model.GenerateResponseAsync("Test prompt");
       Console.WriteLine("Model works! State check is unreliable.");
   } catch (Exception ex) {
       Console.WriteLine($"Model actually unavailable: {ex.Message}");
   }
   ```
   → If model **works**, see [Scenario 1: State Check Returns False Negative](#scenario-1-state-check-returns-false-negative)

2. **Check platform and NPU type**
   ```powershell
   # Check CPU/NPU model
   Get-WmiObject Win32_Processor | Select-Object Name
   ```
   → If **Intel Core Ultra** series, see [Scenario 2: Intel Platform Slow State Checks](#scenario-2-intel-platform-slow-state-checks)

3. **Monitor for timeout vs actual hang**
   ```csharp
   var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
   try {
       var task = LanguageModel.MakeAvailableAsync().AsTask(cts.Token);
       await task;
   } catch (TaskCanceledException) {
       Console.WriteLine("Timed out - see Scenario 3");
   }
   ```
   → If consistently **times out/hangs**, see [Scenario 3: MakeAvailableAsync Hanging](#scenario-3-makeavailableasync-hanging)

---

## Common Scenarios & Solutions

### Scenario 1: State Check Returns False Negative

**Root Cause:** Known issue in WCR SDK where `GetReadyState()` and `IsAvailable()` may incorrectly report `NotReady` or `false`, but the model is actually functional. This appears to be a state caching or synchronization issue in the SDK.

**Related Issue(s):** [#5982](https://github.com/microsoft/WindowsAppSDK/issues/5982), [#5352](https://github.com/microsoft/WindowsAppSDK/issues/5352)

**Workaround:** Skip state checks and handle exceptions instead

**Option A: Optimistic Creation (Recommended)**
```csharp
try
{
    // Skip IsAvailable/GetReadyState checks entirely
    var languageModel = await LanguageModel.CreateAsync();
    var response = await languageModel.GenerateResponseAsync(prompt);
    return response.Text;
}
catch (COMException ex) when (ex.HResult == unchecked((int)0x80040154))
{
    // Model truly unavailable - handle gracefully
    Console.WriteLine("WCR model not available on this device");
    return await FallbackToCloudAPI(prompt);
}
catch (UnauthorizedAccessException)
{
    // Missing capability or MSIX identity - see Access Denied TSG
    throw;
}
```

**Option B: Use EnsureReadyAsync then Create**
```csharp
// EnsureReadyAsync is more reliable than GetReadyState
try
{
    var ensureResult = await LanguageModel.EnsureReadyAsync();
    // Don't check GetReadyState() here - it may still be wrong
    
    // Directly create and use
    var model = await LanguageModel.CreateAsync();
    // ... use model
}
catch (Exception ex)
{
    // Handle actual unavailability
}
```

**Verification:**
- Your app should now work even if state checks fail
- Log both state check results AND actual model creation success

---

### Scenario 2: Intel Platform Slow State Checks

**Root Cause:** On Intel Core Ultra (Series 1 and 2) processors, `GetReadyState()` can take 20-100+ seconds to complete. This appears related to NPU firmware initialization or Windows AI stack loading delays on Intel NPU architectures.

**Related Issue(s):** [#5487](https://github.com/microsoft/WindowsAppSDK/issues/5487)

**Workaround:** First-run initialization with user feedback

1. **Show loading UI for first call:**
```csharp
private static bool _modelInitialized = false;
private static readonly SemaphoreSlim _initLock = new(1, 1);

public async Task<bool> EnsureModelReadyAsync(IProgress<string> progress)
{
    if (_modelInitialized)
        return true;
    
    await _initLock.WaitAsync();
    try
    {
        if (_modelInitialized)
            return true;
        
        progress?.Report("Initializing AI model (first run may take up to 2 minutes)...");
        
        var sw = Stopwatch.StartNew();
        var readyState = LanguageModel.GetReadyState();
        sw.Stop();
        
        Debug.WriteLine($"GetReadyState took {sw.ElapsedMilliseconds}ms");
        
        _modelInitialized = true;
        progress?.Report("Model ready!");
        return readyState == AIFeatureReadyState.Ready;
    }
    finally
    {
        _initLock.Release();
    }
}
```

2. **Use timeout with fallback:**
```csharp
public async Task<LanguageModel?> GetModelWithTimeoutAsync()
{
    var checkTask = Task.Run(() => LanguageModel.GetReadyState());
    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
    
    var completedTask = await Task.WhenAny(checkTask, timeoutTask);
    
    if (completedTask == timeoutTask)
    {
        // State check too slow - just try creating
        Debug.WriteLine("State check timeout - attempting direct creation");
        return await LanguageModel.CreateAsync();
    }
    
    var state = await checkTask;
    if (state == AIFeatureReadyState.Ready)
    {
        return await LanguageModel.CreateAsync();
    }
    
    return null;
}
```

**Verification:**
- Subsequent calls should be faster (cached state)
- User sees progress feedback during slow first initialization

---

### Scenario 3: MakeAvailableAsync Hanging

**Root Cause:** `MakeAvailableAsync()` may hang indefinitely on Intel platforms, likely due to Windows Update or Package Deployment service issues. The underlying FOD (Feature on Demand) installation can stall.

**Related Issue(s):** [#5200](https://github.com/microsoft/WindowsAppSDK/issues/5200)

**Fix/Workaround:**

**Immediate Workaround - Skip MakeAvailable:**
```csharp
// Don't call MakeAvailableAsync at all
// if (!LanguageModel.IsAvailable())
// {
//     await LanguageModel.MakeAvailableAsync();  // DON'T DO THIS
// }

// Instead, just try to create
try
{
    var model = await LanguageModel.CreateAsync();
    // If this succeeds, model is available
}
catch
{
    // Model not available - show message to user
    ShowInstallInstructionsDialog();
}
```

**System-level Fix:**

1. **Check Windows Update service:**
```powershell
Get-Service -Name wuauserv, bits, dosvc | Select-Object Name, Status, StartType
# All should be Running

# If not running:
Start-Service wuauserv
```

2. **Clear Windows Update cache:**
```powershell
Stop-Service wuauserv, bits
Remove-Item C:\Windows\SoftwareDistribution\Download\* -Recurse -Force
Start-Service wuauserv, bits
```

3. **Manual AI component installation:**
```powershell
# Check current AI packages
Get-WindowsPackage -Online | Where-Object {$_.PackageName -like "*AI*"}

# Force Windows Update check
Start-Process "ms-settings:windowsupdate"
# Click "Check for updates"
```

4. **Reboot device:**
   - Often resolves Windows Update/Package Deployment stuck states

**Verification:**
```csharp
// Set a reasonable timeout
var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
var operation = LanguageModel.MakeAvailableAsync();

try
{
    var result = await operation.AsTask(cts.Token);
    Console.WriteLine($"Installation completed: {result.Status}");
}
catch (TaskCanceledException)
{
    Console.WriteLine("Installation timeout - manual intervention needed");
}
```

---

## Additional Context

### Known Issues Across SDK Versions

| SDK Version | Known State Issues |
|-------------|-------------------|
| 1.7-exp2 | GetReadyState false negatives begin |
| 1.7-exp3 | Same issues persist |
| 2.0-exp1 | Issues still present as of Dec 2025 |

### Platform-Specific Behavior

**Intel Core Ultra:**
- First `GetReadyState()` call: 20-100+ seconds (one-time initialization)
- `MakeAvailableAsync()`: May hang (Windows Update related)
- Subsequent calls: Usually fast (<1 second)

**Qualcomm Snapdragon X:**
- Generally faster state checks
- Fewer hanging issues with MakeAvailableAsync
- But still affected by false negative GetReadyState

**AMD Ryzen AI:**
- Similar to Intel (slow first call)
- MakeAvailableAsync more reliable

### Best Practices

1. **Don't rely on state checks for critical logic**
   - Use try/catch around CreateAsync instead
   
2. **Provide user feedback for first-run delays**
   - Show spinner/progress during initialization
   
3. **Cache model instances**
   - Create once, reuse throughout app lifetime
   
4. **Implement timeouts**
   - Never wait indefinitely for MakeAvailableAsync

---

## References

- [Issue #5982: GetReadyState returns NotReady when model available](https://github.com/microsoft/WindowsAppSDK/issues/5982)
- [Issue #5352: GetReadyState not updating after EnsureReadyAsync](https://github.com/microsoft/WindowsAppSDK/issues/5352)
- [Issue #5200: MakeAvailableAsync hanging on Intel](https://github.com/microsoft/WindowsAppSDK/issues/5200)
- [Issue #5487: GetReadyState slow performance](https://github.com/microsoft/WindowsAppSDK/issues/5487)
- [Windows AI APIs Documentation](https://learn.microsoft.com/windows/ai/apis/)

---

**Last Updated:** 2025-12-25  
**Confidence:** 0.82
