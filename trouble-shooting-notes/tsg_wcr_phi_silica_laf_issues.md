# Phi Silica Model and LAF (Limited Access Feature) Issues

**Error Codes:** Various, including "Element not found", Status 3, LAF errors  
**Affected Area:** WCR APIs - Phi Silica language model  
**Common Platforms:** All platforms, Windows build-dependent

---

## Symptom Overview

Issues specific to accessing the Phi Silica language model through WCR APIs. These range from model not being declared/registered properly, to Limited Access Features not being available on the Windows build.

**You might see:**
- Error: "Unspecified error - Not declared by app"
- LAF error: "Limited Access Feature is not available: com.microsoft.windows.ai.languagemodel. Status: 3"
- COMException: "Element not found"
- Model APIs fail even after proper manifest configuration

---

## Related Issues

This troubleshooting guide consolidates multiple related reports:
- [#6019](https://github.com/microsoft/WindowsAppSDK/issues/6019) - "Not declared by app" error with Phi Silica GetReadyState
- [#5892](https://github.com/microsoft/WindowsAppSDK/issues/5892) - LAF com.microsoft.windows.ai.languagemodel missing from Windows
- [#5171](https://github.com/microsoft/WindowsAppSDK/issues/5171) - COMException "Element not found" with LanguageModel

---

## Quick Diagnosis

Run through these checks to identify your specific scenario:

1. **Check for systemAIModels capability**
   ```xml
   <!-- In Package.appxmanifest -->
   <systemai:Capability Name="systemAIModels"/>
   ```
   → If MISSING, see [Scenario 1: Missing systemAIModels Capability](#scenario-1-missing-systemaimodels-capability)

2. **Check Limited Access Feature registration**
   ```csharp
   var featureId = "com.microsoft.windows.ai.languagemodel";
   var result = LimitedAccessFeatures.TryUnlockFeature(
       featureId, 
       token, 
       attestation);
   
   Console.WriteLine($"LAF Status: {result.Status}");
   ```
   → If Status is **3 (Unavailable)**, see [Scenario 2: LAF Not Available on Windows Build](#scenario-2-laf-not-available-on-windows-build)

3. **Check Windows Insider build version**
   ```powershell
   winver
   # or
   [System.Environment]::OSVersion.Version
   ```
   → If not on **26xxx+ Dev Channel**, see [Scenario 2](#scenario-2-laf-not-available-on-windows-build)

---

## Common Scenarios & Solutions

### Scenario 1: Missing systemAIModels Capability

**Root Cause:** Even with LAF token registration, the `systemAIModels` capability must be declared in the app manifest. Without it, you'll get "Not declared by app" errors.

**Related Issue(s):** [#6019](https://github.com/microsoft/WindowsAppSDK/issues/6019)

**Fix:** Add both LAF unlock and manifest capability

1. **Add manifest capability** (see Access Denied TSG for full details):
   ```xml
   <Package
     xmlns:systemai="http://schemas.microsoft.com/appx/manifest/systemai/windows10"
     IgnorableNamespaces="... systemai">
     
     <Capabilities>
       <systemai:Capability Name="systemAIModels"/>
     </Capabilities>
   </Package>
   ```

2. **Ensure LAF unlock code is correct:**
   ```csharp
   // This should come BEFORE any LanguageModel API calls
   var demoToken = "YOUR_PUBLISHER_ID";
   var demoPublisherId = "YOUR_PUBLISHER_ID";
   var featureId = "com.microsoft.windows.ai.languagemodel";
   
   var limitedAccessFeatureResult = LimitedAccessFeatures.TryUnlockFeature(
       featureId,
       demoToken,
       $"{demoPublisherId} has registered their use of {featureId} with Microsoft and agrees to the terms of use.");
   
   if (limitedAccessFeatureResult.Status != LimitedAccessFeatureStatus.Available && 
       limitedAccessFeatureResult.Status != LimitedAccessFeatureStatus.AvailableWithoutToken)
   {
       throw new InvalidOperationException(
           $"Phi-Silica is not available: {limitedAccessFeatureResult.Status}");
   }
   ```

3. **Verify call order:**
   ```csharp
   // ✅ CORRECT ORDER:
   // 1. Unlock LAF
   LimitedAccessFeatures.TryUnlockFeature(...);
   
   // 2. Check ready state
   var readyState = LanguageModel.GetReadyState();
   
   // 3. Create model
   var model = await LanguageModel.CreateAsync();
   ```

**Verification:**
```csharp
var readyState = LanguageModel.GetReadyState();
// Should NOT throw "Not declared by app" exception now
Console.WriteLine($"Ready state: {readyState}");
```

---

### Scenario 2: LAF Not Available on Windows Build

**Root Cause:** The `com.microsoft.windows.ai.languagemodel` Limited Access Feature is only present in specific Windows builds. It's missing from:
- Windows 11 stable releases (22H2, 23H2, 24H2 non-Insider)
- Early Insider builds
- Builds without Phi Silica model package installed

This is **not a bug in your app**, but a Windows platform availability issue.

**Related Issue(s):** [#5892](https://github.com/microsoft/WindowsAppSDK/issues/5892)

**Fix:** Update to compatible Windows build

1. **Check current Windows version:**
   ```powershell
   # Minimum requirement as of Dec 2025:
   # Windows 11 Insider Preview Build 26xxx+ (Dev Channel)
   # or Build 26100.xxxx+ (24H2 with AI Component updates)
   
   winver
   ```

2. **Join Windows Insider Program:**
   - Settings → Windows Update → Windows Insider Program
   - Select **Dev Channel** (most up-to-date AI features)
   - Or **Beta Channel** (more stable, may lag on features)
   - Restart and check for updates

3. **Install Windows AI components:**
   ```powershell
   # After joining Insider program and updating
   # Check for specific AI component packages
   Get-AppxPackage | Where-Object {$_.Name -like "*AI*" -or $_.Name -like "*Phi*"} | 
       Select-Object Name, Version
   ```

4. **Verify LAF availability after update:**
   ```csharp
   var testResult = LimitedAccessFeatures.TryUnlockFeature(
       "com.microsoft.windows.ai.languagemodel",
       "",
       "Test");
   
   Console.WriteLine($"LAF Status: {testResult.Status}");
   // Status should be Available or AvailableWithoutToken
   // NOT 3 (Unavailable)
   ```

**Verification:**
```powershell
# After Windows update, verify Windows version
[System.Environment]::OSVersion.Version
# Should show Major: 10, Build: 26xxx or higher

# Check Phi Silica model is installed
Get-AppxPackage -AllUsers | Where-Object {$_.Name -like "*Phi*"}
```

---

### Scenario 3: COMException "Element Not Found"

**Root Cause:** This error typically occurs during `MakeAvailableAsync()` or model initialization when Windows can't locate required model files or registry entries. Often related to incomplete Windows AI component installation or corrupted package cache.

**Related Issue(s):** [#5171](https://github.com/microsoft/WindowsAppSDK/issues/5171)

**Fix:** Reinstall Windows AI components

1. **Clear Windows package cache:**
   ```powershell
   # Run as Administrator
   Stop-Service -Name "InstallService", "wuauserv"
   
   # Clear cache
   Remove-Item "C:\ProgramData\Microsoft\Windows\AppRepository\*" -Force -ErrorAction SilentlyContinue
   
   # Restart services
   Start-Service -Name "InstallService", "wuauserv"
   ```

2. **Reinstall Windows AI packages:**
   ```powershell
   # Check current AI packages
   Get-AppxPackage -AllUsers | Where-Object {$_.Name -like "*AI*"}
   
   # If found, try re-registering
   Get-AppxPackage -AllUsers | Where-Object {$_.Name -like "*AI*"} | 
       ForEach-Object {Add-AppxPackage -DisableDevelopmentMode -Register "$($_.InstallLocation)\AppXManifest.xml"}
   ```

3. **Force Windows Update check:**
   ```powershell
   # Install PSWindowsUpdate module if not present
   Install-Module PSWindowsUpdate -Force
   
   # Check for updates
   Get-WindowsUpdate
   Install-WindowsUpdate -AcceptAll -AutoReboot
   ```

4. **If issues persist - reset Windows AI stack:**
   ```powershell
   # Remove all AI-related packages (CAUTION)
   Get-AppxPackage -AllUsers | Where-Object {$_.Name -like "*AI*" -or $_.Name -like "*Phi*"} | 
       Remove-AppxPackage -AllUsers
   
   # Reboot
   Restart-Computer
   
   # After reboot, check for Windows Updates to reinstall
   ```

**Verification:**
```csharp
try
{
    var operation = LanguageModel.MakeAvailableAsync();
    var result = await operation;
    
    Console.WriteLine($"Installation status: {result.Status}");
    // Should be CompletedSuccess, not throw "Element not found"
}
catch (COMException ex)
{
    Console.WriteLine($"Error: 0x{ex.HResult:X8} - {ex.Message}");
}
```

---

## Additional Context

### Limited Access Features (LAF) Overview

LAF is Microsoft's mechanism to control access to sensitive APIs:
- Requires publisher registration for production use
- Development/testing may use demo tokens
- Availability depends on Windows build and user region

### Phi Silica Availability Timeline

| Date | Event |
|------|-------|
| June 2024 | Initial Phi Silica preview in limited Insider builds |
| Nov 2024 | Announced at Ignite for WinAppSDK 1.7 |
| Jan 2025 | Released in 1.7-exp3 |
| Ongoing | Requires Windows Insider Dev Channel builds |

### Known Limitations

- **Regional restrictions**: Some regions may not have LAF available
- **Hardware requirements**: Copilot+ PC recommended (works on some other NPU devices)
- **Windows version**: Must be on Insider builds for foreseeable future
- **Production use**: Requires formal LAF registration with Microsoft

### When to Skip Phi Silica

If Phi Silica is unavailable, consider alternatives:
```csharp
if (!await IsPhiSilicaAvailable())
{
    // Fallback options:
    // 1. Azure OpenAI Service
    // 2. Other cloud LLM APIs
    // 3. ONNX Runtime with local Phi-3 model
    // 4. Graceful degradation (disable AI features)
}
```

---

## References

- [Issue #6019: "Not declared by app" with Phi Silica](https://github.com/microsoft/WindowsAppSDK/issues/6019)
- [Issue #5892: LAF missing from Windows](https://github.com/microsoft/WindowsAppSDK/issues/5892)
- [Issue #5171: Element not found COMException](https://github.com/microsoft/WindowsAppSDK/issues/5171)
- [Phi Silica API Documentation](https://learn.microsoft.com/windows/ai/apis/phi-silica)
- [Limited Access Features Documentation](https://learn.microsoft.com/windows/uwp/packaging/app-capability-declarations#custom-capabilities)
- [Windows Insider Program](https://www.microsoft.com/en-us/windowsinsider/)

---

**Last Updated:** 2025-12-25  
**Confidence:** 0.83
