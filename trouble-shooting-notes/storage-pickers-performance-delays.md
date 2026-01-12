# Troubleshooting Guide: FileSavePicker Performance Delays

## Symptom
- `FileSavePicker.PickSaveFileAsync()` hangs 5-10 seconds on first call
- Application appears frozen during delay
- Second and subsequent calls are fast (<1 second)

## Affected Components
- Microsoft.Windows.Storage.Pickers (all picker types)
- Windows App SDK: 1.5.x - 1.6.0
- Issue [#4538](https://github.com/microsoft/WindowsAppSDK/issues/4538)

## Root Cause

First call initializes Windows shell infrastructure:
- Shell namespace handlers
- File type associations
- Recent files indexing
- Cloud storage providers (OneDrive)
- Network location enumeration

This is a Windows shell limitation, not specific to Windows App SDK.

## Workarounds

### Workaround 1: Show Loading UI

Display feedback during delay:

```csharp
private async Task<StorageFile> SaveFileWithLoadingAsync()
{
    var loadingDialog = new ContentDialog
    {
        Title = "Opening file picker...",
        Content = new ProgressRing { IsActive = true },
        XamlRoot = this.Content.XamlRoot
    };
    
    _ = loadingDialog.ShowAsync();
    
    try
    {
        var picker = new FileSavePicker(AppWindow.Id)
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = "MyFile"
        };
        picker.FileTypeChoices.Add("Text", new[] { ".txt" });
        
        return await picker.PickSaveFileAsync();
    }
    finally
    {
        loadingDialog.Hide();
    }
}
```

### Workaround 2: Use Faster Start Locations

Some locations initialize faster:

```csharp
// ❌ SLOW - Enumerates network
SuggestedStartLocation = PickerLocationId.ComputerFolder

// ✅ FASTER - Local folders
SuggestedStartLocation = PickerLocationId.DocumentsLibrary

// ✅ FASTEST - Cached recent files
SuggestedStartLocation = PickerLocationId.Downloads
```

### Workaround 3: Pre-warm on Startup

Create picker object during app startup (partial help):

```csharp
public partial class App : Application
{
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        m_window = new MainWindow();
        m_window.Activate();
        
        // Pre-warm after window loads
        _ = Task.Run(async () =>
        {
            await Task.Delay(2000);
            m_window.DispatcherQueue.TryEnqueue(async () =>
            {
                var picker = new FileSavePicker(AppWindow.Id);
                picker.FileTypeChoices.Add("Text", new[] { ".txt" });
                // Creating object does partial initialization
            });
        });
    }
}
```

**Note:** Only partially effective - actual delay occurs when showing dialog.

## Performance Comparison

| Scenario | First Call | Second Call |
|----------|-----------|-------------|
| No optimization | 5-10 sec | <1 sec |
| Pre-warming | 3-5 sec | <1 sec |
| Loading UI | 5-10 sec* | <1 sec |
| Simple location | 3-5 sec | <1 sec |

*Same delay, better UX

## Verification

Measure performance:

```csharp
var stopwatch = System.Diagnostics.Stopwatch.StartNew();

var picker = new FileSavePicker(AppWindow.Id);
picker.FileTypeChoices.Add("Text", new[] { ".txt" });
var file = await picker.PickSaveFileAsync();

stopwatch.Stop();
Debug.WriteLine($"Picker took: {stopwatch.ElapsedMilliseconds}ms");
// First call: 5000-10000ms
// Second call: 100-500ms
```

## Known Limitations
- Cannot completely eliminate first-call delay (Windows shell limitation)
- Pre-warming doesn't fully initialize all components
- Delay varies by system (faster on SSD, slower with network drives)

## Related Resources
- [FileSavePicker API](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.windows.storage.pickers.filesavepicker)
- [Storage picker best practices](https://learn.microsoft.com/windows/apps/design/controls/file-picker)
