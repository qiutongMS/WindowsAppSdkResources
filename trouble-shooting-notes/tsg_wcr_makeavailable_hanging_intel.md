---
id: wcr_makeavailable_hanging_intel
title: "TextRecognizer.MakeAvailableAsync() hangs on Intel Core Ultra processors"
area: Runtime
symptoms:
  - "MakeAvailableAsync() hangs without returning"
  - "No timeout or error message when calling MakeAvailableAsync"
  - "Application freezes on Intel Core Ultra during model deployment"
errorCodes: []
keywords:
  - "MakeAvailableAsync"
  - "hanging"
  - "Intel"
  - "Core Ultra"
  - "TextRecognizer"
  - "timeout"
  - "freeze"
appliesTo:
  windows: ">=10.0.26120"
  winappsdk: "1.7.250127003-experimental3"
projectType: "packaged"
severity: "common"
lastVerified: "2025-12-23"
references:
  - "https://github.com/microsoft/WindowsAppSDK/issues/5200"
---

# Problem

When calling `TextRecognizer.MakeAvailableAsync()` or similar WCR model deployment APIs on Intel Core Ultra 5 238V and potentially other Intel Core Ultra processors, the async method hangs indefinitely without returning, throwing an exception, or providing any error feedback. The application appears frozen at the MakeAvailableAsync call.

# Quick Diagnosis

1. Verify you're running on Intel Core Ultra processor (check Task Manager > Performance > CPU)
2. Identify if the application hangs specifically at `MakeAvailableAsync()` or `EnsureReadyAsync()` calls
3. Check if the same code works on AMD or Qualcomm Snapdragon devices
4. Verify Windows Insider build version (issue reported on 26120.3585)
5. Look for no error, no exception, just indefinite waiting

# Root Cause

The root cause appears to be related to Windows Update / Feature on Demand (FOD) infrastructure on Intel platforms when downloading WCR AI models. The MakeAvailableAsync() API internally triggers Windows Update to download and deploy the AI model packages, and this process can hang on some Intel systems due to:

1. **Windows Update service issues** on specific Intel platforms
2. **DirectML/NPU driver compatibility** with Intel AI Boost
3. **Model download/validation timeout** issues specific to Intel infrastructure
4. **Race condition** in WCR model deployment code on Intel

The issue is inconsistent - sometimes it works after a system restart or Windows Update cycle.

# Fix / Workaround

**Workaround 1: Use GetReadyState() and IsAvailable() instead**

Avoid MakeAvailableAsync entirely and use the newer APIs:

```csharp
// Instead of MakeAvailableAsync():
// var loadResult = await TextRecognizer.MakeAvailableAsync();

// Use this pattern:
if (!TextRecognizer.IsAvailable())
{
    // Show user message: "Text Recognition model not installed"
    // Direct them to install via Settings or Windows Update
    throw new Exception("OCR model not available. Please install via Windows Settings.");
}

// Or try EnsureReadyAsync with timeout:
var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
try 
{
    var ensureTask = TextRecognizer.EnsureReadyAsync();
    // Note: EnsureReadyAsync doesn't support CancellationToken in current SDK
    // This is just demonstration - you may need to use Task.WhenAny for timeout
    
    var completedTask = await Task.WhenAny(
        ensureTask.AsTask(),
        Task.Delay(TimeSpan.FromMinutes(5), cts.Token));
    
    if (completedTask == ensureTask.AsTask())
    {
        var result = await ensureTask;
        if (result.Status != AIFeatureReadyResultState.Success)
        {
            throw new Exception($"Deployment failed: {result.ExtendedError?.Message}");
        }
    }
    else
    {
        throw new TimeoutException("Model deployment timed out after 5 minutes");
    }
}
catch (TimeoutException)
{
    // Handle timeout - may need manual Windows Update intervention
    throw;
}
```

**Workaround 2: Manually trigger Windows Update**

1. Open Settings > Windows Update
2. Click "Check for updates"
3. Look for "Windows AI Model" or similar optional features
4. Install all available updates
5. Restart system
6. Try application again - model may now be pre-installed

**Workaround 3: Use winget to pre-install models**

```powershell
# Check for Windows AI packages
winget search "AI" | Select-String "Windows"

# If available, install manually
# (Package name varies by Windows version)
```

**Workaround 4: Restart system and Windows Update service**

```powershell
# Restart Windows Update service
net stop wuauserv
net start wuauserv

# Clear Windows Update cache
net stop wuauserv
Remove-Item C:\Windows\SoftwareDistribution\* -Recurse -Force
net start wuauserv

# Check for updates
```

**Workaround 5: Try on AMD/Qualcomm hardware**

The issue appears specific to Intel Core Ultra platforms. Testing indicates the same code works reliably on AMD Ryzen AI 300 series processors.

# Verification

1. Apply one of the workarounds above
2. If using IsAvailable() pattern, verify it returns `true`
3. If using EnsureReadyAsync with timeout, verify it completes within timeout period
4. Create TextRecognizer instance: `var recognizer = await TextRecognizer.CreateAsync()`
5. Test OCR functionality on sample image
6. Verify no hanging behavior

# Deep Dive

## Affected Intel Processors

Reported on:
- Intel Core Ultra 5 238V
- Potentially other Intel Core Ultra Series 2 processors with AI Boost

NOT reported on:
- AMD Ryzen AI 9 HX 370
- AMD Ryzen AI 300 series (works normally)
- Qualcomm Snapdragon X Elite/Plus (works normally)

## Comparison: AMD vs Intel

**AMD Ryzen AI (WORKS):**
```
OS: Windows 11 24H2 (26120.3576)
CPU: AMD Ryzen AI 9 HX 370 w/ Radeon 890M
Result: MakeAvailableAsync() completes successfully
```

**Intel Core Ultra (HANGS):**
```
OS: Windows 11 24H2 (26120.3585)  
CPU: Intel Core Ultra 5 238V
Result: MakeAvailableAsync() hangs indefinitely
```

## Technical Background

`MakeAvailableAsync()` internally:
1. Checks if model is already installed locally
2. If not, triggers Windows Update FOD (Features on Demand) to download
3. Waits for download and extraction
4. Validates model integrity
5. Returns deployment status

The hang occurs during step 2 or 3 - the Windows Update download doesn't complete or times out without proper error propagation.

## API Evolution

Note: MakeAvailableAsync() is an older API pattern:
- **Older SDK versions:** `MakeAvailableAsync()`
- **Newer SDK versions:** `IsAvailable()`, `EnsureReadyAsync()`, `GetReadyState()`

Prefer the newer APIs as they may have better error handling.

## Related Issues

- Issue #5487: GetReadyState() takes 20-100+ seconds on Intel (performance, not hanging)
- Various Windows Update reliability issues on Intel platforms

## Microsoft Response

This issue is under investigation. Status: investigating label applied.

# References

- [GitHub Issue #5200](https://github.com/microsoft/WindowsAppSDK/issues/5200)
- [Text Recognition API Documentation](https://learn.microsoft.com/windows/ai/apis/text-recognition)
- [Windows Update Troubleshooting](https://support.microsoft.com/windows/windows-update-troubleshooting)
