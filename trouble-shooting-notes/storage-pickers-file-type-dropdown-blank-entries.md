# FileOpenPicker File Type Dropdown Shows Blank Entries

**Keywords:** fileopicker filetypefilter blank-entries file-extension-hidden file-explorer-settings dropdown-empty  
**Last Updated:** 2026-01-12  
**Status:** ✅ Root Cause Identified - Fix Planned

## Related Issues

- microsoft/WindowsAppSDK#5837 - FileOpenPicker file type list shows blank entries (1.8.0+)

## Symptom

When using `FileOpenPicker` or `FileSavePicker`, the file type dropdown shows **blank entries** instead of descriptive labels. Filtering still works, but users can't see what file types each option represents.

**Visual Impact:**
```
Save as type: [dropdown]
              | [empty]         |  ← Should be "Text Files (*.txt)"  
              | [empty]         |  ← Should be "Images (*.png, *.jpg)"
              | All files (*.*) |  ← Only this shows (system default)
```

## Error Example

```csharp
var picker = new Microsoft.Windows.Storage.Pickers.FileOpenPicker();
picker.FileTypeFilters.Add(".txt");
picker.FileTypeFilters.Add(".log");
picker.FileTypeFilters.Add(".md");

// Initialize and show picker
var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
var file = await picker.PickSingleFileAsync();

// Result: Dropdown shows blank entries except "All files (*.*)"
```

**Screenshot from issue:**
![Blank file type dropdown entries](https://github.com/user-attachments/assets/a6ae5def-4fff-435b-ab07-95ea4e00571a)

## Root Cause

✅ **Identified by Microsoft team (Nov 2025):**

The WindowsAppSDK file picker implementation **hides the label** before parentheses and only shows the file extension pattern inside parentheses. This works fine **ONLY if:**
- Windows File Explorer setting **"Show file name extensions"** is **ENABLED**

**What happens:**

| Windows Setting | What Picker Shows |
|----------------|-------------------|
| ✅ Show extensions enabled | `(*.txt, *.log)` ← Visible |
| ❌ Show extensions disabled | `( )` ← Empty parentheses (blank) |

**Code issue** (in WindowsAppSDK source):
```cpp
// dev/Interop/StoragePickers/PickerCommon.cpp line ~320
// Sets description to empty string, expecting parentheses content to display
comdlg_filterspec.pszName = L"";  // ← This causes blank if extensions hidden
comdlg_filterspec.pszSpec = /* file pattern like L"*.txt" */;
```

**Design assumption:** User has "Show file extensions" enabled (not Windows default).

## Solutions

### ✅ Solution 1: Enable "Show File Extensions" in Windows (User Workaround)

**For end users experiencing this issue:**

1. Open **File Explorer**
2. Click **View** tab (or ⋯ menu in Windows 11)
3. Check **"File name extensions"**

**Windows 10:**
- File Explorer → View tab → ☑ File name extensions

**Windows 11:**
- File Explorer → ⋯ (View menu) → Show → ☑ File name extensions

**After enabling this setting:**
- File picker dropdowns show `(*.txt, *.log)` correctly
- All apps benefit (not just WinUI apps)

### ✅ Solution 2: Use FileTypeChoices in 2.0-experimental2+ (Developer Fix)

**Available in WinAppSDK 2.0-experimental2+:**

```csharp
var picker = new Microsoft.Windows.Storage.Pickers.FileOpenPicker();

// OLD way (causes blank entries):
// picker.FileTypeFilters.Add(".txt");

// NEW way (allows custom labels):
picker.FileTypeChoices.Add("Text Documents", new List<string> { ".txt", ".log" });
picker.FileTypeChoices.Add("Markdown Files", new List<string> { ".md" });
picker.FileTypeChoices.Add("All Files", new List<string> { "*" });

// Now dropdown shows:
// Text Documents (*.txt, *.log)
// Markdown Files (*.md)
// All Files (*.*)
```

**Benefits:**
- Custom descriptive labels
- Works regardless of Windows settings
- Better user experience

### Solution 3: Include Extensions in Description (Workaround for 1.8)

If stuck on 1.8 and can't use FileTypeChoices yet:

```csharp
// Doesn't actually fix it, but documents intent
// (Still shows blank if extensions hidden, but code is clearer)

var picker = new FileSavePicker();

// At least code documents what SHOULD appear
picker.SuggestedFileName = "document.txt"; // Hint at file type
picker.DefaultFileExtension = ".txt";      // Default selection

picker.FileTypeFilters.Add(".txt");
picker.FileTypeFilters.Add(".log");

// User must enable "Show file extensions" for this to work
```

**Note:** This is **not a real fix**, just makes code intent clear.

### ✅ Solution 4: Wait for Official Fix

**Microsoft response (Nov 2025):**

> "Nice catch! This is indeed a subtle bug. I will add the label back to FileTypeFilters!"

**Status:** Fix is planned but not yet released.

**Tracking:**
- Issue #5837 remains open
- Fix will be in future WindowsAppSDK release (TBD)

## Verification

**Test if your app is affected:**

```csharp
// Test code
var picker = new Microsoft.Windows.Storage.Pickers.FileOpenPicker();
picker.FileTypeFilters.Add(".txt");
picker.FileTypeFilters.Add(".xml");
picker.FileTypeFilters.Add(".json");

// Show picker and visually inspect dropdown
// If entries are blank → Issue affects you
// If entries show (*.txt) etc → Extensions are enabled or fix is applied
```

**User environment check:**

```powershell
# Check if extensions are hidden (PowerShell)
$key = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"
$value = Get-ItemProperty -Path $key -Name "HideFileExt" -ErrorAction SilentlyContinue

if ($value.HideFileExt -eq 1) {
    Write-Warning "File extensions are HIDDEN - pickers will show blank entries"
    Write-Host "Fix: Enable 'Show file name extensions' in File Explorer" -ForegroundColor Yellow
} else {
    Write-Host "File extensions are visible - pickers should work correctly" -ForegroundColor Green
}
```

## Prevention

**For developers:**

1. **Document requirement** - Add to app README:
   > For best experience, enable "Show file name extensions" in Windows File Explorer settings.

2. **Upgrade to 2.0-experimental2+** - Use `FileTypeChoices` instead of `FileTypeFilters`

3. **Test on default Windows** - Don't assume users have "Show extensions" enabled

4. **Add setup instructions** - Include in first-run guide or help docs

**For users:**

1. **Enable "Show file extensions"** - This is good practice anyway (shows file types, prevents `.txt.exe` malware tricks)

## Why This Matters

**Windows default:** "Hide file name extensions" is **enabled by default** in fresh Windows installs.

**Impact:**
- Most users see blank dropdown entries
- Users can't tell which file types are supported
- Filtering still works, but UX is poor

**Who is affected:**
- Apps using `FileOpenPicker`/`FileSavePicker` in WindowsAppSDK 1.8+
- Particularly visible when multiple file types are offered

## Version Information

- **Affected versions:** WindowsAppSDK 1.8.0+
- **Root cause:** Implementation assumes "Show file extensions" is enabled
- **Status:** 
  - ✅ Root cause identified by Microsoft team (Nov 2025)
  - ⏳ Fix planned (not yet released)
- **Workarounds:** 
  - **Best:** Use FileTypeChoices (2.0-experimental2+)
  - **User-side:** Enable "Show file extensions" in Windows
- **Expected fix:** Future WindowsAppSDK release (version TBD)
