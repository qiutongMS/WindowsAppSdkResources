# Self-Contained Single-File App Crashes After Renaming - XamlParseException

**Keywords:** self-contained publishsinglefile xamlparseexception rename-exe resource-loading deployment trimmed  
**Last Updated:** 2026-01-12  
**Status:** Open (Regression in 1.8-experimental2, still present in 2.0-experimental3)

## Related Issues

- microsoft/WindowsAppSDK#5457 - Renaming self-contained exe breaks XAML parsing (1.8-experimental2 regression)

## Symptom

Publishing a WinUI 3 app as **self-contained single-file** works, but **renaming the .exe** causes immediate crash on launch with `XamlParseException`.

**Repro:**
1. Publish with `PublishSingleFile=true`, `WindowsAppSDKSelfContained=true`, `SelfContained=true`, `PublishTrimmed=true`
2. Rename `MyApp.exe` → `RenamedApp.exe`
3. Run `RenamedApp.exe` → **Crash in Window.InitializeComponent()**

## Error Example

```
Unhandled exception: Microsoft.UI.Xaml.Markup.XamlParseException: XAML parsing failed.
   at void Microsoft.UI.Xaml.Application.LoadComponent(object, Uri, ComponentResourceLocation)
   at void Window.InitializeComponent() in Window.g.i.cs:line 69
   at Window..ctor() in Window.xaml.cs:line 46
   at void App.OnLaunched(LaunchActivatedEventArgs) in App.xaml.cs:line 45
```

**Project settings:**
```xml
<PropertyGroup>
  <OutputType>WinExe</OutputType>
  <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
  <SelfContained>true</SelfContained>
  <PublishSingleFile>true</PublishSingleFile>
  <PublishTrimmed>true</PublishTrimmed>
</PropertyGroup>
```

## Root Cause

In **WindowsAppSDK 1.8-experimental2+**, XAML resource loading logic changed to depend on the **original executable name** for self-contained single-file deployments.

**What happens:**
1. During publish, XAML resources are embedded with references to original exe name (e.g., `MyApp.exe`)
2. Resources are bundled into the single-file executable
3. At runtime, resource loader uses **current exe name** to locate resources
4. If exe is renamed, resource paths don't match → `XamlParseException`

**Why this is a regression:**
- **1.8-experimental1:** Worked fine (resource loading used embedded manifest, not exe name)
- **1.8-experimental2+:** Breaks (resource paths now reference exe name)
- **2.0-experimental3:** Still broken

**Specific failure point:**
- `Application.LoadComponent()` builds URI based on current process name
- Expected URI: `ms-appx:///MyApp/Window.xaml`
- Actual URI after rename: `ms-appx:///RenamedApp/Window.xaml`
- Resource not found → Exception

## Solutions

### ✅ Solution: Don't Rename the Published Executable

**This is a limitation, not a bug you can work around.**

**If you need custom exe name:**

**Option 1: Set output name in project file**
```xml
<PropertyGroup>
  <AssemblyName>MyCustomName</AssemblyName>
  <!-- Published exe will be MyCustomName.exe (no rename needed) -->
</PropertyGroup>
```

**Option 2: Use MSBuild property during publish**
```powershell
dotnet publish -p:AssemblyName=CustomAppName -c Release -r win-x64 --self-contained
# Produces CustomAppName.exe directly
```

**Option 3: Create shortcut or wrapper**

If you must distribute under different name:

```powershell
# Create shortcut
$WshShell = New-Object -ComObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut("$PWD\CustomName.lnk")
$Shortcut.TargetPath = "$PWD\MyApp.exe"
$Shortcut.Save()
```

Or create small launcher wrapper (separate .exe that launches the real app).

### ❌ Workaround: Revert to 1.8-experimental1 (Not Recommended)

If you absolutely need rename capability and can't ship yet:

```xml
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.8.250515001-experimental1" />
```

**Not recommended because:**
- Experimental1 is outdated
- Missing fixes from experimental2+
- Not a long-term solution

### Alternative: Disable Single-File Publishing

If renaming is critical, don't use `PublishSingleFile`:

```xml
<PropertyGroup>
  <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
  <SelfContained>true</SelfContained>
  <PublishSingleFile>false</PublishSingleFile> <!-- Changed -->
  <!-- Now you can rename MyApp.exe freely -->
</PropertyGroup>
```

**Trade-off:**
- ✅ Can rename .exe
- ❌ Publish folder has many .dll files (not single-file)

## Why This Matters

**Common scenarios affected:**
1. **Branding/whitelabel apps** - Need to rename exe per customer
2. **Automated builds** - CI/CD renames exe with version (e.g., `App_v1.2.exe`)
3. **Multi-tenant deployments** - Different exe names per tenant
4. **Portable apps** - Users expect ability to rename

**Current limitation:** These scenarios **not supported** with single-file publish in 1.8+.

## Prevention

**For new projects:**

1. **Set AssemblyName upfront** - Decide exe name before first publish
2. **Test rename scenario early** - Verify if your deployment requires renaming
3. **Document limitation** - Add note in README if single-file is used
4. **Consider non-single-file** - If rename is critical, disable single-file publish

**Project template recommendation:**

```xml
<!-- Document this in project comments -->
<PropertyGroup>
  <!-- IMPORTANT: Do NOT rename published .exe when using PublishSingleFile=true -->
  <!-- If custom name needed, set AssemblyName here before publishing -->
  <AssemblyName>YourAppName</AssemblyName>
  <PublishSingleFile>true</PublishSingleFile>
</PropertyGroup>
```

## Verification

Test your published app:

```powershell
# After publish
$publishedExe = "publish\MyApp.exe"

# Test 1: Original name works
& $publishedExe
# Should launch successfully

# Test 2: Renamed fails (expected with single-file)
Copy-Item $publishedExe "publish\Renamed.exe"
& "publish\Renamed.exe"
# Will crash with XamlParseException if single-file
```

## Reporting

**This is a regression** that should be fixed. If this affects you:

1. Add comment to microsoft/WindowsAppSDK#5457
2. Describe your use case (why renaming is needed)
3. Vote/react to increase priority

**Current label:** `area-DeveloperTools` (may need re-labeling to `area-Deployment`)

## Version Information

- **Affected versions:** 
  - ✅ 1.8-experimental1: Works
  - ❌ 1.8-experimental2 → 1.8 stable: Broken
  - ❌ 2.0-experimental3: Still broken
- **Status:** Open regression (introduced in experimental2)
- **Workaround:** Don't rename exe OR don't use PublishSingleFile
- **Expected fix:** Future release (request fix on GitHub issue)
