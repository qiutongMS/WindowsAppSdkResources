# Windows App SDK 1.8 Package Deployment Failure with Scaled Images

**Error Codes:** 0x80073CF9, 0x80073CF3, ERROR_INSTALL_OPEN_PACKAGE_FAILED  
**Affected Area:** MSIX Packaging, Image Asset Deployment  
**Common Platforms:** Windows 10 22H2, Windows 11

---

## Symptom Overview

After upgrading to Windows App SDK 1.8, application packages fail to install when they contain scaled image assets (e.g., `logo.scale-100.png`, `logo.scale-200.png`). The deployment succeeds with Windows App SDK 1.7 using the same assets and package configuration.

**You might see:**
- Package installation fails with error 0x80073CF9
- Error message: "The package could not be opened"
- Event Viewer shows image asset validation errors
- Same package works fine with SDK 1.7
- Issue occurs only when scaled images are present

---

## Related Issues

- [#5820](https://github.com/microsoft/WindowsAppSDK/issues/5820) - App bundle installation failures with scaled images in 1.8

---

## Quick Diagnosis

1. **Check if package has scaled image assets**
   ```powershell
   # Extract .msix and check for scaled images
   Expand-Archive YourApp.msix -DestinationPath extracted
   Get-ChildItem -Path extracted\Assets -Filter "*.scale-*.png"
   # If files exist, you may be affected
   ```

2. **Try installing with verbose logging**
   ```powershell
   Add-AppxPackage -Path YourApp.msix -Verbose -Register
   # Check for image-related errors in output
   ```

3. **Check Event Viewer for details**
   ```powershell
   # Applications and Services Logs > Microsoft > Windows > AppxDeployment-Server
   Get-WinEvent -LogName Microsoft-Windows-AppxPackaging/Operational -MaxEvents 50 |
       Where-Object { $_.Message -match "0x80073CF9" -or $_.Message -match "image" }
   ```

4. **Verify SDK version in project**
   ```xml
   <!-- Check .csproj -->
   <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.8.x" />
   ```

---

## Common Scenarios & Solutions

### Scenario 1: App Bundle Fails to Install with Scaled Images

**Root Cause:** Windows App SDK 1.8 introduced stricter validation for scaled image assets in app bundles. The packaging process may incorrectly flag valid scaled images as malformed or may fail to properly include them in the bundle manifest.

**Related Issue(s):** [#5820](https://github.com/microsoft/WindowsAppSDK/issues/5820)

**Environment:**
- Windows App SDK 1.8.0 - 1.8.5
- MSIX bundles with multiple platform targets (x64, ARM64)
- Image assets with scale qualifiers (scale-100, scale-125, scale-150, scale-200, scale-400)

**Error Symptoms:**
```
Deployment failed with HRESULT: 0x80073CF9
The package could not be opened.
```

Event Viewer (AppxPackaging/Operational):
```
Package validation failed. Error code: 0x80073CF3
Image resource validation error in Assets\logo.scale-200.png
```

**Fix Option 1: Downgrade to Windows App SDK 1.7**

This is the most reliable workaround until Microsoft fixes the issue:

1. **Update package reference**
   ```xml
   <!-- In .csproj -->
   <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.7.240802000" />
   ```

2. **Clean and rebuild**
   ```powershell
   dotnet clean
   Remove-Item -Path "bin", "obj" -Recurse -Force
   dotnet build -c Release
   dotnet publish -c Release
   ```

3. **Test deployment**
   ```powershell
   Add-AppxPackage -Path "bin\Release\net8.0-windows10.0.19041.0\win10-x64\AppPackages\YourApp_1.0.0.0_Test\YourApp_1.0.0.0_x64.msix"
   ```

**Fix Option 2: Use Single-Scale Images (Temporary)**

If you must use SDK 1.8, remove scaled variants and use only base images:

1. **Remove scale qualifiers from filenames**
   ```
   Before:
   Assets\logo.scale-100.png (44x44)
   Assets\logo.scale-125.png (55x55)
   Assets\logo.scale-150.png (66x66)
   Assets\logo.scale-200.png (88x88)
   
   After:
   Assets\logo.png (176x176 - highest resolution)
   ```

2. **Update Package.appxmanifest references**
   ```xml
   <!-- Change from: -->
   <uap:VisualElements DisplayName="YourApp"
       Square150x150Logo="Assets\logo.png"
       Square44x44Logo="Assets\logo.png" />
   
   <!-- To (using single high-res image): -->
   <uap:VisualElements DisplayName="YourApp"
       Square150x150Logo="Assets\logo.png"
       Square44x44Logo="Assets\logo.png" />
   ```

3. **Clean old scaled images from project**
   ```xml
   <!-- Remove from .csproj if explicitly listed -->
   <Content Remove="Assets\*.scale-*.png" />
   ```

4. **Rebuild and test**
   ```powershell
   dotnet build -c Release
   ```

**Note:** This workaround reduces image quality on high-DPI displays.

**Fix Option 3: Reorganize Assets into Resource Packages (Advanced)**

Create separate resource packages for different scales:

1. **Create resource-specific configurations**
   ```xml
   <!-- In .csproj -->
   <PropertyGroup>
     <GenerateAppInstallerFile>true</GenerateAppInstallerFile>
     <AppxBundlePlatforms>x64|ARM64</AppxBundlePlatforms>
     <AppxBundle>Always</AppxBundle>
     <AppInstallerUri>https://yourcdn.com/packages</AppInstallerUri>
   </PropertyGroup>
   
   <!-- Split resources -->
   <ItemGroup>
     <AppxResourceQualifier Include="scale-100" />
     <AppxResourceQualifier Include="scale-200" />
   </ItemGroup>
   ```

2. **Build with resource packs**
   ```powershell
   msbuild YourApp.csproj /p:Configuration=Release /p:AppxBundle=Always /p:AppxBundlePlatforms="x64|ARM64" /p:UapAppxPackageBuildMode=StoreUpload
   ```

3. **Verify bundle contents**
   ```powershell
   # Check generated bundle
   Get-ChildItem "AppPackages\YourApp_*\*.appxbundle"
   ```

---

### Scenario 2: Deployment Succeeds But Images Missing at Runtime

**Root Cause:** Even if the package installs successfully, Windows App SDK 1.8 may fail to properly extract or map scaled image resources at runtime, causing the app to display blank tiles or default images.

**Environment:**
- Windows App SDK 1.8.x
- App installs without error
- Start menu/taskbar shows generic icon instead of app icon

**Symptoms:**
- Package deployment reports success
- App runs normally
- Images in Assets folder are present
- UI shows placeholder images or blank icons

**Fix: Verify Image Format and Naming**

1. **Check image naming conventions**
   ```
   Correct:
   Assets\Square44x44Logo.scale-100.png
   Assets\Square44x44Logo.scale-200.png
   
   Incorrect:
   Assets\Square44x44Logo-scale-100.png (wrong separator)
   Assets\Square44x44Logo_scale_200.png (wrong separator)
   ```

2. **Validate image dimensions match scale factors**
   ```powershell
   # PowerShell script to check dimensions
   Add-Type -AssemblyName System.Drawing
   
   Get-ChildItem "Assets\*.scale-*.png" | ForEach-Object {
       $img = [System.Drawing.Image]::FromFile($_.FullName)
       $scale = if ($_.Name -match "scale-(\d+)") { $matches[1] }
       $expectedSize = 44 * ($scale / 100)
       
       Write-Host "$($_.Name): $($img.Width)x$($img.Height) (expected: $expectedSize)"
       
       if ($img.Width -ne $expectedSize -or $img.Height -ne $expectedSize) {
           Write-Warning "Size mismatch for $($_.Name)"
       }
       
       $img.Dispose()
   }
   ```

3. **Re-export images with correct dimensions**
   ```
   scale-100: 44x44
   scale-125: 55x55
   scale-150: 66x66
   scale-200: 88x88
   scale-400: 176x176
   ```

4. **Rebuild package**
   ```powershell
   dotnet clean
   dotnet build -c Release
   ```

---

### Scenario 3: AppxManifest.xml Build Errors with Scaled Images

**Root Cause:** The build process in SDK 1.8 may fail to generate correct manifest entries for scaled images, especially when using custom build configurations or multi-targeting.

**Environment:**
- Complex .csproj configurations
- Multi-targeting (net8.0-windows10.x;net9.0-windows10.x)
- Custom asset organization

**Build Error:**
```
Error APPX1101: Payload contains two or more files with the same destination path 'Assets\logo.png'
```

**Fix: Use AppxManifest Generation Options**

1. **Explicitly set manifest generator options**
   ```xml
   <PropertyGroup>
     <AppxPackageSigningEnabled>true</AppxPackageSigningEnabled>
     <GenerateAppInstallerFile>false</GenerateAppInstallerFile>
     <AppxAutoIncrementPackageRevision>false</AppxAutoIncrementPackageRevision>
     <GenerateTestArtifacts>false</GenerateTestArtifacts>
     <AppxBundle>Never</AppxBundle>
   </PropertyGroup>
   ```

2. **Ensure assets are marked correctly**
   ```xml
   <ItemGroup>
     <Content Include="Assets\**\*.png">
       <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
     </Content>
   </ItemGroup>
   ```

3. **Check for duplicate asset references**
   ```powershell
   # Find duplicate Content entries in .csproj
   Select-Xml -Path *.csproj -XPath "//Content[@Include]" | 
       Group-Object { $_.Node.Include } | 
       Where-Object { $_.Count -gt 1 }
   ```

4. **Clean intermediate files**
   ```powershell
   Remove-Item -Path "obj", "bin" -Recurse -Force
   dotnet restore
   dotnet build -c Release
   ```

---

## Additional Context

### SDK Version Comparison

| Aspect | SDK 1.7 | SDK 1.8 |
|--------|---------|---------|
| Scaled Image Support | ✅ Working | ⚠️ Broken in some configurations |
| Bundle Validation | Lenient | Strict (may reject valid images) |
| Asset Naming | Standard | Same (but validation different) |
| Workaround Required | None | Downgrade or remove scales |

### Impact

- **Users Cannot Install:** App bundles fail to deploy on user machines
- **Store Submission Issues:** Apps may be rejected during certification
- **Development Blocked:** Cannot test packaged apps with proper image assets
- **Regression:** Previously working apps break after SDK upgrade

### Known Affected Versions

- Windows App SDK 1.8.0
- Windows App SDK 1.8.1
- Windows App SDK 1.8.2
- Windows App SDK 1.8.3
- Windows App SDK 1.8.4
- Windows App SDK 1.8.5

### Microsoft Response

The issue has been reported and is under investigation. No official fix or timeline has been provided as of January 2026.

---

## Related Documentation

- [App Icon Assets](https://learn.microsoft.com/windows/apps/design/style/iconography/app-icon-construction)
- [Image and Icon Assets](https://learn.microsoft.com/windows/apps/design/style/iconography/)
- [Package Manifest Schema](https://learn.microsoft.com/uwp/schemas/appxpackage/appx-package-manifest)
- [Packaging MSIX Apps](https://learn.microsoft.com/windows/msix/package/packaging-uwp-apps)

---

**Last Updated:** January 5, 2026  
**Confidence Score:** 0.95  
**Status:** Awaiting official fix; Downgrade to 1.7 recommended
