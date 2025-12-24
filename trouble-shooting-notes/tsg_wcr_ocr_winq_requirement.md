---
id: wcr_ocr_winq_requirement
title: "TextRecognizer requires Win+Q initialization before use"
area: Runtime
symptoms:
  - "TextRecognizer.IsAvailable() throws error if Win+Q not initialized"
  - "OCR fails until Windows Search is opened with Win+Q"
  - "Character recognition errors after fresh system install or restart"
errorCodes: []
keywords:
  - "TextRecognizer"
  - "Win+Q"
  - "Windows Search"
  - "initialization"
  - "OCR"
  - "prerequisite"
  - "AMD"
  - "Intel"
appliesTo:
  windows: ">=10.0.26120"
  winappsdk: ">=1.7.0-experimental"
projectType: "packaged"
severity: "edge"
lastVerified: "2025-12-23"
references:
  - "https://github.com/microsoft/WindowsAppSDK/issues/5276"
---

# Problem

After a fresh Windows installation or system restart, calling `TextRecognizer.IsAvailable()` or other TextRecognizer APIs throws an exception. However, once the Windows Search interface (Win+Q) has been opened and successfully initialized, the TextRecognizer APIs begin working normally. This unusual prerequisite affects AMD and Intel platforms.

# Quick Diagnosis

1. Confirm you're on a fresh Windows install or recent restart
2. Verify TextRecognizer APIs are failing with exceptions
3. Press **Win+Q** to open Windows Search
4. Wait for search interface to fully load (may show "initializing" or loading spinner)
5. Close search and retry TextRecognizer APIs
6. Check if APIs now work correctly

# Root Cause

The TextRecognizer API appears to have an undocumented dependency on Windows Search infrastructure or related Windows AI components that are lazily initialized when Windows Search is first activated. The Win+Q keyboard shortcut triggers Windows Search, which in turn initializes background AI/OCR services that TextRecognizer depends on.

This dependency might be related to:
- Shared Windows AI runtime components initialized by Windows Search
- DirectML or NPU runtime that Search triggers
- Windows.AI.MachineLearning infrastructure lazy initialization
- Shared model caching or indexing systems

# Fix / Workaround

**Workaround: Programmatically trigger Windows Search initialization**

While you cannot directly call Win+Q from code, you can attempt to trigger similar initialization:

```csharp
// Option 1: Try Windows.ApplicationModel.Search (may not work in all scenarios)
using Windows.ApplicationModel.Search;

public async Task InitializeSearchInfrastructure()
{
    try 
    {
        var searchPane = SearchPane.GetForCurrentView();
        searchPane.Show();
        await Task.Delay(2000); // Give it time to initialize
        searchPane.Hide();
    }
    catch 
    {
        // May not work in all app types
    }
}

// Option 2: Start Windows Search process (may require elevation)
using System.Diagnostics;

public void TriggerWindowsSearch()
{
    try
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "search:",
            UseShellExecute = true
        });
        Thread.Sleep(3000); // Wait for initialization
        // Close search window if needed
    }
    catch { }
}

// Option 3: Show user message to manually trigger
public async Task<bool> EnsureTextRecognizerAvailable()
{
    int retries = 3;
    for (int i = 0; i < retries; i++)
    {
        try
        {
            if (TextRecognizer.IsAvailable())
            {
                return true;
            }
            
            await Task.Delay(2000);
        }
        catch when (i == retries - 1)
        {
            // Show message to user
            var dialog = new ContentDialog
            {
                Title = "OCR Initialization Required",
                Content = "Please press Win+Q to open Windows Search, wait for it to load, then close it and try again.",
                CloseButtonText = "OK"
            };
            await dialog.ShowAsync();
            throw;
        }
        catch
        {
            await Task.Delay(2000);
        }
    }
    return false;
}
```

**Manual Workaround for End Users:**

1. After booting Windows or starting application
2. Press **Win+Q** (Windows Search keyboard shortcut)
3. Wait for search interface to fully initialize (1-3 seconds)
4. Close the search window
5. Run your application using TextRecognizer
6. APIs should now work normally

**System-Level Workaround:**

Configure Windows Search to start automatically:

1. Open Services (services.msc)
2. Find "Windows Search" service
3. Set Startup type to **Automatic**
4. Start the service
5. Restart system
6. This may pre-initialize the required components

# Verification

1. Restart your computer (fresh boot)
2. **Do NOT** open Windows Search yet
3. Run your application and call `TextRecognizer.IsAvailable()`
4. If it fails, press **Win+Q**, wait for search to initialize, close it
5. Run application again
6. `TextRecognizer.IsAvailable()` should now return `true`
7. Create TextRecognizer and test OCR functionality

# Deep Dive

## Affected Platforms

Reported on:
- **AMD Ryzen AI 9 HX 370** (OS build 26120.3360)
- **Intel Core Ultra 5 238V** (OS build 26120.3291)
- NOT reported on Qualcomm Snapdragon X Elite/Plus (may not affect ARM64)

## System Reinstall Observation

The reporter noted:
> "I have reinstalled the system several times. After each reinstall, no matter whether the system is restarted or not, as long as the system win+Q screen has not been initialized successfully, the following error will appear."

This indicates the issue is **reproducible and consistent** on fresh Windows installations on AMD/Intel platforms.

## Technical Hypothesis

Possible explanations for this dependency:

1. **Shared ML Runtime:** Windows Search and WCR may share Windows.AI.MachineLearning.dll or similar runtime that's lazily loaded
2. **DirectML Initialization:** Opening Search may trigger GPU/NPU runtime initialization that TextRecognizer requires
3. **Model Registry:** Search may initialize a model registry or cache that TextRecognizer queries
4. **Windows AI Service:** Both may depend on a background service (e.g., "Windows AI Service") that Search starts

## Architecture Impact

This suggests an architectural coupling between:
- Windows Search infrastructure
- Windows Copilot Runtime APIs
- Potentially shared DirectML/NPU initialization

This is likely unintentional and may be considered a bug.

## Workaround Effectiveness

Success rates of workarounds:
- ✅ **Manual Win+Q:** 100% effective but requires user action
- ⚠️ **Programmatic Search trigger:** Variable - depends on app permissions and Windows version
- ✅ **Windows Search service auto-start:** May pre-initialize components
- ❓ **Unknown:** Microsoft has not confirmed or provided official workaround

## Comparison with Other WCR APIs

Does NOT affect:
- ✓ ImageDescriptionGenerator
- ✓ Phi Silica LanguageModel
- ✓ ImageScaler

Appears **specific to TextRecognizer** OCR API.

## Potential Future Fix

Microsoft should:
1. Remove dependency on Windows Search initialization
2. Explicitly initialize required components when TextRecognizer is first accessed
3. Document any true prerequisites clearly
4. Provide better error messages if prerequisites not met

# References

- [GitHub Issue #5276](https://github.com/microsoft/WindowsAppSDK/issues/5276)
- [Text Recognition API Documentation](https://learn.microsoft.com/windows/ai/apis/text-recognition)
- [Windows Search Overview](https://support.microsoft.com/windows/search-windows)
