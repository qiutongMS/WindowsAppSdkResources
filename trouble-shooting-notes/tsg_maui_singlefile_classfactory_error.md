# .NET MAUI Single-File Executable ClassFactory Error

**Error Codes:** 0x80040154 (CLASS_E_CLASSNOTAVAILABLE), Exception from HRESULT  
**Affected Area:** .NET MAUI on Windows, WinRT Component Activation  
**Common Platforms:** Windows 10/11 with .NET MAUI apps published as single-file executables

---

## Symptom Overview

When deploying a .NET MAUI Windows application as a single-file executable using the PublishSingleFile option, the application crashes at startup with a ClassFactory error. This occurs specifically when trying to instantiate WinRT components required by MAUI's Windows implementation.

**You might see:**
- Application crashes immediately on launch
- Error: "Unable to get WinRT factory" or "ClassFactory cannot supply requested class"
- HRESULT: 0x80040154 (CLASS_E_CLASSNOTAVAILABLE)
- Works fine in Debug mode but fails in published single-file mode
- Issue specific to Windows; other platforms work correctly

---

## Related Issues

- [#6058](https://github.com/microsoft/WindowsAppSDK/issues/6058) - .NET MAUI single-file exe fails with ClassFactory error

---

## Quick Diagnosis

1. **Check if PublishSingleFile is enabled**
   ```xml
   <!-- In .csproj -->
   <PropertyGroup Condition="$(TargetFramework.Contains('-windows'))">
     <PublishSingleFile>true</PublishSingleFile>
   </PropertyGroup>
   ```

2. **Check if error occurs only with published single-file**
   ```powershell
   # Test regular publish (not single-file)
   dotnet publish -c Release -f net8.0-windows10.0.19041.0
   & "bin\Release\net8.0-windows10.0.19041.0\win10-x64\publish\YourApp.exe"
   # Works? Issue is related to single-file packaging
   ```

3. **Check error message for WinRT-related stack traces**
   ```
   System.Runtime.InteropServices.COMException (0x80040154): Retrieving the COM class factory for component with CLSID {...} failed
   at WinRT.ComWrappersSupport.FindObject[T](IntPtr thisPtr)
   at Microsoft.UI.Xaml.Application...
   ```

4. **Verify Windows App SDK version**
   ```xml
   <!-- Check .csproj or packages config -->
   <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.x.x" />
   ```

---

## Common Scenarios & Solutions

### Scenario 1: Single-File .NET MAUI App Crashes with 0x80040154

**Root Cause:** When .NET publishes a single-file executable, it bundles all DLLs into a single .exe file. However, Windows App SDK and WinRT components rely on COM registration that expects separate DLL files to exist on disk. The WinRT activation code can't find the necessary WinRT metadata and implementation DLLs when they're embedded in the single-file bundle.

**Related Issue(s):** [#6058](https://github.com/microsoft/WindowsAppSDK/issues/6058)

**Environment:**
- .NET MAUI 8.0+ or .NET 9.0+
- Windows App SDK (any version when used with MAUI)
- PublishSingleFile=true
- Windows 10 or Windows 11

**Error Details:**
```
Unhandled exception. System.Runtime.InteropServices.COMException (0x80040154): Retrieving the COM class factory for component with CLSID {GUID} failed due to the following error: 80040154 Class not registered (0x80040154 (REGDB_E_CLASSNOTREG)).
   at WinRT.ComWrappersSupport.FindObject[T](IntPtr thisPtr)
   at WinRT.ComWrappersSupport.CreateRcwForComObject[T](IntPtr ptr)
   at Microsoft.UI.Xaml.Application.InitializeComponent()
```

**Fix Option 1: Disable PublishSingleFile for Windows**

The most reliable solution is to disable single-file publishing specifically for the Windows target:

1. **Modify .csproj to exclude Windows from single-file**
   ```xml
   <PropertyGroup>
     <!-- Default for all platforms -->
     <PublishSingleFile>false</PublishSingleFile>
   </PropertyGroup>
   
   <!-- Enable only for non-Windows platforms -->
   <PropertyGroup Condition="!$(TargetFramework.Contains('-windows'))">
     <PublishSingleFile>true</PublishSingleFile>
   </PropertyGroup>
   
   <!-- Or explicitly for each platform -->
   <PropertyGroup Condition="$(TargetFramework.Contains('-android'))">
     <PublishSingleFile>true</PublishSingleFile>
   </PropertyGroup>
   
   <PropertyGroup Condition="$(TargetFramework.Contains('-ios'))">
     <PublishSingleFile>true</PublishSingleFile>
   </PropertyGroup>
   
   <PropertyGroup Condition="$(TargetFramework.Contains('-maccatalyst'))">
     <PublishSingleFile>true</PublishSingleFile>
   </PropertyGroup>
   ```

2. **Publish Windows target**
   ```powershell
   dotnet publish -c Release -f net8.0-windows10.0.19041.0 -r win10-x64
   ```

3. **Verify output contains separate DLLs**
   ```powershell
   Get-ChildItem "bin\Release\net8.0-windows10.0.19041.0\win10-x64\publish\*.dll" | Select-Object Name
   # Should show Microsoft.WindowsAppSDK.dll, WinRT.Runtime.dll, etc.
   ```

**Fix Option 2: Extract Required DLLs at Runtime (Advanced)**

If you must use single-file, extract WinRT DLLs before WinRT initialization:

1. **Mark critical DLLs for extraction**
   ```xml
   <PropertyGroup Condition="$(TargetFramework.Contains('-windows'))">
     <PublishSingleFile>true</PublishSingleFile>
     <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
   </PropertyGroup>
   
   <ItemGroup Condition="$(TargetFramework.Contains('-windows'))">
     <!-- Force extraction of WinRT-related DLLs -->
     <ResolvedFileToPublish Update="Microsoft.WindowsAppSDK.dll">
       <ExcludeFromSingleFile>true</ExcludeFromSingleFile>
     </ResolvedFileToPublish>
     <ResolvedFileToPublish Update="WinRT.Runtime.dll">
       <ExcludeFromSingleFile>true</ExcludeFromSingleFile>
     </ResolvedFileToPublish>
     <ResolvedFileToPublish Update="Microsoft.Windows.SDK.NET.dll">
       <ExcludeFromSingleFile>true</ExcludeFromSingleFile>
     </ResolvedFileToPublish>
   </ItemGroup>
   ```

2. **Rebuild and test**
   ```powershell
   dotnet clean
   dotnet publish -c Release -f net8.0-windows10.0.19041.0 -r win10-x64
   ```

**Fix Option 3: Use MSIX Packaging Instead**

For Windows distribution, use MSIX packaging which properly handles all dependencies:

1. **Add Windows Application Packaging Project**
   - Add new "Windows Application Packaging Project" to solution
   - Reference your .NET MAUI project
   - Configure Package.appxmanifest

2. **Update .csproj for MSIX compatibility**
   ```xml
   <PropertyGroup Condition="$(TargetFramework.Contains('-windows'))">
     <WindowsPackageType>MSIX</WindowsPackageType>
     <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
     <!-- Don't use PublishSingleFile with MSIX -->
     <PublishSingleFile>false</PublishSingleFile>
   </PropertyGroup>
   ```

3. **Build MSIX package**
   - Right-click packaging project → Publish → Create App Packages
   - Choose "Sideloading" for testing
   - Package will contain all dependencies correctly

4. **Distribute MSIX**
   ```powershell
   # Users install via:
   Add-AppxPackage YourApp_1.0.0.0_x64.msix
   ```

---

### Scenario 2: PublishSingleFile Works But App Size Is Huge

**Root Cause:** When excluding WinRT DLLs from single-file bundling (using Fix Option 2 above), the resulting deployment has both a large .exe file and separate DLL files, negating the size benefits of single-file deployment.

**Environment:**
- .NET MAUI with Windows App SDK
- PublishSingleFile with ExcludeFromSingleFile on critical DLLs

**Observation:**
```
YourApp.exe: 150 MB
Microsoft.WindowsAppSDK.dll: 12 MB
WinRT.Runtime.dll: 2 MB
[other DLLs]: 20 MB total
Total: ~184 MB
```

**Solution: Use PublishTrimmed with Careful Configuration**

1. **Enable trimming to reduce size**
   ```xml
   <PropertyGroup Condition="$(TargetFramework.Contains('-windows'))">
     <PublishTrimmed>true</PublishTrimmed>
     <TrimMode>link</TrimMode>
     <!-- Preserve WinRT types -->
     <TrimmerRootAssembly Include="WinRT.Runtime" />
     <TrimmerRootAssembly Include="Microsoft.WindowsAppSDK" />
   </PropertyGroup>
   ```

2. **Create TrimmerRoots.xml for MAUI**
   ```xml
   <!-- TrimmerRoots.xml -->
   <linker>
     <assembly fullname="Microsoft.Maui" />
     <assembly fullname="Microsoft.Maui.Controls" />
     <assembly fullname="Microsoft.WindowsAppSDK">
       <type fullname="*" preserve="all" />
     </assembly>
     <assembly fullname="WinRT.Runtime">
       <type fullname="*" preserve="all" />
     </assembly>
   </linker>
   ```

3. **Reference in .csproj**
   ```xml
   <ItemGroup>
     <TrimmerRootDescriptor Include="TrimmerRoots.xml" />
   </ItemGroup>
   ```

4. **Test thoroughly**
   ```powershell
   dotnet publish -c Release -f net8.0-windows10.0.19041.0 -r win10-x64 --self-contained
   # Test all app functionality, especially WinRT features
   ```

**Alternative: Accept Multi-File Distribution for Windows**

Windows users are accustomed to applications with multiple files. Consider:
- Package in a .zip file with clear instructions
- Use installer (WiX, Inno Setup, etc.)
- Use ClickOnce deployment
- Use MSIX (App Installer, Microsoft Store)

---

### Scenario 3: Self-Contained Deployment with Framework Dependencies

**Root Cause:** Even with PublishSingleFile disabled, deploying self-contained .NET MAUI apps with Windows App SDK can result in missing framework dependencies if the Windows App SDK Runtime isn't pre-installed on target machines.

**Environment:**
- Self-contained .NET MAUI app
- Target machines without Windows App SDK Runtime
- Windows App SDK 1.x

**Error:**
```
Application startup exception: Microsoft.WindowsAppSDK.Runtime.dll not found
```

**Solution: Include Windows App SDK Runtime**

1. **Use WindowsAppSDKSelfContained**
   ```xml
   <PropertyGroup Condition="$(TargetFramework.Contains('-windows'))">
     <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
     <PublishSingleFile>false</PublishSingleFile>
     <SelfContained>true</SelfContained>
   </PropertyGroup>
   ```

2. **Verify runtime files are included**
   ```powershell
   dotnet publish -c Release -f net8.0-windows10.0.19041.0 -r win10-x64 --self-contained
   
   # Check for runtime files
   Get-ChildItem "bin\Release\net8.0-windows10.0.19041.0\win10-x64\publish" -Recurse -Filter "Microsoft.WindowsAppSDK.*.dll"
   ```

3. **Test on clean machine**
   - Use Windows Sandbox or VM without .NET or Windows App SDK installed
   - Copy publish folder
   - Run YourApp.exe
   - Should work without any prerequisites

**Alternative: Require Windows App SDK Runtime Installation**

If self-contained is too large, require runtime:

1. **Create installer script**
   ```powershell
   # install-prerequisites.ps1
   $wasdkUrl = "https://aka.ms/windowsappsdk/1.8/latest/windowsappruntimeinstall-x64.exe"
   $installer = "$env:TEMP\windowsappruntimeinstall.exe"
   
   Write-Host "Downloading Windows App SDK Runtime..."
   Invoke-WebRequest -Uri $wasdkUrl -OutFile $installer
   
   Write-Host "Installing..."
   Start-Process -FilePath $installer -ArgumentList "/quiet" -Wait
   
   Remove-Item $installer
   Write-Host "Installation complete!"
   ```

2. **Include in deployment**
   ```
   YourApp\
     YourApp.exe
     [other files]
     install-prerequisites.ps1
     README.txt (instructions to run install-prerequisites.ps1 if app doesn't start)
   ```

---

## Additional Context

### .NET MAUI + Windows App SDK Compatibility

| .NET Version | MAUI Version | Windows App SDK | PublishSingleFile Support |
|--------------|--------------|-----------------|---------------------------|
| .NET 8 | MAUI 8.0 | 1.5+ | ❌ No (crashes) |
| .NET 9 | MAUI 9.0 | 1.6+ | ❌ No (crashes) |

### Impact

- **Deployment Complexity:** Can't use convenient single-file distribution on Windows
- **Size Concerns:** Windows deployments larger than other platforms
- **User Confusion:** Windows version behaves differently than Android/iOS/Mac
- **Cross-Platform Inconsistency:** Build configurations must differ per platform

### Root Cause Analysis

The fundamental issue is that WinRT/COM components used by Windows App SDK require:
1. **COM Registration:** Components must be discoverable via registry or side-by-side manifests
2. **Separate DLL Files:** WinRT activation expects to load DLLs from file paths
3. **Metadata Access:** WinRT.Host.dll needs to read WinMD files

Single-file bundling embeds everything into the .exe, breaking these assumptions.

### Future Outlook

- **Potential Fix:** Microsoft could update Windows App SDK to support embedded WinRT activation
- **Workaround Status:** Current workarounds are production-ready but not ideal
- **Recommendation:** Use MSIX packaging for Windows distribution

---

## Related Documentation

- [.NET MAUI Windows App SDK](https://learn.microsoft.com/dotnet/maui/windows/)
- [PublishSingleFile](https://learn.microsoft.com/dotnet/core/deploying/single-file/overview)
- [Windows App SDK Deployment](https://learn.microsoft.com/windows/apps/windows-app-sdk/deploy-overview)
- [MSIX Packaging](https://learn.microsoft.com/windows/msix/)

---

**Last Updated:** January 5, 2026  
**Confidence Score:** 0.95  
**Status:** Workarounds available; Fundamental fix requires Windows App SDK changes
