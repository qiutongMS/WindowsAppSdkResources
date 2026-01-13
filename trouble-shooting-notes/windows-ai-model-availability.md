# Troubleshooting Guide: Model Availability and Loading Issues

## Symptom
AI model availability checks fail or models cannot be loaded:
- `GetReadyState()` returns `NotReady` even when model is available
- `MakeAvailableAsync()` hangs indefinitely
- `EnsureReadyAsync()` fails with error codes
- Model works despite availability checks failing

## Affected Components
- **LanguageModel** (Phi Silica)
- **ImageDescriptionGenerator**
- **TextRecognizer** (OCR)
- **Windows App SDK**: 1.7-experimental2, 1.7-experimental3, 1.8+

## Related Issues
- #5982 - GetReadyState and EnsureReadyAsync return NotReady when model is available
- #6019 - GetReadyState reports "Not declared by app" error
- #5200 - MakeAvailableAsync() hangs without error
- #5171 - MakeAvailableAsync returns "Element not found" COMException
- #5201 - IsAvailable/MakeAvailable creates friction with Windows Update errors

## Root Causes

### 1. Bug in Availability Check APIs (SDK 2.0 Experimental 1)
`GetReadyState()` and `EnsureReadyAsync()` incorrectly report models as unavailable even when they can be used.

### 2. Windows Update / Feature on Demand Issues
Model downloads depend on Windows Update infrastructure which can fail with various errors:
- `0x800700AA (ERROR_BUSY)` - Resource in use
- `0x80246019 (WU_E_DM_UNAUTHORIZED_MSA_USER)` - Update service issue
- `0x80073D3B` - Product not applicable or cannot be found

### 3. Hardware/Platform Specific Issues
- Intel Core Ultra platforms may experience very slow `GetReadyState()` calls (20-100+ seconds)
- Some platforms may not have models available at all

### 4. Missing LAF Declaration
The Limited Access Feature for language models may not be properly declared in the manifest.

## Solutions

### Solution 1: Skip Availability Checks (Temporary Workaround for SDK 2.0-exp1)

If you're on Windows App SDK 2.0.250930001-experimental1, the availability check APIs have a known bug:

```csharp
// DON'T DO THIS - it will incorrectly report NotReady:
// if (!LanguageModel.GetReadyState() == AIFeatureReadyState.Ready) { ... }

// WORKAROUND - Skip the check and try to create the model directly:
try 
{
    var languageModel = LanguageModel.CreateAsync().get();
    // Model works fine even though GetReadyState() said NotReady
}
catch (Exception ex)
{
    // Handle actual unavailability here
}
```

**Note**: This bug should be fixed in later SDK versions.

### Solution 2: Fix LAF Declaration

Add proper LAF unlock before checking availability:

```csharp
var featureId = "com.microsoft.windows.ai.languagemodel";
var limitedAccessFeatureResult = LimitedAccessFeatures.TryUnlockFeature(
    featureId,
    demoToken,
    $"{demoPublisherId} has registered their use of {featureId} with Microsoft and agrees to the terms of use.");

if (limitedAccessFeatureResult.Status != LimitedAccessFeatureStatus.Available && 
    limitedAccessFeatureResult.Status != LimitedAccessFeatureStatus.AvailableWithoutToken)
{
    throw new InvalidOperationException($"LAF not available: {limitedAccessFeatureResult.Status}");
}

// NOW check availability
var readyState = LanguageModel.GetReadyState();
```

### Solution 3: Handle MakeAvailableAsync Hangs

If `MakeAvailableAsync()` hangs indefinitely:

```csharp
// Add timeout handling
var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

try
{
    var loadTask = TextRecognizer.MakeAvailableAsync().AsTask(cts.Token);
    var result = await loadTask;
    
    if (result.Status != PackageDeploymentStatus.CompletedSuccess)
    {
        throw new Exception(result.ExtendedError?.Message ?? "Model loading failed");
    }
}
catch (TaskCanceledException)
{
    // Timeout occurred - may need system reboot or Windows Update troubleshooting
    throw new Exception("Model download timed out. Try rebooting and checking Windows Update.");
}
```

### Solution 4: Windows Update Troubleshooting

For Windows Update related errors:

1. **Check Windows Update status**:
   ```powershell
   Get-WindowsUpdateLog
   ```

2. **Reset Windows Update components**:
   ```powershell
   net stop wuauserv
   net stop bits
   net stop cryptsvc
   rd /s /q %systemroot%\SoftwareDistribution
   rd /s /q %systemroot%\system32\catroot2
   net start wuauserv
   net start bits
   net start cryptsvc
   ```

3. **Reboot the system** - Often resolves `ERROR_BUSY` issues

### Solution 5: Intel Platform Performance Issues

If `GetReadyState()` takes 20-100+ seconds on Intel Core Ultra platforms:

1. **Ensure latest Intel drivers** are installed
2. **Check for Windows Insider updates** (Build 26120.4161+)
3. **Add async/await with progress indication**:

```csharp
var progressIndicator = ShowProgressDialog();
await Task.Run(() => {
    var state = LanguageModel.GetReadyState();
    progressIndicator.Close();
    return state;
});
```

## Verification Steps

1. **Test without availability check** to confirm model actually works
2. **Check Windows Update status** and logs
3. **Verify LAF unlock** returns Available status
4. **Test on different hardware** if available
5. **Check Event Viewer** for deployment errors

## Expected Outcome
- `GetReadyState()` returns `Ready` when model is available
- `MakeAvailableAsync()` completes within reasonable time (< 5 minutes)
- Model creation succeeds after availability check passes

## Known Issues

### Windows App SDK 2.0 Experimental 1
- **GetReadyState() bug**: Returns `NotReady` even when model works fine
- **Recommended**: Upgrade to later experimental version or skip availability checks

### Intel Core Ultra Platforms  
- **Very slow GetReadyState()**: Can take 20-100+ seconds
- **Workaround**: Use async with timeout and progress indication

### Windows Update Dependencies
- Model distribution via Windows Update/Feature on Demand is unreliable
- Various error codes (0x800700AA, 0x80246019, 0x80073D3B) reported
- Reboot often required to clear stuck state

## Additional Resources
- [Windows AI APIs - Get Started](https://learn.microsoft.com/windows/ai/apis/)
- [Troubleshoot Windows Update](https://support.microsoft.com/windows/troubleshoot-problems-updating-windows-188c2b0f-10a7-d72f-65b8-32d177eb136c)
- Issue #5982 for availability check bug details
- Issue #5201 for Windows Update friction discussion

## Related TSGs
- [Authentication and Access Denied Errors](windows-ai-authentication-access-denied.md)
- [Phi Silica Initialization Failures](windows-ai-phi-silica-initialization.md)
