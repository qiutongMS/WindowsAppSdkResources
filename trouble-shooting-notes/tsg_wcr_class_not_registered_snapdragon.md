---
id: wcr_class_not_registered_snapdragon
title: "REGDB_E_CLASSNOTREG error on Snapdragon X Elite processors"
area: Runtime
symptoms:
  - "COMException: Class not registered (0x80040154)"
  - "WCR APIs fail with REGDB_E_CLASSNOTREG on Snapdragon devices"
  - "ImageDescriptionGenerator or TextRecognizer throws registration error"
errorCodes:
  - "0x80040154"
  - "REGDB_E_CLASSNOTREG"
keywords:
  - "Class not registered"
  - "REGDB_E_CLASSNOTREG"
  - "0x80040154"
  - "Snapdragon"
  - "X Elite"
  - "ARM64"
  - "COM"
  - "registration"
appliesTo:
  windows: ">=10.0.26120"
  winappsdk: ">=1.7.0-experimental"
projectType: "packaged"
severity: "common"
lastVerified: "2025-12-23"
references:
  - "https://github.com/microsoft/WindowsAppSDK/issues/5244"
---

# Problem

When attempting to use Windows Copilot Runtime APIs (such as `ImageDescriptionGenerator`, `TextRecognizer`, `LanguageModel`) on Snapdragon X Elite ARM64 devices, calls to `CreateAsync()` or similar initialization methods throw a `COMException` with error code `0x80040154` (REGDB_E_CLASSNOTREG) indicating the COM class is not registered.

**Example exception:**
```
System.Runtime.InteropServices.COMException (0x80040154): Class not registered (0x80040154 (REGDB_E_CLASSNOTREG))
   at Windows.AI.Copilot.Runtime.ImageDescriptionGenerator.CreateAsync()
```

# Quick Diagnosis

1. Verify you're running on Snapdragon X Elite or Snapdragon X Plus processor
2. Check Windows Insider build version (should be >= 26120)
3. Confirm error code is `0x80040154` (REGDB_E_CLASSNOTREG)
4. Verify the specific WCR API being called (ImageDescriptionGenerator, TextRecognizer, etc.)
5. Check if systemAIModels capability is declared in manifest
6. Verify MSIX package identity is properly configured

# Root Cause

The "Class not registered" error on Snapdragon X Elite/Plus devices indicates that the Windows Runtime classes for WCR APIs are not properly registered in the Windows registry or are missing from the ARM64 system.

Possible causes:

1. **Missing Windows AI Components:** The required Windows AI features (FOD - Features on Demand) for ARM64 are not installed or not fully deployed
2. **Incomplete Windows Update:** The Windows Insider build may not include all ARM64 WCR components
3. **Limited Access Feature not unlocked:** The LAF token for WCR is not properly unlocked on the device
4. **Wrong MSIX architecture:** Application is built for x64 but running on ARM64 (should be ARM64 or Any CPU)
5. **Missing manifest capabilities:** systemAIModels capability not declared
6. **Regional/Language restrictions:** Some WCR features may not be available in all regions on ARM64

# Fix / Workaround

**Fix 1: Verify and add systemAIModels capability**

Ensure your Package.appxmanifest includes the systemAIModels capability:

```xml
<Package
  xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
  xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
  xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
  IgnorableNamespaces="uap rescap">
  
  <Capabilities>
    <rescap:Capability Name="systemAIModels" />
  </Capabilities>
</Package>
```

**Fix 2: Ensure ARM64 or Any CPU build**

Verify your project is targeting ARM64 or Any CPU (AnyCPU):

```xml
<PropertyGroup>
  <Platform>ARM64</Platform>
  <!-- OR -->
  <PlatformTarget>AnyCPU</PlatformTarget>
</PropertyGroup>
```

If your application is built as x64-only, rebuild for ARM64:

1. Open Configuration Manager in Visual Studio
2. Add ARM64 platform configuration
3. Rebuild solution for ARM64
4. Deploy ARM64 MSIX package to Snapdragon device

**Fix 3: Install Windows AI components manually**

Check and install Windows AI Features on Demand:

```powershell
# Check current optional features
Get-WindowsOptionalFeature -Online | Where-Object {$_.FeatureName -like "*AI*"}

# If available, enable Windows AI features
Enable-WindowsOptionalFeature -Online -FeatureName "Windows.AI.Runtime" -NoRestart

# Check for Windows Updates
Get-WindowsUpdate
Install-WindowsUpdate -AcceptAll
```

**Fix 4: Verify Limited Access Feature unlock**

Check LAF status and ensure it's unlocked:

```csharp
using Microsoft.Windows.AI.Phi;
using Microsoft.Windows.AI.Generative;

// Check Phi Silica status
var phiStatus = LanguageModel.GetReadyState();
Debug.WriteLine($"Phi Silica LAF Status: {phiStatus.Status}");
Debug.WriteLine($"Extended Error: {phiStatus.ExtendedError?.Message}");

// Status should be Success (0), not NotAvailable (3) or NeedsAttestation (2)
```

If status is NotAvailable:
1. Verify device is Snapdragon X Elite/Plus (Qualcomm Oryon CPU)
2. Check Windows version is Insider Canary/Dev >= 26120
3. Ensure region/language settings are compatible (US English recommended)
4. Contact Microsoft for LAF token if needed

**Fix 5: Update Windows Insider build**

Ensure you're on latest Canary or Dev channel build:

1. Open Settings > Windows Update > Windows Insider Program
2. Select **Canary Channel** or **Dev Channel**
3. Check for updates
4. Install all available updates (may include ARM64 WCR components)
5. Restart system
6. Retry application

**Fix 6: Re-register Windows Runtime components**

If components exist but aren't registered, try re-registration:

```powershell
# Run PowerShell as Administrator
# Re-register Windows.AI.* WinRT components
Get-AppxPackage -AllUsers | Where-Object {$_.Name -like "*AI*"} | ForEach-Object {
    Add-AppxPackage -DisableDevelopmentMode -Register "$($_.InstallLocation)\AppXManifest.xml"
}
```

**Fix 7: Check regional availability**

Some WCR features may be region-locked on ARM64:

1. Open Settings > Time & Language > Language & Region
2. Set **Country or region** to **United States**
3. Set **Windows display language** to **English (United States)**
4. Restart system
5. Retry application

# Verification

After applying fixes:

1. Rebuild application for ARM64 (if applicable)
2. Redeploy MSIX package to Snapdragon device
3. Launch application
4. Call WCR API CreateAsync():

```csharp
try 
{
    var generator = await ImageDescriptionGenerator.CreateAsync();
    Console.WriteLine("ImageDescriptionGenerator created successfully!");
}
catch (COMException ex) when (ex.HResult == unchecked((int)0x80040154))
{
    Console.WriteLine($"Still failing: {ex.Message}");
    // Try other fixes
}
```

5. Verify no REGDB_E_CLASSNOTREG exception
6. Test actual functionality (e.g., describe an image, recognize text)

# Deep Dive

## Affected Devices

Reported on:
- **Snapdragon X Elite** (Qualcomm Oryon CPU)
- **Snapdragon X Plus** (potentially, not explicitly confirmed)

NOT reported on:
- ✓ AMD Ryzen AI 300 series (works)
- ✓ Intel Core Ultra Series 2 (works, but has other issues)

## Windows Insider Requirements

For Snapdragon X Elite/Plus, WCR requires:
- Windows 11 Insider Preview Canary/Dev Channel
- Build >= 26120
- All latest updates installed
- Optional: Switch to US region/language

## COM Registration Background

The error `REGDB_E_CLASSNOTREG (0x80040154)` means:
- The COM class CLSID is not found in the Windows Registry
- The associated DLL/WinMD is not registered or missing
- For WinRT, this often indicates missing Windows components

WCR APIs are Windows Runtime (WinRT) projections of COM objects. The actual implementation is in Windows system DLLs (e.g., Windows.AI.dll).

## ARM64 vs x64/x86

Important architectural considerations:
- **x64/x86 apps cannot use ARM64 WinRT components**
- Must build as ARM64 or Any CPU with ARM64 support
- MSIX package must include ARM64 architecture

## Limited Access Feature Context

WCR APIs on Snapdragon require:
1. **Device support:** Only Snapdragon X Elite/Plus with NPU
2. **Windows version:** Insider builds >= 26120
3. **LAF unlock:** Limited Access Feature token granted by Microsoft
4. **Capability declaration:** systemAIModels in manifest

Missing any of these = REGDB_E_CLASSNOTREG or similar errors.

## Related Errors

Similar errors on Snapdragon:
- `0x80004005` (E_FAIL): General failure, often LAF-related
- `0x8007000E` (E_OUTOFMEMORY): NPU memory issues
- `Status = 3` (NotAvailable): LAF not available on device

## Microsoft Response

Status: No official response yet. Issue may be addressed in future Windows Insider builds with better ARM64 WCR support.

## Debugging Steps

To investigate further:

```powershell
# Check if Windows.AI DLLs exist on ARM64
Get-ChildItem -Path C:\Windows\System32 -Filter "*AI*.dll" -Recurse -ErrorAction SilentlyContinue

# Check registered WinRT classes
Get-ChildItem -Path "HKLM:\SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\PackageRepository\Packages" | Where-Object {$_.Name -like "*AI*"}

# Check NPU driver status
Get-PnpDevice | Where-Object {$_.FriendlyName -like "*Qualcomm*" -or $_.FriendlyName -like "*NPU*"}
```

# References

- [GitHub Issue #5244](https://github.com/microsoft/WindowsAppSDK/issues/5244)
- [Snapdragon X Elite WCR Requirements](https://learn.microsoft.com/windows/ai/overview#platform-requirements)
- [Limited Access Features Documentation](https://learn.microsoft.com/windows/apps/windows-app-sdk/limited-access-features)
- [Windows Runtime Class Registration](https://learn.microsoft.com/uwp/api/windows.applicationmodel.package.current)
