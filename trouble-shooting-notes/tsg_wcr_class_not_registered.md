# Class Not Registered (0x80040154) with WCR APIs

**Error Code:** `0x80040154 (REGDB_E_CLASSNOTREG)`  
**Affected Area:** WCR APIs - Runtime activation  
**Common Platforms:** x86 architecture, misconfigured Windows builds, Snapdragon devices

---

## Symptom Overview

When calling WCR API methods like `LanguageModel.IsAvailable()`, `ImageDescriptionGenerator.GetReadyState()`, or other WCR initialization APIs, you receive a "Class not registered" COM exception with HRESULT 0x80040154.

**You might see:**
```
System.Runtime.InteropServices.COMException
  HResult=0x80040154
  Message=Class not registered
  Source=WinRT.Runtime
```

This error indicates the WCR runtime components cannot be activated on your system.

---

## Related Issues

This troubleshooting guide consolidates multiple related reports:
- [#5613](https://github.com/microsoft/WindowsAppSDK/issues/5613) - REGDB_E_CLASSNOTREG when running x86 app on x64 OS
- [#5123](https://github.com/microsoft/WindowsAppSDK/issues/5123) - Class not registered calling Phi Silica
- [#5244](https://github.com/microsoft/WindowsAppSDK/issues/5244) - Class not registered on Snapdragon X Elite

---

## Quick Diagnosis

Run through these checks to identify your specific scenario:

1. **Check your project's target architecture**
   ```xml
   <!-- In .csproj file -->
   <PropertyGroup>
     <Platforms>x86;x64;ARM64</Platforms>
   </PropertyGroup>
   ```
   ```powershell
   # Or check current build configuration
   dotnet build --configuration Release /p:Platform=x64
   ```
   → If building for **x86**, see [Scenario 1: x86 Architecture Not Supported](#scenario-1-x86-architecture-not-supported)

2. **Check Windows version and build number**
   ```powershell
   [System.Environment]::OSVersion.Version
   # or
   winver
   ```
   → If not on Insider build (26xxx+) or missing Windows AI Component updates, see [Scenario 2: Missing Windows Components](#scenario-2-missing-windows-components-or-build)

3. **Check Windows Feature Experience Pack**
   ```powershell
   Get-AppxPackage -Name "*AIToolkit*"
   Get-AppxPackage -Name "*Phi*"
   ```
   → If no AI packages found, see [Scenario 2](#scenario-2-missing-windows-components-or-build)

---

## Common Scenarios & Solutions

### Scenario 1: x86 Architecture Not Supported

**Root Cause:** WCR APIs (Phi Silica, OCR, Image Description) are **NOT supported on x86 architecture**. The AI models and runtime components are only available for x64 and ARM64 platforms. This is by design due to model size and NPU requirements.

**Related Issue(s):** [#5613](https://github.com/microsoft/WindowsAppSDK/issues/5613)

**Fix:** Change your build target to x64 or ARM64

1. **Update .csproj to remove x86:**
   ```xml
   <PropertyGroup>
     <!-- Remove x86 from platforms -->
     <Platforms>x64;ARM64</Platforms>
     <RuntimeIdentifiers>win-x64;win-arm64</RuntimeIdentifiers>
   </PropertyGroup>
   ```

2. **If you need x86 for other scenarios, use conditional compilation:**
   ```csharp
   #if !X86
       // WCR API calls only for x64/ARM64
       if (LanguageModel.IsAvailable())
       {
           // Use WCR features
       }
   #else
       // Fallback for x86
       throw new PlatformNotSupportedException("WCR APIs require x64 or ARM64");
   #endif
   ```

3. **Rebuild for x64:**
   ```powershell
   dotnet build -c Release /p:Platform=x64
   ```

**Verification:**
```powershell
# Check the built binary architecture
dumpbin /headers "bin\Release\net8.0-windows10.0.22621.0\win-x64\YourApp.exe" | findstr "machine"
# Should show: 8664 machine (x64)
```

---

### Scenario 2: Missing Windows Components or Build

**Root Cause:** WCR APIs require:
- Windows 11 Insider build (currently 26xxx series for Dev Channel)
- Windows AI Component updates (delivered via Windows Update)
- Copilot+ PC hardware (for full functionality) or specific NPU support

**Related Issue(s):** [#5123](https://github.com/microsoft/WindowsAppSDK/issues/5123), [#5244](https://github.com/microsoft/WindowsAppSDK/issues/5244)

**Fix:** Update Windows and verify AI components

1. **Check Windows version requirements:**
   ```powershell
   # Minimum for WCR APIs (as of Dec 2025)
   # Windows 11 version 24H2 (Build 26100.xxxx+)
   # Insider builds recommended: 26xxx series
   
   [System.Environment]::OSVersion.Version
   ```

2. **Join Windows Insider Program if needed:**
   - Settings → Windows Update → Windows Insider Program
   - Choose Dev Channel or Beta Channel
   - Restart and update

3. **Install Windows AI Component updates:**
   ```powershell
   # Check for updates
   Start-Process "ms-settings:windowsupdate"
   
   # Or use PowerShell
   Install-Module PSWindowsUpdate
   Get-WindowsUpdate -Install -AcceptAll
   ```

4. **Verify AI packages are installed:**
   ```powershell
   Get-AppxPackage | Where-Object {$_.Name -like "*AI*" -or $_.Name -like "*Phi*"}
   ```
   
   You should see packages like:
   - Microsoft.Windows.AIFoundation
   - MicrosoftWindows.Client.AI (or similar)

**Verification:**
```csharp
// This should now return true or at least not throw REGDB_E_CLASSNOTREG
bool available = LanguageModel.IsAvailable();
Console.WriteLine($"Language Model Available: {available}");
```

---

### Scenario 3: Snapdragon Platform Configuration

**Root Cause:** On Snapdragon X Elite/Plus devices, WCR APIs may fail if:
- Windows AI drivers are not properly installed
- NPU firmware needs update
- Qualcomm drivers are outdated

**Related Issue(s):** [#5244](https://github.com/microsoft/WindowsAppSDK/issues/5244)

**Fix:** Update Qualcomm drivers and verify NPU

1. **Update Qualcomm drivers:**
   ```powershell
   # Open Device Manager
   devmgmt.msc
   
   # Look for "Neural processing controllers" or "Qualcomm(R) Hexagon(TM) NPU"
   # Right-click → Update driver
   ```

2. **Check for OEM updates:**
   - Use manufacturer's update utility (e.g., Lenovo Vantage, HP Support Assistant)
   - Or download latest drivers from OEM website

3. **Verify NPU is recognized:**
   ```powershell
   Get-PnpDevice | Where-Object {$_.FriendlyName -like "*NPU*" -or $_.FriendlyName -like "*Neural*"}
   ```

4. **Ensure manifest has correct capabilities** (from access denied TSG):
   ```xml
   <systemai:Capability Name="systemAIModels"/>
   ```

**Verification:**
```powershell
# Check NPU is active in Task Manager
# Performance tab → NPU should show activity when running AI workloads
```

---

## Additional Context

### Supported Architectures

| Architecture | WCR Support | Notes |
|--------------|-------------|-------|
| x64 | ✅ Yes | Fully supported on compatible hardware |
| ARM64 | ✅ Yes | Native support on Snapdragon X series |
| x86 | ❌ No | Not supported - by design |

### Hardware Requirements

While the SDK can be installed on various devices:
- **Full functionality**: Requires Copilot+ PC (NPU with 40+ TOPS)
- **Limited functionality**: May work on devices with compatible NPU
- **No functionality**: Devices without NPU or on unsupported architectures

### Error Code Reference

- **0x80040154**: Class not registered (WinRT component not available)
- Common causes: Wrong architecture, missing Windows components, outdated drivers

---

## References

- [Issue #5613: WCR x86 architecture not supported](https://github.com/microsoft/WindowsAppSDK/issues/5613)
- [Issue #5123: Class not registered with Phi Silica](https://github.com/microsoft/WindowsAppSDK/issues/5123)
- [Issue #5244: Class not registered on Snapdragon](https://github.com/microsoft/WindowsAppSDK/issues/5244)
- [Windows Insider Program](https://www.microsoft.com/en-us/windowsinsider/)
- [Copilot+ PC Requirements](https://www.microsoft.com/en-us/windows/copilot-plus-pcs)

---

**Last Updated:** 2025-12-25  
**Confidence:** 0.88
