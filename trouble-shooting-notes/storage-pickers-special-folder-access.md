# Troubleshooting Guide: FolderPicker Special Folder Access Security Issue

## Symptom
- `FolderPicker` allows selecting system folders (`C:\Windows`, `C:\Program Files`)
- No warning or restriction on protected directories
- Apps can write to sensitive locations if elevated

## Affected Components
- Microsoft.Windows.Storage.Pickers.FolderPicker
- Windows App SDK: 1.8.0+
- Issue [#5951](https://github.com/microsoft/WindowsAppSDK/issues/5951)

## Root Cause

Windows App SDK `FolderPicker` uses Win32 `IFileOpenDialog` which allows selecting any folder the user has access to. Unlike UWP sandboxed pickers, there's no built-in system folder restriction.

## Security Implications

Apps could write to system folders if elevated:

```csharp
var picker = new FolderPicker(AppWindow.Id);
var folder = await picker.PickSingleFolderAsync();
// ⚠️ User selects C:\Windows\System32

// ❌ App can now write to System32 if elevated!
var file = await folder.CreateFileAsync("malicious.dll", 
    CreationCollisionOption.ReplaceExisting);
```

## Solutions

### Solution 1: Validate Selected Folder (Recommended)

Check and reject system folders:

```csharp
using System.IO;

public class SafeFolderPicker
{
    private static readonly string[] BlockedPaths = new[]
    {
        Environment.GetFolderPath(Environment.SpecialFolder.System),
        Environment.GetFolderPath(Environment.SpecialFolder.Windows),
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SysWOW64"),
    };
    
    public static bool IsSystemFolder(string folderPath)
    {
        var normalizedPath = Path.GetFullPath(folderPath).TrimEnd('\\').ToLowerInvariant();
        
        foreach (var blockedPath in BlockedPaths)
        {
            if (string.IsNullOrEmpty(blockedPath)) continue;
            
            var normalizedBlocked = Path.GetFullPath(blockedPath).TrimEnd('\\').ToLowerInvariant();
            
            if (normalizedPath == normalizedBlocked || 
                normalizedPath.StartsWith(normalizedBlocked + "\\"))
            {
                return true;
            }
        }
        return false;
    }
    
    public static async Task<StorageFolder> PickSafeFolderAsync(WindowId windowId)
    {
        var picker = new FolderPicker(windowId)
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };
        picker.FileTypeFilter.Add("*");
        
        while (true)
        {
            var folder = await picker.PickSingleFolderAsync();
            if (folder == null) return null;
            
            if (IsSystemFolder(folder.Path))
            {
                var dialog = new ContentDialog
                {
                    Title = "Invalid Folder",
                    Content = $"Cannot select system folder:\n{folder.Path}\n\nChoose a different location.",
                    CloseButtonText = "OK"
                };
                await dialog.ShowAsync();
                continue;
            }
            
            return folder;
        }
    }
}

// Usage
var folder = await SafeFolderPicker.PickSafeFolderAsync(AppWindow.Id);
```

### Solution 2: Use Recommended Locations

Suggest safe default locations:

```csharp
private readonly Dictionary<string, PickerLocationId> SafeLocations = new()
{
    ["Documents"] = PickerLocationId.DocumentsLibrary,
    ["Desktop"] = PickerLocationId.Desktop,
    ["Downloads"] = PickerLocationId.Downloads,
    ["Pictures"] = PickerLocationId.PicturesLibrary,
};

var picker = new FolderPicker(AppWindow.Id)
{
    SuggestedStartLocation = PickerLocationId.DocumentsLibrary // Safe default
};
```

### Solution 3: Educational UI

Warn users before showing picker:

```csharp
private async Task<StorageFolder> PickFolderWithWarningAsync()
{
    var warningDialog = new ContentDialog
    {
        Title = "Select Folder",
        Content = "⚠️ Do not select system folders:\n" +
                 "• C:\\Windows\n• C:\\Program Files\n\n" +
                 "✅ Recommended: Documents, Desktop, Downloads",
        PrimaryButtonText = "Choose Folder",
        CloseButtonText = "Cancel",
        XamlRoot = this.Content.XamlRoot
    };
    
    if (await warningDialog.ShowAsync() != ContentDialogResult.Primary)
        return null;
    
    return await SafeFolderPicker.PickSafeFolderAsync(AppWindow.Id);
}
```

## Verification Tests

```csharp
// Should detect system folders
Assert.IsFalse(SafeFolderPicker.IsSystemFolder("C:\\Windows"));
Assert.IsFalse(SafeFolderPicker.IsSystemFolder("C:\\Windows\\System32"));
Assert.IsFalse(SafeFolderPicker.IsSystemFolder("C:\\Program Files"));

// Should allow user folders
Assert.IsTrue(SafeFolderPicker.IsSystemFolder("C:\\Users\\John\\Documents"));
Assert.IsTrue(SafeFolderPicker.IsSystemFolder("D:\\Projects"));
```

## Best Practices

✅ **DO:**
- Always validate selected folders
- Show clear error messages
- Suggest safe locations (Documents, Desktop, Downloads)
- Log blocked selection attempts

❌ **DON'T:**
- Assume picker blocks system folders
- Run app with elevated privileges unless necessary
- Trust user input without validation

## Related Resources
- [File access permissions](https://learn.microsoft.com/windows/apps/develop/data-access/file-access-permissions)
- [Security best practices](https://learn.microsoft.com/windows/apps/develop/security)
