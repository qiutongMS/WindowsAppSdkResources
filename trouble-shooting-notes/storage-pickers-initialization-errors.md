# Troubleshooting Guide: Storage Pickers Initialization Errors

## Symptom
Storage pickers fail with COM errors:
- **0x80004005** (E_FAIL) - Generic COM error
- **0x80070490** (ERROR_NOT_FOUND) - "Element not found"
- `COMException` during picker creation or display

## Affected Components
- Microsoft.Windows.Storage.Pickers (all picker types)
- Windows App SDK: 1.6.0 - 1.8.0+
- Issues: [#4625](https://github.com/microsoft/WindowsAppSDK/issues/4625), [#5749](https://github.com/microsoft/WindowsAppSDK/issues/5749), [#5747](https://github.com/microsoft/WindowsAppSDK/issues/5747)

## Root Causes

### Cause 1: Missing Window Handle
Pickers require valid window handle (HWND):

```csharp
// ❌ WRONG - No window association
var picker = new FolderPicker(); 
await picker.PickSingleFolderAsync(); // COM error!

// ✅ CORRECT - Window handle provided
var picker = new FolderPicker(AppWindow.Id);
```

### Cause 2: Empty FileTypeFilter
Some pickers crash if `FileTypeFilter` is empty:

```csharp
// ❌ May crash
var picker = new FolderPicker(AppWindow.Id);
await picker.PickSingleFolderAsync();

// ✅ Always initialize
var picker = new FolderPicker(AppWindow.Id);
picker.FileTypeFilter.Add("*");
```

### Cause 3: Self-Contained Deployment Issues
Self-contained apps may miss required COM components or Windows App SDK runtime DLLs.

## Solutions

### Solution 1: Always Provide Window Handle

```csharp
using Microsoft.UI.Windowing;

public sealed partial class MainWindow : Window
{
    private AppWindow AppWindow => AppWindow.GetFromWindowId(
        Win32Interop.GetWindowIdFromWindow(WindowNative.GetWindowHandle(this)));
    
    private async Task<StorageFolder> PickFolderAsync()
    {
        var picker = new FolderPicker(AppWindow.Id)
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };
        picker.FileTypeFilter.Add("*"); // Required!
        
        return await picker.PickSingleFolderAsync();
    }
}
```

### Solution 2: Fix Self-Contained Deployment

**Option A: Use Framework-Dependent**

```xml
<PropertyGroup>
  <WindowsAppSDKSelfContained>false</WindowsAppSDKSelfContained>
  <WindowsPackageType>None</WindowsPackageType>
</PropertyGroup>
```

**Option B: Ensure Self-Contained Runtime Files**

```xml
<PropertyGroup>
  <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
  <WindowsAppSdkDeploymentManagerInitialize>true</WindowsAppSdkDeploymentManagerInitialize>
  <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
</PropertyGroup>
```

Initialize in App.xaml.cs:

```csharp
using Microsoft.Windows.ApplicationModel.DynamicDependency;

public App()
{
    Bootstrap.TryInitialize(0x00010000, out _);
    this.InitializeComponent();
}
```

### Solution 3: Always Initialize FileTypeFilter

```csharp
// FolderPicker
var folderPicker = new FolderPicker(AppWindow.Id);
folderPicker.FileTypeFilter.Add("*"); // Required!

// FileOpenPicker
var filePicker = new FileOpenPicker(AppWindow.Id);
filePicker.FileTypeFilter.Add(".txt"); // Required!
```

### Solution 4: Verify Window Setup

```csharp
private void ValidateWindowHandle()
{
    var hwnd = WindowNative.GetWindowHandle(this);
    if (hwnd == IntPtr.Zero)
        throw new InvalidOperationException("Window handle is null");
    
    var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
    if (windowId.Value == 0)
        throw new InvalidOperationException("Window ID is invalid");
    
    Debug.WriteLine($"✅ Window handle valid: {hwnd}");
}
```

## Complete Working Example

```csharp
using Microsoft.UI.Windowing;
using Microsoft.Windows.Storage.Pickers;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
    }
    
    private async Task<StorageFolder> ShowFolderPickerAsync()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        
        var picker = new FolderPicker(windowId)
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };
        picker.FileTypeFilter.Add("*"); // Required!
        
        return await picker.PickSingleFolderAsync();
    }
}
```

## Verification Checklist

- [ ] Window fully initialized (not in constructor)
- [ ] `WindowNative.GetWindowHandle()` returns non-zero
- [ ] Picker created with `new Picker(windowId)` constructor
- [ ] `FileTypeFilter.Add()` called at least once
- [ ] If self-contained: `Bootstrap.TryInitialize()` in App.xaml.cs
- [ ] If self-contained: Verify runtime DLLs in output folder

## Related Resources
- [File pickers documentation](https://learn.microsoft.com/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/storage-pickers)
- [Self-contained deployment](https://learn.microsoft.com/windows/apps/windows-app-sdk/deployment-architecture)
