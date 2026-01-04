# WCR MakeAvailableAsync Hanging and Slow State Checks

**Error Codes:** None (performance/hanging issue)  
**Affected Area:** WCR APIs - Slow initialization and hanging downloads  
**Common Platforms:** Intel Core Ultra series (Series 1, Series 2)

---

## Symptom Overview

WCR state management APIs exhibit severe performance issues or hang indefinitely on Intel platforms.

**You might see:**
- `GetReadyState()` takes 20-100+ seconds on first call
- `MakeAvailableAsync()` hangs forever (never completes)
- App appears frozen during WCR initialization
- Subsequent calls faster, but first call extremely slow

---

## Related Issues

- [#5487](https://github.com/microsoft/WindowsAppSDK/issues/5487) - GetReadyState takes 20-100 seconds on Intel
- [#5200](https://github.com/microsoft/WindowsAppSDK/issues/5200) - MakeAvailableAsync hanging on Intel Core Ultra 5

---

## Quick Diagnosis

### Check if you have Intel NPU platform:

```powershell
Get-WmiObject Win32_Processor | Select-Object Name
```

**Affected processors:**
- Intel Core Ultra 5 (Series 1: 125H, 135H, etc.)
- Intel Core Ultra 7 (Series 1: 155H, 165H, etc.)
- Intel Core Ultra 9 (Series 1: 185H)
- Intel Core Ultra 5/7/9 (Series 2: 200 series)

If you have Intel Core Ultra → likely affected by this issue.

---

## Two Distinct Problems

This TSG addresses **two related issues**:

### Problem 1: Slow First GetReadyState Call

- **Symptom:** First `GetReadyState()` takes 20-100 seconds
- **Cause:** NPU/Windows AI stack initialization delay
- **Workaround:** Show loading UI, cache result
- **See:** [Scenario 1](#scenario-1-slow-first-getreadystate-call)

### Problem 2: MakeAvailableAsync Hanging Forever

- **Symptom:** `MakeAvailableAsync()` never completes
- **Cause:** Windows Update / FOD installation stuck
- **Workaround:** Skip MakeAvailable, use CreateAsync directly
- **See:** [Scenario 2](#scenario-2-makeavailableasync-hanging)

---

## Scenario 1: Slow First GetReadyState Call

### Root Cause

On Intel Core Ultra processors, the first `GetReadyState()` call triggers NPU firmware initialization and Windows AI stack loading. This one-time initialization can take 20-100+ seconds. Subsequent calls are fast (<1 second).

**Why Intel-specific?**
- Intel NPU driver architecture requires lazy initialization
- First call loads firmware from disk into NPU memory
- Windows AI DLLs are loaded on-demand
- Other platforms (Snapdragon, AMD) have different initialization patterns

---

### Solution: First-Run Loading UI

**Option A: Show Progress During First Call**

```csharp
private static bool _modelInitialized = false;
private static readonly SemaphoreSlim _initLock = new(1, 1);

public async Task<bool> EnsureModelReadyAsync(IProgress<string> progress)
{
    if (_modelInitialized)
        return true;  // Fast path for subsequent calls
    
    await _initLock.WaitAsync();
    try
    {
        if (_modelInitialized)
            return true;
        
        progress?.Report("Initializing AI model (first run may take up to 2 minutes)...");
        
        var sw = Stopwatch.StartNew();
        
        // This may take 20-100+ seconds on Intel platforms
        var readyState = LanguageModel.GetReadyState();
        
        sw.Stop();
        Debug.WriteLine($"GetReadyState took {sw.Elapsed.TotalSeconds:F1} seconds");
        
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

**Usage in UI:**
```csharp
private async void OnFirstAIFeatureUsed()
{
    var progress = new Progress<string>(msg => 
    {
        StatusText.Text = msg;  // Update UI
    });
    
    ShowLoadingSpinner();
    
    var ready = await EnsureModelReadyAsync(progress);
    
    HideLoadingSpinner();
    
    if (ready)
    {
        // Proceed with WCR features
    }
}
```

---

**Option B: Timeout with Fallback to CreateAsync**

```csharp
public async Task<LanguageModel?> GetModelWithTimeoutAsync()
{
    var checkTask = Task.Run(() => LanguageModel.GetReadyState());
    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
    
    var completedTask = await Task.WhenAny(checkTask, timeoutTask);
    
    if (completedTask == timeoutTask)
    {
        // State check too slow - just try creating directly
        Debug.WriteLine("GetReadyState timeout - attempting direct creation");
        
        try
        {
            return await LanguageModel.CreateAsync();
        }
        catch
        {
            return null;
        }
    }
    
    // State check completed within timeout
    var state = await checkTask;
    if (state == AIFeatureReadyState.Ready)
    {
        return await LanguageModel.CreateAsync();
    }
    
    return null;
}
```

---

**Option C: Background Initialization on App Start**

```csharp
// In App.xaml.cs or main window constructor
public MainWindow()
{
    InitializeComponent();
    
    // Start initialization in background (don't await)
    _ = Task.Run(async () =>
    {
        try
        {
            Debug.WriteLine("Pre-initializing WCR in background...");
            var state = LanguageModel.GetReadyState();
            Debug.WriteLine($"Background initialization complete: {state}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Background init failed: {ex.Message}");
        }
    });
}
```

This way, by the time user clicks "Generate AI text", initialization is already complete.

---

### Verification

```csharp
// Measure first vs subsequent calls
var sw = Stopwatch.StartNew();
var state1 = LanguageModel.GetReadyState();
sw.Stop();
Console.WriteLine($"First call: {sw.Elapsed.TotalSeconds:F1}s");

sw.Restart();
var state2 = LanguageModel.GetReadyState();
sw.Stop();
Console.WriteLine($"Second call: {sw.Elapsed.TotalSeconds:F1}s");
```

Expected output on Intel Core Ultra:
```
First call: 47.3s
Second call: 0.2s
```

---

## Scenario 2: MakeAvailableAsync Hanging

### Root Cause

`MakeAvailableAsync()` triggers Feature on Demand (FOD) installation through Windows Update. On Intel platforms, this can hang indefinitely due to:
- Windows Update service stuck
- Package Deployment service not responding
- Network/proxy issues blocking FOD download
- Corrupted Windows Update cache

---

### Solution: Skip MakeAvailable Entirely

**Recommended Approach - Don't Call MakeAvailableAsync:**

```csharp
public async Task<LanguageModel?> GetLanguageModelAsync()
{
    // ❌ DON'T DO THIS (hangs on Intel):
    // if (!LanguageModel.IsAvailable())
    // {
    //     await LanguageModel.MakeAvailableAsync();  // Hangs forever!
    // }
    
    // ✅ DO THIS instead:
    try
    {
        // Just try to create - if it fails, model not available
        var model = await LanguageModel.CreateAsync();
        return model;
    }
    catch (COMException ex) when (ex.HResult == unchecked((int)0x80040154))
    {
        // Model not available - show user instructions
        ShowModelInstallationInstructions();
        return null;
    }
}

private void ShowModelInstallationInstructions()
{
    MessageBox.Show(
        "AI model not installed.\n\n" +
        "Please install from:\n" +
        "Settings → Windows Update → Advanced options → Optional features\n\n" +
        "Search for 'AI' or 'Copilot Runtime'",
        "AI Model Required"
    );
}
```

---

### Alternative: MakeAvailable with Timeout

If you must use `MakeAvailableAsync`, add aggressive timeout:

```csharp
public async Task<bool> TryInstallModelAsync()
{
    var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
    
    try
    {
        ShowLoadingMessage("Downloading AI model (may take several minutes)...");
        
        var operation = LanguageModel.MakeAvailableAsync();
        var result = await operation.AsTask(cts.Token);
        
        HideLoadingMessage();
        
        if (result.Status == AIFeatureInstallationStatus.Installed)
        {
            ShowSuccess("AI model installed successfully!");
            return true;
        }
        else
        {
            ShowError($"Installation failed: {result.Status}");
            return false;
        }
    }
    catch (TaskCanceledException)
    {
        HideLoadingMessage();
        ShowError("Installation timed out - please install manually through Windows Update");
        return false;
    }
}
```

---

### System-Level Fixes

If MakeAvailableAsync is hanging, try these Windows-level fixes:

**1. Restart Windows Update Services:**

```powershell
# Run as Administrator
Restart-Service wuauserv, bits, dosvc -Force

# Verify they're running
Get-Service wuauserv, bits, dosvc | Format-Table Name, Status, StartType
```

Expected output:
```
Name      Status  StartType
----      ------  ---------
wuauserv  Running Automatic
bits      Running Automatic
dosvc     Running Automatic
```

**2. Clear Windows Update Cache:**

```powershell
# Run as Administrator
Stop-Service wuauserv, bits
Remove-Item C:\Windows\SoftwareDistribution\Download\* -Recurse -Force
Start-Service wuauserv, bits
```

**3. Manual Windows Update:**

```powershell
# Open Windows Update settings
Start-Process "ms-settings:windowsupdate"

# Click "Check for updates"
# Let all updates install
# Reboot
```

**4. Check for AI Packages:**

```powershell
Get-WindowsPackage -Online | Where-Object {$_.PackageName -like "*AI*" -or $_.PackageName -like "*Copilot*"}
```

**5. Nuclear Option - Complete Reboot:**

Sometimes Windows Update just needs a fresh start:
```powershell
shutdown /r /t 0
```

---

## Platform Comparison

| Platform | First GetReadyState | MakeAvailableAsync |
|----------|---------------------|-------------------|
| **Intel Core Ultra** | ⚠️ 20-100+ seconds | ⚠️ May hang forever |
| Snapdragon X Elite/Plus | ✅ 1-5 seconds | ✅ Usually works |
| AMD Ryzen AI 300 | ⚠️ 5-20 seconds | ✅ Usually works |

---

## Best Practices

### ✅ DO:

1. **Show loading UI for first WCR call**
   ```csharp
   StatusText.Text = "Initializing AI (first use may take up to 2 minutes)...";
   ```

2. **Cache state after first call**
   ```csharp
   private static bool _initialized = false;
   ```

3. **Skip MakeAvailableAsync on Intel**
   ```csharp
   // Just use CreateAsync and handle exceptions
   ```

4. **Use timeout for any long-running state checks**
   ```csharp
   await Task.WhenAny(checkTask, Task.Delay(10000));
   ```

---

### ❌ DON'T:

1. **Call GetReadyState in tight loop**
   ```csharp
   while (GetReadyState() != Ready) { }  // May hang UI for 100s
   ```

2. **Call MakeAvailableAsync without timeout**
   ```csharp
   await MakeAvailableAsync();  // May never return on Intel
   ```

3. **Block UI thread**
   ```csharp
   var state = GetReadyState();  // Freezes UI for 100s on Intel
   ```

---

## Verification Checklist

After implementing workarounds:

- [ ] First WCR use shows loading message to user
- [ ] App doesn't freeze/hang during initialization
- [ ] Timeout prevents indefinite wait
- [ ] Subsequent calls are fast
- [ ] Error messages guide user to manual installation if needed

---

## References

- [Issue #5487: GetReadyState slow on Intel](https://github.com/microsoft/WindowsAppSDK/issues/5487)
- [Issue #5200: MakeAvailableAsync hanging on Intel Core Ultra](https://github.com/microsoft/WindowsAppSDK/issues/5200)
- [Windows AI APIs Documentation](https://learn.microsoft.com/windows/ai/apis/)

---

**Last Updated:** 2026-01-04  
**Confidence:** 0.85

## Changelog

**2026-01-04:**
- Split from state_management_issues.md
- Added Intel platform specific details
- Enhanced system-level fix procedures
