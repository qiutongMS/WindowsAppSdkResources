# Troubleshooting Guide: Storage Pickers PublishAot InvalidCastException

## Symptom
With `PublishAot` enabled, storage pickers throw:
```
System.InvalidCastException: Unable to cast object of type 'System.__ComObject' 
to type 'ABI.Microsoft.Windows.Storage.Pickers.IFileOpenPickerMethods'
```

## Affected Components
- Microsoft.Windows.Storage.Pickers (all picker types)
- Windows App SDK: 1.6.0+
- Issue [#4743](https://github.com/microsoft/WindowsAppSDK/issues/4743)

## Root Cause

Native AOT compilation removes runtime type information required for:
- WinRT COM interop
- Dynamic interface casting
- Type marshaling between .NET and Windows Runtime

Storage pickers depend on runtime type resolution unavailable in PublishAot.

## Solutions

### Solution 1: Disable PublishAot (Recommended)

Native AOT is **not supported** with Windows App SDK WinRT components.

```xml
<PropertyGroup>
  <!-- ❌ Remove this -->
  <!-- <PublishAot>true</PublishAot> -->
  
  <!-- ✅ Use these alternatives -->
  <PublishReadyToRun>true</PublishReadyToRun>
  <SelfContained>true</SelfContained>
  <TieredCompilation>true</TieredCompilation>
</PropertyGroup>
```

### Solution 2: Conditional Compilation

Isolate picker code from PublishAot builds:

```xml
<PropertyGroup>
  <PublishAot Condition="'$(Configuration)' != 'Debug'">true</PublishAot>
  <DefineConstants Condition="'$(PublishAot)' == 'true'">NATIVE_AOT</DefineConstants>
</PropertyGroup>
```

```csharp
private async Task<StorageFile> OpenFileAsync()
{
#if NATIVE_AOT
    throw new NotSupportedException(
        "File pickers not supported in Native AOT builds. Use standard build.");
#else
    var picker = new FileOpenPicker(AppWindow.Id);
    picker.FileTypeFilter.Add("*");
    return await picker.PickSingleFileAsync();
#endif
}
```

### Solution 3: Win32 Fallback (Advanced)

Use Win32 `IFileOpenDialog` directly:

```csharp
// Requires Microsoft.Windows.CsWin32 package
#if NATIVE_AOT
using Windows.Win32.UI.Shell;

unsafe
{
    IFileOpenDialog* dialog = null;
    PInvoke.CoCreateInstance(
        typeof(FileOpenDialog).GUID,
        null,
        CLSCTX.CLSCTX_INPROC_SERVER,
        typeof(IFileOpenDialog).GUID,
        (void**)&dialog);
    
    dialog->Show(new HWND(hwnd));
    
    IShellItem* item;
    dialog->GetResult(&item);
    
    PWSTR path;
    item->GetDisplayName(SIGDN.SIGDN_FILESYSPATH, &path);
    var result = new string(path);
    PInvoke.CoTaskMemFree(path);
}
#endif
```

**Warning:** Complex, requires extensive Win32 interop knowledge and manual memory management.

## Performance Comparison

| Configuration | Startup | Size | Pickers | Recommended |
|--------------|---------|------|---------|-------------|
| Debug (JIT) | Baseline | Baseline | ✅ Works | Dev only |
| Release (JIT) | -10% | Baseline | ✅ Works | ✅ Production |
| PublishReadyToRun | -30% | +15% | ✅ Works | ✅ Recommended |
| PublishAot | -50% | -40% | ❌ Breaks | ❌ Not supported |

## Verification

Check for PublishAot:

```powershell
# Search .csproj
Select-String -Path *.csproj -Pattern "PublishAot"

# Build output should show:
# ✅ Using ReadyToRun compilation
# ❌ Generating native code (PublishAot=true)
```

After removing `PublishAot`, pickers should work:

```csharp
var picker = new FileOpenPicker(AppWindow.Id);
picker.FileTypeFilter.Add(".txt");
var file = await picker.PickSingleFileAsync(); // ✅ No InvalidCastException
```

## Known Limitations
- PublishAot not supported with Windows App SDK WinRT as of v1.6
- No official workaround from Microsoft
- Affects all WinRT COM interop, not just pickers

## Related Resources
- [Native AOT deployment](https://learn.microsoft.com/dotnet/core/deploying/native-aot/)
- [ReadyToRun compilation](https://learn.microsoft.com/dotnet/core/deploying/ready-to-run)
- Track issue [#4743](https://github.com/microsoft/WindowsAppSDK/issues/4743)
