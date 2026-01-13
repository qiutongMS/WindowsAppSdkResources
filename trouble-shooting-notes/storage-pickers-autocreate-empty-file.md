# Troubleshooting Guide: FileSavePicker Auto-Creates Empty Files

## Symptom
When using `FileSavePicker.PickSaveFileAsync()`, an empty file is automatically created on disk as soon as the user clicks "Save" button, even before you write any content to it.

## Affected Components
- **Microsoft.Windows.Storage.Pickers.FileSavePicker**
- **Windows App SDK**: 1.8.2+

## Related Issues
- [#5976](https://github.com/microsoft/WindowsAppSDK/issues/5976) - FileSavePicker auto creates empty file after clicking OK button

## Root Cause

This is **standard Windows shell behavior**, not a bug. The Windows file picker creates the file immediately when the user confirms the save location to:
1. Reserve the filename
2. Ensure write permissions
3. Prevent race conditions with other applications

The same behavior occurs with Win32 `IFileSaveDialog` and all Windows file save dialogs.

## Solution

Simply overwrite the auto-created empty file with your actual content. This is the standard approach used by most Windows applications:

```csharp
var saveDialog = new FileSavePicker(AppWindow.Id)
{
    DefaultFileExtension = ".txt",
};

saveDialog.FileTypeChoices.Add("Text Files", [".txt"]);
saveDialog.FileTypeChoices.Add("JSON Files", [".json"]);
saveDialog.FileTypeChoices.Add("XML Files", [".xml"]);

var file = await saveDialog.PickSaveFileAsync();

if (file != null)
{
    // File already exists as empty file at this point
    // Just write your content - it will overwrite the empty file
    await FileIO.WriteTextAsync(file, yourContent);
    
    // Or use streams
    using (var stream = await file.OpenStreamForWriteAsync())
    {
        // Write your data
    }
}
```

## Alternative: Delete and Recreate (Not Recommended)

If you absolutely need to delete the empty file first:

```csharp
var file = await saveDialog.PickSaveFileAsync();

if (file != null)
{
    var folder = await file.GetParentAsync();
    var filename = file.Name;
    
    // Delete the auto-created empty file
    await file.DeleteAsync();
    
    // Recreate with your content
    var newFile = await folder.CreateFileAsync(
        filename, 
        CreationCollisionOption.ReplaceExisting);
    
    await FileIO.WriteTextAsync(newFile, yourContent);
}
```

**Why not recommended:**
- Extra I/O operations
- Potential race condition if another process creates file with same name
- Complicates error handling

## Verification

Test that file is created even without writing:

```csharp
var file = await saveDialog.PickSaveFileAsync();

if (file != null)
{
    // Check file exists and is empty
    var properties = await file.GetBasicPropertiesAsync();
    Debug.WriteLine($"File size: {properties.Size} bytes"); // Will be 0
    
    bool exists = File.Exists(file.Path); // Will be true
}
```

## Expected Behavior

| Action | File State |
|--------|-----------|
| User clicks "Save" | Empty file created on disk |
| You call `WriteTextAsync` | File contains your content |
| User cancels | No file created (returns null) |

## Comparison with Legacy Pickers

**Windows.Storage.Pickers.FileSavePicker** (UWP):
- Did NOT auto-create files
- File only created when you wrote to it

**Microsoft.Windows.Storage.Pickers.FileSavePicker** (Windows App SDK 1.8+):
- Auto-creates empty file (matches Win32 behavior)
- More consistent with standard Windows applications

## Additional Resources
- [FileSavePicker Class Documentation](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.windows.storage.pickers.filesavepicker)
- [File Access and Permissions](https://learn.microsoft.com/windows/apps/develop/files/file-access-permissions)

## Related TSGs
- [Storage Pickers FileTypeChoices Configuration](storage-pickers-filetypechoices-configuration.md)
- [Storage Pickers Initialization Errors](storage-pickers-initialization-errors.md)
