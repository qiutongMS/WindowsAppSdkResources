# Troubleshooting Guide: StorageFile UnauthorizedAccessException

## Symptom
- `StorageFile.GetFileFromPathAsync()` throws `UnauthorizedAccessException` (0x80070005)
- File exists and is readable in File Explorer
- Same code works in Win32 apps but fails in WinUI 3

## Affected Components
- Windows.Storage.StorageFile, StorageFolder
- Windows App SDK: All versions
- Issue [#4844](https://github.com/microsoft/WindowsAppSDK/issues/4844)

## Root Cause

WinUI 3 apps have restricted file access compared to Win32:
- **Missing capabilities** - No `broadFileSystemAccess` in manifest
- **Invalid locations** - Path outside app's allowed folders
- **No user consent** - Files not picked by user or in Future Access List

## Solutions

### Solution 1: Use File Pickers (Recommended)
User grants access via picker:

```csharp
var picker = new FileOpenPicker(AppWindow.Id);
picker.FileTypeFilter.Add(".txt");
var file = await picker.PickSingleFileAsync();

if (file != null)
{
    var content = await FileIO.ReadTextAsync(file);
    
    // Save to Future Access List
    var token = StorageApplicationPermissions.FutureAccessList.Add(file);
}
```

### Solution 2: Add broadFileSystemAccess Capability
For unrestricted file access:

```xml
<Package xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities">
  <Capabilities>
    <rescap:Capability Name="broadFileSystemAccess" />
  </Capabilities>
</Package>
```

User must enable in Settings > Privacy > File system.

Check permission:
```csharp
try
{
    var testFile = await StorageFile.GetFileFromPathAsync(@"C:\Users\Public\test.txt");
    return true; // Access granted
}
catch (UnauthorizedAccessException)
{
    // Show dialog: "Enable file access in Settings > Privacy > File system"
    await Windows.System.Launcher.LaunchUriAsync(
        new Uri("ms-settings:privacy-broadfilesystemaccess"));
    return false;
}
```

### Solution 3: Use Future Access List
Store user-granted access:

```csharp
// After picker selection
var token = StorageApplicationPermissions.FutureAccessList.Add(file, "config-file");
ApplicationData.Current.LocalSettings.Values["ConfigToken"] = token;

// Later retrieval
if (ApplicationData.Current.LocalSettings.Values.TryGetValue("ConfigToken", out object savedToken))
{
    var file = await StorageApplicationPermissions.FutureAccessList
        .GetFileAsync(savedToken.ToString());
}
```

### Solution 4: Use App Local Storage
For app-specific files (always accessible):

```csharp
var localFolder = ApplicationData.Current.LocalFolder;
var file = await localFolder.CreateFileAsync("data.txt", 
    CreationCollisionOption.OpenIfExists);
await FileIO.WriteTextAsync(file, "content");
```

## Access Requirements

| Location | GetFileFromPathAsync | broadFileSystemAccess | Picker + Future Access |
|----------|---------------------|----------------------|------------------------|
| App Local Folder | ✅ Always | Not needed | Not needed |
| User Documents | ❌ Fails | ✅ Required | ✅ Works |
| C:\Windows | ❌ Fails | ❌ Still fails | ❌ Not allowed |
| Arbitrary Path | ❌ Fails | ✅ Required | ✅ After picked |

## Verification

```csharp
// ✅ Should always work
var localFile = await ApplicationData.Current.LocalFolder
    .CreateFileAsync("test.txt", CreationCollisionOption.ReplaceExisting);

// ❌ May fail without broadFileSystemAccess
try
{
    var publicFile = await StorageFile.GetFileFromPathAsync(
        @"C:\Users\Public\Documents\test.txt");
}
catch (UnauthorizedAccessException)
{
    Debug.WriteLine("❌ Broad file access not granted");
}
```

## Best Practices
- ✅ Use pickers for user files (no capabilities needed)
- ✅ Use Future Access List to persist access
- ✅ Use app local storage for app data
- ❌ Avoid requiring broadFileSystemAccess if possible
- ❌ Never assume arbitrary paths work without capabilities

## Related Resources
- [File access permissions](https://learn.microsoft.com/windows/apps/develop/data-access/file-access-permissions)
- [FutureAccessList API](https://learn.microsoft.com/uwp/api/windows.storage.accesscache.storageapplicationpermissions.futureaccesslist)
