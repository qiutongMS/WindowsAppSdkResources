# Troubleshooting Guide: FileSavePicker FileTypeChoices Configuration Issues

## Symptom
FileTypeChoices in FileSavePicker don't behave as expected:
- Default file extension ignored when FileTypeChoices is populated
- FileTypeChoices sorted alphabetically, not insertion order
- FileTypeChoices entries appear blank (SDK 1.8.0 only)

## Affected Components
- Microsoft.Windows.Storage.Pickers.FileSavePicker
- Windows App SDK: 1.8.0, 1.8.2, 1.8.3
- Issues: [#5975](https://github.com/microsoft/WindowsAppSDK/issues/5975), [#5827](https://github.com/microsoft/WindowsAppSDK/issues/5827), [#5837](https://github.com/microsoft/WindowsAppSDK/issues/5837)

## Root Causes

1. **DefaultFileExtension ignored** - When `FileTypeChoices` is populated, first alphabetically-sorted entry becomes default
2. **Alphabetical sorting** - Unlike UWP which preserved insertion order, SDK sorts by display name
3. **Blank entries** - Bug in SDK 1.8.0 (fixed in 1.8.2+)

## Solutions

### Solution 1: Control Default via SuggestedFileName

Include extension in suggested filename:

```csharp
var savePicker = new FileSavePicker(AppWindow.Id);

savePicker.FileTypeChoices.Add("XML Documents", [".xml"]);
savePicker.FileTypeChoices.Add("JSON Files", [".json"]);
savePicker.FileTypeChoices.Add("Text Files", [".txt"]);

// Extension in filename makes it default
savePicker.SuggestedFileName = "Document.xml"; // Makes .xml default
```

### Solution 2: Control Order with Prefix Naming

Force order by prefixing display names:

```csharp
var savePicker = new FileSavePicker(AppWindow.Id);

// Numeric prefixes (sorted alphabetically)
savePicker.FileTypeChoices.Add("1. XML (Preferred)", [".xml"]);
savePicker.FileTypeChoices.Add("2. JSON", [".json"]);  
savePicker.FileTypeChoices.Add("3. Text", [".txt"]);
```

**Note:** Prefixes are visible to users (workaround only).

### Solution 3: Fix Blank Entries by Upgrading

If entries appear blank, upgrade to SDK 1.8.2+:

```xml
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.8.251003001" />
```

Bug was fixed in 1.8.2+.

### Solution 4: Use Single FileTypeChoice

For single default extension:

```csharp
var savePicker = new FileSavePicker(AppWindow.Id)
{
    DefaultFileExtension = ".xml", // Works when FileTypeChoices is empty
    SuggestedFileName = "Document"
};

// Don't add FileTypeChoices if you want DefaultFileExtension to work
var file = await savePicker.PickSaveFileAsync();
```

Or add only one choice:

```csharp
var savePicker = new FileSavePicker(AppWindow.Id);
savePicker.FileTypeChoices.Add("XML Files", [".xml"]);
savePicker.SuggestedFileName = "Document.xml";
```

## Behavior Comparison

| Feature | Windows.Storage.Pickers | Microsoft.Windows.Storage.Pickers |
|---------|------------------------|-----------------------------------|
| DefaultFileExtension with FileTypeChoices | ✅ Works | ❌ Ignored |
| FileTypeChoices order | ✅ Insertion order | ❌ Alphabetical |
| FileTypeChoices display | ✅ Works | ⚠️ Broken in 1.8.0, ✅ Fixed in 1.8.2+ |

## Complete Example

```csharp
private async Task<StorageFile> ShowSavePickerAsync()
{
    var savePicker = new FileSavePicker(AppWindow.Id)
    {
        SuggestedStartLocation = PickerLocationId.DocumentsLibrary
    };

    // Add in alphabetical order (how they'll display)
    savePicker.FileTypeChoices.Add("JSON Files", [".json"]);
    savePicker.FileTypeChoices.Add("Text Files", [".txt"]);
    savePicker.FileTypeChoices.Add("XML Documents", [".xml"]);
    
    // Control default via filename extension
    savePicker.SuggestedFileName = "MyDocument.xml";

    return await savePicker.PickSaveFileAsync();
}
```

## Verification

Test which extension is default:

```csharp
var savePicker = new FileSavePicker(AppWindow.Id);
savePicker.FileTypeChoices.Add("XML", [".xml"]);
savePicker.FileTypeChoices.Add("JSON", [".json"]);
savePicker.SuggestedFileName = "test.xml";

var file = await savePicker.PickSaveFileAsync();
// Verify .xml is pre-selected in dropdown
```

## Related Resources
- [FileSavePicker API](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.windows.storage.pickers.filesavepicker)
- [Migration from Windows.Storage.Pickers](https://learn.microsoft.com/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/storage)
