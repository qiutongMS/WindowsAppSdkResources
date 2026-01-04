# OCR Catastrophic Failure on Snapdragon X Devices

**Error Code:** 0x8000FFFF (Catastrophic failure)  
**Affected Area:** WCR APIs - TextRecognizer on Snapdragon platform  
**Common Platforms:** Snapdragon X Elite, Snapdragon X Plus

---

## Symptom Overview

On Snapdragon X Elite/Plus devices running Windows 11 24H2, `TextRecognizer` APIs throw "Catastrophic failure" (0x8000FFFF) even when:
- Manifest is configured correctly with `systemAIModels` capability
- Other WCR APIs (like Phi Silica) work fine
- Windows build supports WCR

This appears to be related to Qualcomm NPU driver issues or platform-specific OCR component problems, not app configuration.

**You might see:**
- `COMException: 灾难性故障 (0x8000FFFF)` when calling `TextRecognizer.CreateAsync()`
- Error message may be in Chinese on some Windows builds
- `TextRecognizer.GetReadyState()` may return `NotReady`
- Other WCR APIs (LanguageModel, ImageDescriber) work fine on the same device

---

## Related Issues

- [#5684](https://github.com/microsoft/WindowsAppSDK/issues/5684) - TextRecognizer catastrophic failure on Snapdragon

---

## Root Cause

The root cause is **Qualcomm NPU driver compatibility issues** or Windows OCR component bugs specific to Snapdragon X platform. Microsoft and Qualcomm are aware of this issue.

**Known contributing factors:**
- Outdated Qualcomm Hexagon NPU drivers
- Specific Windows 11 build numbers with incomplete Snapdragon support
- Platform-specific OCR initialization failures

---

## Solutions

### Solution 1: Update Qualcomm Drivers

**Check current NPU driver version:**
```powershell
Get-PnpDevice | Where-Object {$_.FriendlyName -like "*Hexagon*" -or $_.FriendlyName -like "*NPU*"} |
    Get-PnpDeviceProperty -KeyName DEVPKEY_Device_DriverVersion
```

**Update drivers through:**

1. **Windows Update** (recommended first)
   ```powershell
   # Check for optional updates
   Start-Process "ms-settings:windowsupdate"
   ```
   Look for optional Qualcomm driver updates

2. **Device Manager**
   - Right-click Start → Device Manager
   - Expand "Neural Processors" or "System Devices"
   - Find "Qualcomm Hexagon NPU" or similar
   - Right-click → Update Driver → Search automatically

3. **OEM Update Utilities**
   - **Lenovo**: Lenovo Vantage
   - **Dell**: Dell SupportAssist  
   - **HP**: HP Support Assistant
   - **Microsoft Surface**: Surface app

4. **Qualcomm Website** (if OEM doesn't provide updates)
   - Visit [Qualcomm Support](https://www.qualcomm.com/support)
   - Search for your specific Snapdragon X model

**After updating, restart the system.**

---

### Solution 2: Verify Windows Build

Check your Windows build number:
```powershell
winver
# Or
Get-ComputerInfo | Select-Object WindowsVersion, OSBuild
```

**Recommended builds for Snapdragon X:**
- **Windows 11 24H2**: Build 26100.4652 or later
- **Windows 11 Insider**: Latest available

If you're on an older build:
```powershell
# Check for Windows updates
Start-Process "ms-settings:windowsupdate"
```

---

### Solution 3: Try Win+Q Workaround

Even on Snapdragon, the [Win+Q initialization workaround](tsg_wcr_ocr_winq_initialization_intel_amd.md) sometimes helps:

1. Press **Win+Q** to open Windows Search
2. Type any text
3. Close Windows Search
4. Try `TextRecognizer.CreateAsync()` again

This initializes the Windows OCR stack, which may resolve some initialization issues.

---

### Solution 4: Detailed Error Diagnostics

Get more error information:

```csharp
try
{
    if (TextRecognizer.GetReadyState() == AIFeatureReadyState.NotReady)
    {
        var loadResult = await TextRecognizer.EnsureReadyAsync();
        
        if (loadResult.Status == AIFeatureReadyResultState.Failure)
        {
            // On Snapdragon, Error.Message may be empty or in Chinese
            Console.WriteLine($"Error Code: {loadResult.Error?.HResult:X8}");
            Console.WriteLine($"Error Message: {loadResult.Error?.Message ?? "No details"}");
            
            // Log for debugging
            LogToFile($"TextRecognizer failed: HResult={loadResult.Error?.HResult:X8}");
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
        Console.WriteLine("⚠️ This is a known Snapdragon platform issue");
        Console.WriteLine("Try: 1) Update drivers, 2) Update Windows, 3) Use fallback OCR");
    }
}
```

---

### Solution 5: Fallback to Classic OCR API

If WCR OCR remains unreliable, implement a fallback:

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
        // Fall back to Windows.Media.Ocr (older API, more compatible)
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

**Windows.Media.Ocr characteristics:**
- ✅ More compatible across Snapdragon platforms
- ✅ Simpler API, fewer dependencies
- ✅ Usually doesn't require NPU drivers
- ❌ Lower accuracy than WCR OCR
- ❌ Doesn't leverage NPU acceleration

---

## Platform Compatibility Matrix

| Platform | OCR Status | Known Issues | Primary Fix |
|----------|------------|--------------|-------------|
| **Snapdragon X Elite** | ⚠️ Catastrophic failure | Driver/NPU bugs | Update drivers |
| **Snapdragon X Plus** | ⚠️ Catastrophic failure | Same as Elite | Update drivers |
| **Snapdragon 8cx Gen 3** | ❌ Not supported | No WCR support | Use fallback only |
| **Older Snapdragon** | ❌ Not supported | No NPU for WCR | Use fallback only |

---

## Verification

After applying fixes:

```csharp
// Test OCR functionality
try
{
    var state = TextRecognizer.GetReadyState();
    Console.WriteLine($"TextRecognizer state: {state}");
    
    if (state == AIFeatureReadyState.Ready)
    {
        var recognizer = await TextRecognizer.CreateAsync();
        Console.WriteLine("✅ TextRecognizer initialized successfully");
        
        // Test with a simple image
        // ... your test code ...
    }
    else
    {
        Console.WriteLine("⚠️ TextRecognizer not ready, consider using fallback OCR");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Still failing: {ex.Message}");
    Console.WriteLine("Recommend using Windows.Media.Ocr fallback");
}
```

---

## Alternative OCR Solutions

If WCR OCR remains unreliable on your Snapdragon devices:

1. **Windows.Media.Ocr** (UWP API) - Most compatible
2. **Azure Computer Vision** - Cloud-based, most accurate
3. **Tesseract.NET** - Open source, fully offline
4. **Google Cloud Vision** - Cloud-based, good accuracy

---

## References

- [Issue #5684: Catastrophic failure on Snapdragon](https://github.com/microsoft/WindowsAppSDK/issues/5684)
- [Qualcomm Snapdragon X Drivers](https://www.qualcomm.com/support)
- [Windows.Media.Ocr Documentation](https://learn.microsoft.com/uwp/api/windows.media.ocr)
- [Snapdragon X Platform Support](https://www.qualcomm.com/products/mobile/snapdragon)

---

**Last Updated:** 2026-01-04  
**Confidence:** 0.75

## Changelog

**2026-01-04:**
- Split from platform_specific_issues.md for better MCP resource targeting
- Added detailed driver update instructions
- Enhanced fallback solution examples

**2025-12-25:**
- Initial version in consolidated TSG
