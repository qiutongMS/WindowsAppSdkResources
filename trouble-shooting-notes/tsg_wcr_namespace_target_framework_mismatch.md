# WCR Namespace Not Found - Target Framework Mismatch

**Error Code:** CS0246 (Namespace not found)  
**Affected Area:** WCR APIs - Project target framework configuration  
**Common Platforms:** All platforms

---

## Symptom Overview

You have the correct SDK version and packages, but WinRT namespaces (including WCR) aren't available because your project targets generic .NET instead of Windows-specific .NET.

**You might see:**
- WCR namespaces not found even with correct SDK
- Other `Windows.*` WinRT namespaces also missing
- Console apps or class libraries can't find WinRT APIs
- Documentation examples don't compile

---

## Related Issues

- [#5127](https://github.com/microsoft/WindowsAppSDK/issues/5127) - Unable to use Microsoft.Windows.Vision/Imaging

---

## Root Cause

WCR APIs are WinRT components that require Windows-specific target frameworks. Using generic `net8.0` or `net9.0` doesn't provide WinRT projections needed for WCR namespaces.

---

## Solution

### For WinUI 3 Apps (Typical)

Update `.csproj`:

```xml
<PropertyGroup>
  <!-- ❌ WRONG -->
  <!-- <TargetFramework>net8.0</TargetFramework> -->
  
  <!-- ✅ CORRECT -->
  <TargetFramework>net8.0-windows10.0.22621.0</TargetFramework>
  
  <!-- Optional: Minimum supported OS -->
  <TargetPlatformMinVersion>10.0.19041.0</TargetPlatformMinVersion>
  
  <!-- For WinUI 3 -->
  <UseWinUI>true</UseWinUI>
</PropertyGroup>
```

### For Console Apps Using WCR

```xml
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <TargetFramework>net8.0-windows10.0.22621.0</TargetFramework>
  
  <!-- Don't need WinForms/WPF for WCR -->
  <UseWindowsForms>false</UseWindowsForms>
  <UseWPF>false</UseWPF>
  
  <!-- Enable WinRT support -->
  <EnableDefaultItems>true</EnableDefaultItems>
</PropertyGroup>
```

### For Class Libraries

```xml
<PropertyGroup>
  <TargetFramework>net8.0-windows10.0.22621.0</TargetFramework>
  
  <!-- Allow consuming projects to determine min version -->
  <TargetPlatformMinVersion>10.0.19041.0</TargetPlatformMinVersion>
</PropertyGroup>
```

---

## Target Framework Versions

### Recommended Target Frameworks

- **net8.0-windows10.0.22621.0** - Windows 11 (22H2+)
- **net8.0-windows10.0.19041.0** - Windows 10 version 2004+
- **net9.0-windows10.0.22621.0** - .NET 9 with Windows 11

### Minimum OS Versions

- **10.0.22621.0** - Windows 11 22H2 (recommended for WCR)
- **10.0.19041.0** - Windows 10 version 2004 (minimum for WindowsAppSDK)

---

## Verification

### Test WinRT Projection Works

```csharp
using Microsoft.Windows.AI.Generative;
using Windows.Foundation;  // WinRT namespace
using Windows.Storage;     // WinRT namespace

// All should compile if TFM is correct
var model = LanguageModel.IsAvailable();
IAsyncOperation<string> asyncOp = null;  // WinRT type
StorageFile file = null;  // WinRT type
```

### Check Actual Target Framework

```powershell
# Check compiled assembly's target framework
dotnet build
$dll = "bin\Debug\net8.0-windows10.0.22621.0\YourApp.dll"
[System.Reflection.Assembly]::LoadFile((Resolve-Path $dll).Path).GetCustomAttributes([System.Runtime.Versioning.TargetFrameworkAttribute], $false).FrameworkName
```

---

## Common Scenarios

### Migrating from Generic .NET Console App

**Before:**
```xml
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <TargetFramework>net8.0</TargetFramework>
</PropertyGroup>
```

**After:**
```xml
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <TargetFramework>net8.0-windows10.0.22621.0</TargetFramework>
  <TargetPlatformMinVersion>10.0.19041.0</TargetPlatformMinVersion>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.7.250127003-experimental3" />
</ItemGroup>
```

### Class Library for WCR

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows10.0.22621.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.7.250127003-experimental3" />
  </ItemGroup>
</Project>
```

---

## Troubleshooting

### Still can't find namespaces after TFM change?

1. **Clean and rebuild:**
   ```powershell
   dotnet clean
   Remove-Item bin, obj -Recurse -Force
   dotnet build
   ```

2. **Verify NuGet restore:**
   ```powershell
   dotnet restore --force
   ```

3. **Check for CsWinRT package (rare edge cases):**
   ```xml
   <!-- Usually not needed, but some scenarios require it -->
   <PackageReference Include="Microsoft.Windows.CsWinRT" Version="2.0.1" />
   ```

4. **Reload IDE:**
   - Visual Studio: Close and reopen solution
   - VS Code: Reload window (Ctrl+Shift+P → "Reload Window")

---

## Multi-Targeting (Advanced)

If you need to support both Windows and cross-platform:

```xml
<PropertyGroup>
  <TargetFrameworks>net8.0;net8.0-windows10.0.22621.0</TargetFrameworks>
</PropertyGroup>

<ItemGroup Condition="'$(TargetFramework)' == 'net8.0-windows10.0.22621.0'">
  <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.7.250127003-experimental3" />
</ItemGroup>
```

Use conditional compilation:
```csharp
#if WINDOWS
using Microsoft.Windows.AI.Generative;

public async Task<string> GenerateAsync(string prompt)
{
    var model = await LanguageModel.CreateAsync();
    return await model.GenerateResponseAsync(prompt);
}
#else
public async Task<string> GenerateAsync(string prompt)
{
    throw new PlatformNotSupportedException("WCR only available on Windows");
}
#endif
```

---

## References

- [Issue #5127: Unable to use Vision/Imaging namespaces](https://github.com/microsoft/WindowsAppSDK/issues/5127)
- [Target Frameworks Documentation](https://learn.microsoft.com/dotnet/standard/frameworks)
- [Windows-specific TFMs](https://learn.microsoft.com/windows/apps/windows-app-sdk/set-up-your-development-environment#target-frameworks)

---

**Last Updated:** 2026-01-04  
**Confidence:** 0.89

## Changelog

**2026-01-04:**
- Split from namespace_not_found.md
- Added multi-targeting guidance
