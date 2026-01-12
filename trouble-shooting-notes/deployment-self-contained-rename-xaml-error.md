# XamlParseException After Renaming Self-Contained Executable

**Keywords:** XamlParseException, PublishSingleFile, WindowsAppSDKSelfContained, rename exe, XAML parsing failed, self-contained deployment, InitializeComponent error

**Error Example:**
```
Unhandled exception: Microsoft.UI.Xaml.Markup.XamlParseException: XAML parsing failed
   at Microsoft.UI.Xaml.Application.LoadComponent(...)
   at MyApp.MainWindow.InitializeComponent() in MainWindow.xaml.cs:line 28
   at MyApp.MainWindow..ctor() in MainWindow.xaml.cs:line 23
```

**Occurs when:**
- Published with `<PublishSingleFile>true</PublishSingleFile>`
- Published with `<WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>`
- Renamed the output `.exe` file
- Run the renamed executable

---

## Quick Match

**You're seeing this if:**
- Published a self-contained single-file WinAppSDK app
- Renamed the `.exe` after publishing
- App crashes with `XamlParseException` on startup
- Error occurs in `Window.InitializeComponent()`
- Worked fine before renaming

→ Root cause: XAML resource loading is hardcoded to original exe name

---

## Related Issues

- [#5457](https://github.com/microsoft/WindowsAppSDK/issues/5457) - Renaming self-contained exe breaks XAML parsing

---

## Root Cause

**Why it happens:**

When you publish a WinAppSDK app with self-contained deployment and single-file packaging, the XAML resource loading system embeds **hardcoded references** to the original executable name.

**Build-time behavior:**
1. MSBuild generates XAML resource files (`.g.cs`, `.xbf`)
2. Resource paths are compiled with original exe name: `ms-appx:///MyOriginalApp/MainWindow.xaml`
3. Resources are embedded into the single-file exe with original name as key

**Runtime behavior after rename:**
1. App launches as `RenamedApp.exe`
2. XAML framework tries to load: `ms-appx:///MyOriginalApp/MainWindow.xaml`
3. ❌ Resource lookup fails because exe name is now `RenamedApp.exe`
4. `XamlParseException` thrown in `InitializeComponent()`

**This is a regression:**
- ✅ Worked in WindowsAppSDK 1.8-experimental1
- ❌ Broken in 1.8-experimental2
- ❌ Still broken in 2.0-experimental3

**Affected configurations:**
```xml
<PublishSingleFile>true</PublishSingleFile>
<WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
<SelfContained>true</SelfContained>
<PublishTrimmed>true</PublishTrimmed>  <!-- Optional, but common -->
```

---

## Solutions

### ✅ Solution 1: Use Original Executable Name (Recommended)

**Do NOT rename the published exe:**

```powershell
# Set desired name BEFORE publishing
# In .csproj:
<PropertyGroup>
  <AssemblyName>MyDesiredAppName</AssemblyName>
</PropertyGroup>

# Publish
dotnet publish -c Release
# Output: bin\Release\...\publish\MyDesiredAppName.exe
```

**Why this works:**
- XAML resources compiled with correct name
- No runtime lookup mismatch
- Supports all self-contained scenarios

**Pros:**
- ✅ No workarounds needed
- ✅ Fully supported configuration

**Cons:**
- Cannot change name post-publish

---

### Solution 2: Set AssemblyName Before Publishing

**Plan exe name in advance:**

```xml
<PropertyGroup>
  <OutputType>WinExe</OutputType>
  <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
  
  <!-- Set final exe name before first publish -->
  <AssemblyName>ProductionAppName</AssemblyName>
  
  <PublishSingleFile>true</PublishSingleFile>
  <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
  <SelfContained>true</SelfContained>
</PropertyGroup>
```

**Publish:**
```powershell
dotnet publish -c Release
# Output: ProductionAppName.exe (already named correctly)
```

**When to use:** Planning deployment name before first build

---

### ⚠️ Workaround 3: Republish After Rename

**If you must change the name:**

1. Change `<AssemblyName>` in .csproj to new name
2. Clean build output
3. Republish completely

```powershell
# Update .csproj AssemblyName to "NewName"

# Clean and republish
dotnet clean
dotnet publish -c Release

# Now output is NewName.exe with correct resources
```

**Why this works:**
- Forces XAML resource regeneration with new name
- Recompiles embedded resource paths

**Cons:**
- ❌ Full rebuild required
- ❌ Cannot rename existing published exe

---

### ❌ Workaround 4: Disable Single-File Publishing

**If renaming is required and republish not possible:**

Remove single-file publishing:

```xml
<PropertyGroup>
  <!-- Remove or set to false: -->
  <PublishSingleFile>false</PublishSingleFile>
  
  <!-- Keep self-contained: -->
  <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
  <SelfContained>true</SelfContained>
</PropertyGroup>
```

**Result:**
- Publish folder contains multiple DLLs
- Can rename main exe without breaking XAML
- Resource loading uses relative paths

**Cons:**
- ❌ Lose single-file deployment benefit
- ❌ More files to distribute

**When to use:** Single-file publishing is not critical requirement

---

## Prevention Best Practices

1. **Plan exe name early** - Set `<AssemblyName>` before first publish
2. **Avoid post-publish rename** - Build naming into project file
3. **Test immediately after publish** - Catch rename issues early
4. **Document deployment names** - Keep track of production exe names

**Recommended workflow:**

```xml
<!-- Development -->
<PropertyGroup Condition="'$(Configuration)'=='Debug'">
  <AssemblyName>MyApp.Dev</AssemblyName>
</PropertyGroup>

<!-- Production -->
<PropertyGroup Condition="'$(Configuration)'=='Release'">
  <AssemblyName>MyApp</AssemblyName>
</PropertyGroup>
```

---

## Diagnostic Steps

**Test if rename is the issue:**

```powershell
# Publish with original name
dotnet publish -c Release -p:PublishSingleFile=true

# Test original exe (should work)
.\bin\Release\net8.0-windows10.0.19041.0\publish\OriginalName.exe

# Rename and test
Copy-Item "OriginalName.exe" "RenamedApp.exe"
.\RenamedApp.exe
# If XamlParseException → Confirms issue
```

**Check embedded resource names:**

```powershell
# List resources in exe (requires ResourceViewer or similar)
# Resources will show paths like:
# ms-appx:///OriginalName/MainWindow.xaml
```

**Verify AssemblyName in project:**

```powershell
# Check current AssemblyName
Select-Xml -Path "*.csproj" -XPath "//AssemblyName" | ForEach-Object { $_.Node.InnerText }
```

---

## Workaround Status

- ⚠️ **Regression** - Worked in 1.8-experimental1, broken in experimental2+
- 📝 **Status:** Open, assigned to team-DeveloperTools
- 🔧 **Fix needed:** XAML resource loading should use runtime exe name

**Timeline:**
- Broken since: WindowsAppSDK 1.8-experimental2 (2024)
- Still broken: 2.0-experimental3 (as of 2025)
- Expected fix: TBD by team-DeveloperTools

**Recommended approach until fixed:**
- **Best:** Use Solution 1 (never rename)
- **Alternative:** Use Solution 2 (plan name early)
- **Emergency:** Use Workaround 4 (disable single-file)

---

## Impact Assessment

**High impact if:**
- Automated deployment scripts rename executables
- Localized builds need different exe names
- Versioned releases use name-based versioning (e.g., `MyApp-v1.2.exe`)

**Low impact if:**
- Exe name is fixed at design time
- Distribution uses installers (installer can name shortcut differently)

---

**Updated:** 2026-01-12 | **Confidence:** 0.94
