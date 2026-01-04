# OCR Requires Win+Q Initialization (Intel Core Ultra, AMD Ryzen AI)

**Error Code:** 0x8000FFFF (Catastrophic failure)  
**Affected Area:** WCR APIs - TextRecognizer initialization  
**Common Platforms:** Intel Core Ultra, AMD Ryzen AI 300

---

## Symptom Overview

On Intel Core Ultra and AMD Ryzen AI platforms, the Windows OCR (TextRecognizer) APIs fail to initialize unless Windows Search has been opened at least once. This manifests as `TextRecognizer.CreateAsync()` throwing exceptions or `EnsureReadyAsync()` failing, even though the NPU and Windows build support WCR.

**You might see:**
- `COMException: Catastrophic failure (0x8000FFFF)` when calling `TextRecognizer.CreateAsync()`
- `TextRecognizer.GetReadyState()` returns `NotReady` even after installing WCR
- OCR suddenly works after using Windows Search (Win+Q)
- Same code works on some devices but not others with identical Windows builds

**This appears to be a Windows platform bug, not a WinAppSDK issue.**

---

## Related Issues

- [#5276](https://github.com/microsoft/WindowsAppSDK/issues/5276) - TextRecognizer requires Win+Q initialization

---

## Root Cause

On AMD Ryzen AI and Intel Core Ultra platforms, the Windows OCR system component requires one-time initialization that, for unclear reasons, only occurs when Windows Search is opened. Before this initialization, `TextRecognizer` APIs fail.

**Theory (unconfirmed):**
- Windows Search uses OCR for image indexing
- First search query initializes the OCR stack
- WCR OCR uses the same underlying Windows component
- Until Search initializes it, WCR can't access it

**Microsoft has not officially documented this requirement** - it was discovered through community testing.

---

## Solutions

### Solution 1: Manual Workaround (Development/Testing)

**Steps:**
1. Press **Win+Q** on your keyboard to open Windows Search
2. Type any text in the search box (this activates the search engine)
3. Close the Windows Search window
4. Run your app - OCR should now work

This is a one-time setup per Windows user profile.

**Verification:**
```csharp
if (TextRecognizer.GetReadyState() == AIFeatureReadyState.Ready)
{
    var recognizer = await TextRecognizer.CreateAsync();
    Console.WriteLine("✅ OCR initialized successfully");
}
```

---

### Solution 2: Programmatic User Prompt

Show users a dialog instructing them to perform the Win+Q workaround:

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

**Usage:**
```csharp
if (!await EnsureOCRInitializedAsync())
{
    // User cancelled - handle gracefully
    ShowMessage("OCR features are unavailable");
}
```

---

### Solution 3: Programmatic Search Trigger (Experimental)

Attempt to programmatically open and close Windows Search to trigger initialization:

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
        
        // Give search time to initialize OCR
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

**⚠️ Warnings:**
- This may not work in all Windows configurations (e.g., Enterprise with search disabled)
- Requires UI interaction permissions
- May be blocked by security software
- Not recommended for production apps

---

## Platform Compatibility

| Platform | Status | Notes |
|----------|--------|-------|
| **Intel Core Ultra** | ⚠️ Requires Win+Q | Slow first-time init (2-5 seconds) |
| **AMD Ryzen AI 300** | ⚠️ Requires Win+Q | Same behavior as Intel |
| **Snapdragon X** | Different issue | See [Snapdragon TSG](tsg_wcr_ocr_snapdragon_catastrophic_failure.md) |
| **Older platforms** | ✅ Usually works | May not have this issue |

---

## Alternative Solutions

If the Win+Q workaround is unacceptable for your app:

### Use Windows.Media.Ocr (Classic UWP API)

```csharp
using Windows.Media.Ocr;
using Windows.Graphics.Imaging;

public async Task<string> RecognizeTextAsync(SoftwareBitmap bitmap)
{
    var ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
    var result = await ocrEngine.RecognizeAsync(bitmap);
    return result.Text;
}
```

**Pros:**
- More compatible across platforms
- No Win+Q initialization required
- Simpler API, fewer dependencies

**Cons:**
- Lower accuracy than WCR OCR
- Doesn't leverage NPU acceleration

### Use Cloud OCR Services

- **Azure Computer Vision**: Most accurate, requires internet
- **Tesseract.NET**: Open source, fully offline
- **Google Cloud Vision**: Good accuracy, cloud-based

---

## References

- [Issue #5276: TextRecognizer Win+Q requirement](https://github.com/microsoft/WindowsAppSDK/issues/5276)
- [Windows.Media.Ocr Documentation](https://learn.microsoft.com/uwp/api/windows.media.ocr)
- [TextRecognizer API Reference](https://learn.microsoft.com/windows/ai/apis/textrecognizer)

---

**Last Updated:** 2026-01-04  
**Confidence:** 0.85

## Changelog

**2026-01-04:**
- Split from platform_specific_issues.md for better MCP resource targeting
- Enhanced workaround solutions

**2025-12-25:**
- Initial version in consolidated TSG
