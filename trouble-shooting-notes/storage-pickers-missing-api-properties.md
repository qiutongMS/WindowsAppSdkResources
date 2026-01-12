# Troubleshooting Guide: Storage Pickers Missing API Properties

## Symptom
Useful properties missing in `Microsoft.Windows.Storage.Pickers`:
- Cannot set custom dialog title
- Cannot use `SettingsIdentifier` to remember locations
- Cannot pick multiple folders (`PickMultipleFoldersAsync` missing)

## Affected Components
- Microsoft.Windows.Storage.Pickers (all picker types)
- Windows App SDK: 1.8.0+
- Issues: [#5879](https://github.com/microsoft/WindowsAppSDK/issues/5879), [#5847](https://github.com/microsoft/WindowsAppSDK/issues/5847), [#5848](https://github.com/microsoft/WindowsAppSDK/issues/5848)

## Root Cause

When porting from `Windows.Storage.Pickers` to Windows App SDK 1.8, several properties/methods were not included:
- **Title**: Never existed in UWP either, commonly requested
- **SettingsIdentifier**: Existed in UWP, not ported to SDK
- **PickMultipleFoldersAsync**: Win32 supports it, not exposed in SDK

## Workarounds

### Workaround 1: Custom Title (No Solution)

**No way** to set custom title currently:

```csharp
var picker = new FolderPicker(AppWindow.Id)
{
    CommitButtonText = "Select Project Folder", // ✅ This works
    // Title = "Pick folder" // ❌ Property doesn't exist
};
```

**Status:** Feature request - upvote [#5879](https://github.com/microsoft/WindowsAppSDK/issues/5879)

### Workaround 2: Implement SettingsIdentifier Manually

Save/restore locations using ApplicationData:

```csharp
public class PickerLocationManager
{
    public static async Task<StorageFolder> GetLastLocationAsync(string scenarioId)
    {
        var settings = ApplicationData.Current.LocalSettings;
        if (settings.Containers.TryGetValue("PickerLocations", out var container))
        {
            if (container.Values.TryGetValue(scenarioId, out object pathObj))
            {
                return await StorageFolder.GetFolderFromPathAsync(pathObj.ToString());
            }
        }
        return null;
    }
    
    public static void SaveLastLocation(string scenarioId, StorageFolder folder)
    {
        var settings = ApplicationData.Current.LocalSettings;
        var container = settings.CreateContainer("PickerLocations", 
            ApplicationDataCreateDisposition.Always);
        container.Values[scenarioId] = folder.Path;
    }
}

// Usage
var lastFolder = await PickerLocationManager.GetLastLocationAsync("project-picker");
// NOTE: Cannot actually SET initial folder in picker (limitation)

var picker = new FolderPicker(AppWindow.Id);
picker.FileTypeFilter.Add("*");
var folder = await picker.PickSingleFolderAsync();

if (folder != null)
{
    PickerLocationManager.SaveLastLocation("project-picker", folder);
}
```

**Limitations:** Cannot force picker to open at saved location, only tracks user preference.

### Workaround 3: Multiple Folder Selection (No Direct Solution)

**Cannot** pick multiple folders currently:

```csharp
// ❌ This method doesn't exist
// var folders = await picker.PickMultipleFoldersAsync();

// ✅ Only single folder works
var folder = await picker.PickSingleFolderAsync();
```

**Alternative:** Show picker in loop:

```csharp
private async Task<List<StorageFolder>> PickMultipleFoldersLoopAsync()
{
    var folders = new List<StorageFolder>();
    
    while (true)
    {
        var picker = new FolderPicker(AppWindow.Id);
        picker.FileTypeFilter.Add("*");
        
        var folder = await picker.PickSingleFolderAsync();
        if (folder == null) break;
        
        folders.Add(folder);
        
        var dialog = new ContentDialog
        {
            Content = "Add another folder?",
            PrimaryButtonText = "Yes",
            CloseButtonText = "Done"
        };
        
        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
            break;
    }
    
    return folders;
}
```

**Status:** Feature request - upvote [#5848](https://github.com/microsoft/WindowsAppSDK/issues/5848)

## Feature Status

| Feature | Windows.Storage.Pickers | Microsoft.Windows.Storage.Pickers | Status |
|---------|------------------------|-----------------------------------|---------|
| Title | ❌ Never | ❌ No | Requested [#5879](https://github.com/microsoft/WindowsAppSDK/issues/5879) |
| SettingsIdentifier | ✅ Yes | ❌ No | Requested [#5847](https://github.com/microsoft/WindowsAppSDK/issues/5847) |
| PickMultipleFoldersAsync | ❌ Never | ❌ No | Requested [#5848](https://github.com/microsoft/WindowsAppSDK/issues/5848) |

## Verification

Test API limitations:

```csharp
var picker = new FolderPicker(AppWindow.Id);

var hasTitle = typeof(FolderPicker).GetProperty("Title") != null; // false
var hasSettingsId = typeof(FolderPicker).GetProperty("SettingsIdentifier") != null; // false
var hasMultiplePick = typeof(FolderPicker).GetMethod("PickMultipleFoldersAsync") != null; // false
```

## How to Help

If you need these features:
1. 👍 Upvote the GitHub issues
2. Add comments describing your use case
3. Share how missing features impact your app

## Related Resources
- [FolderPicker API](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.windows.storage.pickers.folderpicker)
- [Legacy Windows.Storage.Pickers](https://learn.microsoft.com/uwp/api/windows.storage.pickers)
