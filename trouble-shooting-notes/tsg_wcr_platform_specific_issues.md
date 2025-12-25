# Platform-Specific WCR Issues (OCR, Snapdragon, etc.)

**Error Codes:** Various, platform-dependent  
**Affected Area:** WCR APIs - Platform/hardware compatibility  
**Common Platforms:** Specific NPU models, Snapdragon devices

---

## Symptom Overview

WCR API failures that occur on specific hardware platforms or require unusual initialization steps. These issues are not related to configuration or SDK version, but to platform-specific requirements or bugs.

**You might see:**
- OCR/TextRecognizer only works after opening Windows Search (Win+Q)
- "Catastrophic failure" on Snapdragon devices
- Platform-specific COMExceptions
- APIs work on one device but not another with same Windows build

---

## Related Issues

This troubleshooting guide consolidates multiple related reports:
- [#5276](https://github.com/microsoft/WindowsAppSDK/issues/5276) - TextRecognizer requires Win+Q initialization
- [#5684](https://github.com/microsoft/WindowsAppSDK/issues/5684) - TextRecognizer catastrophic failure on Snapdragon

---

## Quick Diagnosis

Run through these checks to identify your specific scenario:

1. **Identify your platform:**
   ```powershell
   Get-WmiObject Win32_Processor | Select-Object Name, Description
   Get-PnpDevice | Where-Object {$_.FriendlyName -like "*NPU*"}
   ```
   → Note the CPU/NPU model

2. **Check if using OCR/TextRecognizer APIs:**
   ```csharp
   using Microsoft.Windows.Vision;
   
   // If using TextRecognizer...
   var recognizer = await TextRecognizer.CreateAsync();
   ```
   → If OCR on **AMD or Intel**, see [Scenario 1: OCR Win+Q Requirement](#scenario-1-ocr-requires-winq-initialization-amd-intel)

3. **Check for "Catastrophic failure" error:**
   ```
   System.Runtime.InteropServices.COMException: 灾难性故障
   HResult=0x8000FFFF
   ```
   → If on **Snapdragon**, see [Scenario 2: Snapdragon Catastrophic Failure](#scenario-2-snapdragon-catastrophic-failure)

---

## Common Scenarios & Solutions

### Scenario 1: OCR Requires Win+Q Initialization (AMD, Intel)

**Root Cause:** On AMD Ryzen AI and Intel Core Ultra platforms, the Windows OCR system component requires one-time initialization that, for unclear reasons, only occurs when Windows Search is opened. Before this initialization, `TextRecognizer.EnsureReadyAsync()` fails or `CreateAsync()` throws errors.

This appears to be a Windows platform bug, not a WinAppSDK issue.

**Related Issue(s):** [#5276](https://github.com/microsoft/WindowsAppSDK/issues/5276)

**Workaround:** Initialize OCR through Win+Q before first app use

**Manual workaround (for development/testing):**
1. Press **Win+Q** to open Windows Search
2. Type any text (activates search engine)
3. Close Windows Search
4. Run your app - OCR should now work

**Programmatic workaround:**

```csharp
public static async Task<bool> EnsureOCRInitializedAsync()
{
    try
    {
        // Try to create OCR recognizer
        var recognizer = await TextRecognizer.CreateAsync();
        recognizer?.Dispose();
        return true;
    }
    catch (COMException ex) when (ex.HResult == unchecked((int)0x8000FFFF))
    {
        // OCR not initialized - show user instructions
        var result = await ShowOCRInitDialog();
        
        if (result == true)
        {
            // User says they opened Win+Q, try again
            return await EnsureOCRInitializedAsync();
        }
        
        return false;
    }
}

private static async Task<bool> ShowOCRInitDialog()
{
    var dialog = new ContentDialog
    {
        Title = "OCR Initialization Required",
        Content = @"To enable text recognition, please:
        
1. Press Win+Q on your keyboard
2. Type any text in the search box
3. Close the search window
4. Click OK below to continue

This is a one-time setup requirement.",
        PrimaryButtonText = "OK - I've Done This",
        SecondaryButtonText = "Cancel",
        DefaultButton = ContentDialogButton.Primary
    };
    
    var result = await dialog.ShowAsync();
    return result == ContentDialogResult.Primary;
}
```

**Alternative: Programmatically trigger search (experimental):**

```csharp
using System.Runtime.InteropServices;

public static class OCRInitializer
{
    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
    
    private const byte VK_LWIN = 0x5B;
    private const byte VK_Q = 0x51;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    
    public static async Task TriggerWindowsSearchAsync()
    {
        // Simulate Win+Q press
        keybd_event(VK_LWIN, 0, 0, UIntPtr.Zero);
        keybd_event(VK_Q, 0, 0, UIntPtr.Zero);
        
        await Task.Delay(100);
        
        // Release keys
        keybd_event(VK_Q, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_LWIN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        
        // Give search time to initialize
        await Task.Delay(2000);
        
        // Close search window (send Esc)
        keybd_event(0x1B, 0, 0, UIntPtr.Zero);
        keybd_event(0x1B, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }
}

// Usage:
await OCRInitializer.TriggerWindowsSearchAsync();
var recognizer = await TextRecognizer.CreateAsync();
```

**Verification:**
```csharp
// After workaround, this should succeed
if (TextRecognizer.GetReadyState() == AIFeatureReadyState.Ready)
{
    var recognizer = await TextRecognizer.CreateAsync();
    Console.WriteLine("OCR initialized successfully");
}
```

---

### Scenario 2: Snapdragon Catastrophic Failure

**Root Cause:** On Snapdragon X Elite/Plus devices running Windows 11 24H2, `TextRecognizer` APIs can throw "Catastrophic failure" (0x8000FFFF) even when:
- Manifest is configured correctly
- systemAIModels capability is present
- Other WCR APIs (like Phi Silica) work fine

This appears to be related to Qualcomm NPU driver issues or platform-specific OCR component problems.

**Related Issue(s):** [#5684](https://github.com/microsoft/WindowsAppSDK/issues/5684)

**Fix/Workaround:**

**1. Update Qualcomm drivers:**
```powershell
# Check current NPU driver version
Get-PnpDevice | Where-Object {$_.FriendlyName -like "*Hexagon*" -or $_.FriendlyName -like "*NPU*"} |
    Get-PnpDeviceProperty -KeyName DEVPKEY_Device_DriverVersion
```

Update through:
- Device Manager → Neural Processors → Update Driver
- OEM update utility (e.g., Lenovo Vantage, Dell SupportAssist)
- Windows Update (may have optional Qualcomm driver updates)

**2. Verify Windows build number:**
```powershell
winver
# Should be 26100.4652 or later for best Snapdragon support
```

**3. Check for error details:**
```csharp
try
{
    if (TextRecognizer.GetReadyState() == AIFeatureReadyState.NotReady)
    {
        var loadResult = await TextRecognizer.EnsureReadyAsync();
        
        if (loadResult.Status == AIFeatureReadyResultState.Failure)
        {
            // On Snapdragon, Error.Message may be empty
            Console.WriteLine($"Error Code: {loadResult.Error?.HResult:X8}");
            Console.WriteLine($"Error Message: {loadResult.Error?.Message ?? "No details"}");
        }
    }
    
    var recognizer = await TextRecognizer.CreateAsync();
}
catch (COMException ex)
{
    Console.WriteLine($"HResult: 0x{ex.HResult:X8}");
    Console.WriteLine($"Message: {ex.Message}");
    
    if (ex.HResult == unchecked((int)0x8000FFFF))
    {
        // Catastrophic failure - likely platform issue
        await ShowFallbackOptionsDialog();
    }
}
```

**4. Try the Win+Q workaround (same as Scenario 1):**

Even on Snapdragon, the Win+Q initialization trick may help:
```csharp
// Press Win+Q, type something, close search
// Then try OCR again
```

**5. Use fallback OCR if WCR fails:**
```csharp
public async Task<string> RecognizeTextAsync(SoftwareBitmap bitmap)
{
    try
    {
        // Try Windows WCR OCR first
        var recognizer = await TextRecognizer.CreateAsync();
        var result = await recognizer.RecognizeTextFromImageAsync(bitmap);
        return result.Text;
    }
    catch (COMException ex) when (ex.HResult == unchecked((int)0x8000FFFF))
    {
        // Fall back to Windows.Media.Ocr (older API, but more compatible)
        return await FallbackToClassicOCR(bitmap);
    }
}

private async Task<string> FallbackToClassicOCR(SoftwareBitmap bitmap)
{
    using var stream = new InMemoryRandomAccessStream();
    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
    encoder.SetSoftwareBitmap(bitmap);
    await encoder.FlushAsync();
    
    stream.Seek(0);
    var decoder = await BitmapDecoder.CreateAsync(stream);
    var softwareBitmap = await decoder.GetSoftwareBitmapAsync();
    
    var ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
    var ocrResult = await ocrEngine.RecognizeAsync(softwareBitmap);
    
    return ocrResult.Text;
}
```

**Verification:**
- OCR should work after driver updates + reboot
- If still failing, fallback OCR should provide functionality

---

## Additional Context

### Platform Compatibility Matrix

| Platform | OCR Status | Known Issues | Workaround |
|----------|------------|--------------|------------|
| **Intel Core Ultra** | ⚠️ Requires Win+Q | Slow first-time init | Win+Q workaround |
| **AMD Ryzen AI 300** | ⚠️ Requires Win+Q | Same as Intel | Win+Q workaround |
| **Snapdragon X Elite/Plus** | ⚠️ Catastrophic failure | Driver/platform bugs | Update drivers, fallback OCR |
| **Snapdragon (older)** | ❌ Not supported | No NPU | Use fallback only |

### Why Win+Q Initializes OCR?

**Theory (unconfirmed):**
- Windows Search uses OCR for image indexing
- First search query initializes OCR stack
- WCR OCR uses same underlying Windows component
- Until search initializes it, WCR can't access it

**Microsoft has not officially documented this requirement** - it was discovered through community testing.

### Alternative OCR Solutions

If WCR OCR is unreliable on your target platform:

1. **Windows.Media.Ocr** (classic UWP API)
   - More compatible across platforms
   - Simpler API, fewer dependencies
   - Lower accuracy than WCR

2. **Azure Computer Vision** (cloud)
   - Most accurate
   - Requires internet connection
   - Has cost implications

3. **Tesseract.NET** (open source)
   - Fully offline
   - Good accuracy with proper training
   - Larger deployment size

---

## References

- [Issue #5276: TextRecognizer Win+Q requirement](https://github.com/microsoft/WindowsAppSDK/issues/5276)
- [Issue #5684: Catastrophic failure on Snapdragon](https://github.com/microsoft/WindowsAppSDK/issues/5684)
- [Windows.Media.Ocr Documentation](https://learn.microsoft.com/uwp/api/windows.media.ocr)
- [Qualcomm Snapdragon X Drivers](https://www.qualcomm.com/support)

---

**Last Updated:** 2025-12-25  
**Confidence:** 0.80
