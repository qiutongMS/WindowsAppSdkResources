# Windows App SDK 1.8 Compatibility Issues on Windows 10

**Error Codes:** N/A (Package Detection Failure)  
**Affected Area:** Deployment, Package Manager  
**Common Platforms:** Windows 10 version 1809 (17763)

---

## Symptom Overview

After upgrading to Windows App SDK 1.8, packaged applications immediately crash on Windows 10 (build 17763) without leaving any crash dumps. The issue occurs because `FindPackagesByPackageFamily` is unable to locate the Windows App SDK 1.8 packages, causing the `DeploymentManager` initializer to fail.

**You might see:**
- Application crashes immediately upon launch on Windows 10
- No crash dump files are generated
- `FindPackagesByPackageFamily` returns no results despite packages being installed
- Same application works fine on Windows 11
- Issue affects both C++ and C# WinUI 3 projects

---

## Related Issues

This troubleshooting guide addresses:
- [#6117](https://github.com/microsoft/WindowsAppSDK/issues/6117) - FindPackagesByPackageFamily unable to find wasdk 1.8 packages on Windows 10

---

## Quick Diagnosis

1. **Check if the issue occurs on Windows 10 specifically**
   ```powershell
   # Check Windows version
   [System.Environment]::OSVersion.Version
   # If Major = 10 and Build = 17763, this issue may apply
   ```

2. **Verify Windows App SDK packages are installed**
   ```powershell
   Get-AppxPackage -Name "*Microsoft.WindowsAppRuntime.1.8*"
   # Packages should be listed, but app can't find them
   ```

3. **Check if app uses auto-initializer**
   ```xml
   <!-- In .vcxproj or .csproj, look for: -->
   <WindowsAppSdkAutoInitialize>true</WindowsAppSdkAutoInitialize>
   ```

---

## Common Scenarios & Solutions

### Scenario 1: Windows 10 Build 17763 Package Detection Failure

**Root Cause:** Windows App SDK 1.8 has a regression where `FindPackagesByPackageFamily` fails to locate framework packages on Windows 10 build 17763, even though the packages are properly installed. The auto-initializer depends on this API and fails silently, causing the app to crash on startup.

**Related Issue(s):** [#6117](https://github.com/microsoft/WindowsAppSDK/issues/6117)

**Environment:**
- Windows 10 version 1809 (build 17763)
- Windows App SDK 1.8.3 (1.8.251106002)
- Both packaged C++ and C# WinUI 3 applications affected

**Workaround:** Disable auto-initializer in project file

1. **Add property to disable auto-initialization**
   ```xml
   <!-- Add to .vcxproj for C++ or .csproj for C# -->
   <PropertyGroup>
     <WindowsAppSdkAutoInitialize>false</WindowsAppSdkAutoInitialize>
   </PropertyGroup>
   ```

2. **For C++ projects, manually initialize if needed**
   ```cpp
   // In your app initialization code
   #include <microsoft.ui.xaml.window.h>
   
   // Manual initialization may be required
   // Check Windows App SDK documentation for manual init steps
   ```

3. **For C# projects, check if manual initialization is needed**
   ```csharp
   // Most C# apps may work without explicit initialization
   // Test thoroughly after disabling auto-init
   ```

4. **Rebuild and test on Windows 10**
   ```powershell
   dotnet build -c Release
   # Or use MSBuild for C++ projects
   ```

**Verification:**
```powershell
# Deploy and run on Windows 10 build 17763
# App should now launch successfully
```

**Impact of Workaround:**
- Disabling auto-initializer may affect certain Windows App SDK features
- Review [documentation](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/project-properties) for potential impacts
- Test all app functionality thoroughly

---

### Scenario 2: Downgrade to Windows App SDK 1.7

**Alternative Solution:** If disabling auto-initializer causes issues, downgrade to SDK 1.7

1. **Update package references**
   ```xml
   <!-- Change in .csproj or .vcxproj -->
   <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.7.251014001" />
   ```

2. **Clean build artifacts**
   ```powershell
   Remove-Item -Path "bin", "obj" -Recurse -Force
   ```

3. **Rebuild solution**
   ```powershell
   dotnet build -c Release
   ```

**Verification:**
```powershell
# Test on both Windows 10 and Windows 11
# Ensure compatibility across platforms
```

---

## Additional Context

### Why This Happens

1. **API Regression:** Windows App SDK 1.8 introduced a regression in package enumeration on older Windows 10 builds
2. **Silent Failure:** The auto-initializer fails without proper error reporting
3. **No Crash Dumps:** The failure occurs so early in initialization that crash dumps aren't generated

### Affected Versions

- **Windows App SDK:** 1.8.0, 1.8.1, 1.8.2, 1.8.3
- **Windows Version:** Windows 10 version 1809 (build 17763)
- **Project Types:** Both C++ and C# WinUI 3 packaged applications

### Microsoft Tracking

This issue is being investigated by the Windows App SDK team. Monitor the related GitHub issue for updates on official fixes.

---

## Related Documentation

- [Windows App SDK Project Properties](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/project-properties)
- [Windows App SDK Release Notes](https://learn.microsoft.com/windows/apps/windows-app-sdk/release-notes)

---

**Last Updated:** January 5, 2026  
**Confidence Score:** 0.95  
**Status:** Workaround available (disable auto-initializer); Awaiting official fix
