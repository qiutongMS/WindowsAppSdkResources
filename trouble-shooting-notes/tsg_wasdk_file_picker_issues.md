# Windows App SDK File Picker Language and Configuration Issues

**Error Codes:** N/A (Behavior/Configuration Issues)  
**Affected Area:** File Pickers (FileOpenPicker, FileSavePicker)  
**Common Platforms:** WinUI 3 Desktop, Windows 10/11

---

## Symptom Overview

File pickers in Windows App SDK applications display in incorrect languages, fail to respect default file extension settings, or show file type choices in unexpected order. These issues often manifest after Windows or SDK updates and can significantly impact user experience in multilingual environments.

**You might see:**
- File picker dialog displays in English when system language is Japanese/Chinese/etc.
- Default file extension (.txt, .jpg, etc.) not pre-selected in save dialog
- File type dropdown shows options in wrong order (not matching FileTypeChoices order)
- Language settings work for app UI but not for system dialogs

---

## Related Issues

This guide consolidates multiple related file picker problems:
- [#6105](https://github.com/microsoft/WindowsAppSDK/issues/6105) - FileSavePicker language doesn't match app language after Win11 update
- [#5975](https://github.com/microsoft/WindowsAppSDK/issues/5975) - Default file extension not selected in FileSavePicker
- [#5827](https://github.com/microsoft/WindowsAppSDK/issues/5827) - FileTypeChoices display order incorrect

---

## Quick Diagnosis

1. **Check file picker language vs system language**
   ```csharp
   // Debug current language settings
   var systemLang = Windows.System.UserProfile.GlobalizationPreferences.Languages[0];
   var appLang = Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride;
   Debug.WriteLine($"System: {systemLang}, App: {appLang}");
   
   // Open picker and observe language
   var picker = new FileSavePicker();
   // ... configure picker
   ```
   → If picker language doesn't match app language, see [Scenario 1](#scenario-1-file-picker-ignores-app-language-setting)

2. **Check default file extension behavior**
   ```csharp
   var picker = new FileSavePicker();
   picker.SuggestedFileName = "document";
   picker.FileTypeChoices.Add("Text File", new List<string> { ".txt" });
   picker.FileTypeChoices.Add("Rich Text", new List<string> { ".rtf" });
   picker.DefaultFileExtension = ".txt";
   
   // After user cancels/picks, check what was selected
   ```
   → If .txt is not pre-selected, see [Scenario 2](#scenario-2-defaultfileextension-not-selected-in-save-dialog)

3. **Check FileTypeChoices order**
   ```csharp
   var picker = new FileOpenPicker();
   picker.FileTypeFilter.Add(".jpg");
   picker.FileTypeFilter.Add(".png");
   picker.FileTypeFilter.Add(".gif");
   
   // Open picker and check dropdown order
   ```
   → If order doesn't match code order, see [Scenario 3](#scenario-3-filetypechoices-display-in-wrong-order)

4. **Check Windows version and updates**
   ```powershell
   # Check for recent Windows updates
   Get-HotFix | Sort-Object -Property InstalledOn -Descending | Select-Object -First 5
   ```

---

## Common Scenarios & Solutions

### Scenario 1: File Picker Ignores App Language Setting

**Root Cause:** Starting with certain Windows 11 updates (24H2 and later), the file picker dialog language is now controlled by Windows Display Language rather than the app's `PrimaryLanguageOverride` setting. This is a breaking change from previous behavior where file pickers respected the app's language settings.

**Related Issue(s):** [#6105](https://github.com/microsoft/WindowsAppSDK/issues/6105)

**Environment:**
- Windows 11 24H2+ (Build 22631.4602 or later)
- WinUI 3 Desktop applications
- Apps using `ApplicationLanguages.PrimaryLanguageOverride`

**Before (Windows 11 23H2 and earlier):**
- App sets `PrimaryLanguageOverride = "ja-JP"`
- File picker displays in Japanese
- Consistent language across app and system dialogs

**After (Windows 11 24H2+):**
- App sets `PrimaryLanguageOverride = "ja-JP"`
- App UI displays in Japanese
- File picker displays in English (if Windows Display Language is English)

**Workaround: Guide User to Change Windows Display Language**

Since this is a Windows OS change, there's no code-level fix. You must document for users:

1. **Add language selection notice in your app**
   ```csharp
   // C# - Show info dialog on first run
   public async Task ShowLanguageNotice()
   {
       var dialog = new ContentDialog
       {
           Title = "Language Settings",
           Content = "For the best experience, please set Windows Display Language " +
                     "to match your preferred app language.\n\n" +
                     "Settings > Time & Language > Language & Region > Windows Display Language",
           CloseButtonText = "OK"
       };
       
       dialog.XamlRoot = MainWindow.Content.XamlRoot;
       await dialog.ShowAsync();
   }
   ```

2. **Provide deep link to Windows Settings (if supported)**
   ```csharp
   // Launch Windows Settings to Language page
   var uri = new Uri("ms-settings:regionlanguage");
   await Windows.System.Launcher.LaunchUriAsync(uri);
   ```

3. **Document in app help/FAQ**
   ```markdown
   ## File Picker Language
   
   Due to Windows 11 changes, file picker dialogs (Open/Save) will display
   in your Windows Display Language, not the app's language setting.
   
   To change file picker language:
   1. Open Settings > Time & Language > Language & Region
   2. Set "Windows Display Language" to your preferred language
   3. Sign out and sign back in
   ```

**Alternative: Investigate Third-Party File Pickers**

For critical multilingual apps, consider:
- **Windows.Forms.OpenFileDialog** (may have different behavior)
  ```csharp
  using System.Windows.Forms;
  
  var dialog = new OpenFileDialog();
  dialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
  if (dialog.ShowDialog() == DialogResult.OK)
  {
      var file = await StorageFile.GetFileFromPathAsync(dialog.FileName);
  }
  ```

- **Custom in-app file browser** (full control but complex)

**Verification:**
```csharp
// Check Windows Display Language
using Windows.System.UserProfile;

var displayLang = GlobalizationPreferences.Languages[0];
var appLang = ApplicationLanguages.PrimaryLanguageOverride;

if (displayLang != appLang)
{
    // Warn user about potential language mismatch in pickers
    Debug.WriteLine($"Warning: Display={displayLang}, App={appLang}");
}
```

---

### Scenario 2: DefaultFileExtension Not Selected in Save Dialog

**Root Cause:** The `DefaultFileExtension` property on `FileSavePicker` is not reliably working in Windows App SDK. The picker opens with no extension selected in the file type dropdown, forcing users to manually select the extension even when it's explicitly set in code.

**Related Issue(s):** [#5975](https://github.com/microsoft/WindowsAppSDK/issues/5975)

**Environment:**
- Windows App SDK 1.6+
- FileSavePicker in WinUI 3 Desktop
- Multiple file types in FileTypeChoices

**Expected Behavior:**
```csharp
var picker = new FileSavePicker();
picker.SuggestedFileName = "document";
picker.FileTypeChoices.Add("Text Files", new List<string> { ".txt" });
picker.FileTypeChoices.Add("Rich Text Files", new List<string> { ".rtf" });
picker.DefaultFileExtension = ".txt";  // Should pre-select .txt
```
→ ".txt" should be selected in dropdown when picker opens

**Actual Behavior:**
→ No extension selected, or "All Files (*.*)" selected

**Fix Option 1: Set DefaultFileExtension AFTER FileTypeChoices**

Order matters. Set `DefaultFileExtension` after populating `FileTypeChoices`:

```csharp
var picker = new FileSavePicker();
picker.SuggestedFileName = "document";

// Add all file types FIRST
picker.FileTypeChoices.Add("Text Files", new List<string> { ".txt" });
picker.FileTypeChoices.Add("Rich Text Files", new List<string> { ".rtf" });
picker.FileTypeChoices.Add("Word Documents", new List<string> { ".docx" });

// Set default AFTER adding all choices
picker.DefaultFileExtension = ".txt";

// Initialize window handle
var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(MainWindow);
WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

var file = await picker.PickSaveFileAsync();
```

**Fix Option 2: Make Default Extension the First FileTypeChoice**

Place the default extension as the first item:

```csharp
var picker = new FileSavePicker();
picker.SuggestedFileName = "document";

// Put default extension FIRST
picker.FileTypeChoices.Add("Text Files", new List<string> { ".txt" });  // This will be selected
picker.FileTypeChoices.Add("Rich Text Files", new List<string> { ".rtf" });
picker.FileTypeChoices.Add("Word Documents", new List<string> { ".docx" });

// Still set DefaultFileExtension for good measure
picker.DefaultFileExtension = ".txt";

var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(MainWindow);
WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

var file = await picker.PickSaveFileAsync();
```

**Fix Option 3: Programmatically Append Extension if Missing**

If picker doesn't respect default, ensure extension is added to filename:

```csharp
var picker = new FileSavePicker();
picker.SuggestedFileName = "document";
picker.FileTypeChoices.Add("Text Files", new List<string> { ".txt" });
picker.FileTypeChoices.Add("Rich Text Files", new List<string> { ".rtf" });
picker.DefaultFileExtension = ".txt";

var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(MainWindow);
WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

var file = await picker.PickSaveFileAsync();

if (file != null)
{
    // Ensure extension is present
    var path = file.Path;
    if (!Path.HasExtension(path))
    {
        var newPath = path + ".txt";
        await file.RenameAsync(Path.GetFileName(newPath));
    }
}
```

**Verification:**
```csharp
// Test helper to verify default extension behavior
public static async Task TestDefaultExtension()
{
    var picker = new FileSavePicker();
    picker.SuggestedFileName = "test_default_ext";
    picker.FileTypeChoices.Add("Text", new List<string> { ".txt" });
    picker.FileTypeChoices.Add("JSON", new List<string> { ".json" });
    picker.DefaultFileExtension = ".txt";
    
    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(MainWindow);
    WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
    
    var file = await picker.PickSaveFileAsync();
    
    if (file != null)
    {
        Debug.WriteLine($"Selected: {file.FileType}");
        Debug.Assert(file.FileType == ".txt", "Default extension was not selected!");
    }
}
```

---

### Scenario 3: FileTypeChoices Display in Wrong Order

**Root Cause:** The file type dropdown in `FileOpenPicker` and `FileSavePicker` may display file types in a different order than they were added to the `FileTypeChoices` collection. This appears to be related to Windows sorting the types alphabetically or by some internal logic.

**Related Issue(s):** [#5827](https://github.com/microsoft/WindowsAppSDK/issues/5827)

**Environment:**
- Windows App SDK all versions
- FileOpenPicker and FileSavePicker
- Multiple file types

**Expected Order (as coded):**
```csharp
picker.FileTypeChoices.Add("Images", new List<string> { ".jpg", ".png", ".gif" });
picker.FileTypeChoices.Add("Documents", new List<string> { ".docx", ".pdf" });
picker.FileTypeChoices.Add("All Files", new List<string> { "*" });

// Expected dropdown:
// 1. Images (*.jpg;*.png;*.gif)
// 2. Documents (*.docx;*.pdf)
// 3. All Files (*.*)
```

**Actual Order (in picker):**
```
1. All Files (*.*)
2. Documents (*.docx;*.pdf)
3. Images (*.jpg;*.png;*.gif)
```

**Workaround: Use Numeric or Alpha Prefixes**

Force correct order by prefixing with numbers or letters:

```csharp
var picker = new FileSavePicker();

// Use numeric prefixes
picker.FileTypeChoices.Add("1. Images", new List<string> { ".jpg", ".png", ".gif" });
picker.FileTypeChoices.Add("2. Documents", new List<string> { ".docx", ".pdf" });
picker.FileTypeChoices.Add("3. All Files", new List<string> { "*" });

// Or use alpha prefixes that sort correctly
picker.FileTypeChoices.Add("A) Images", new List<string> { ".jpg", ".png", ".gif" });
picker.FileTypeChoices.Add("B) Documents", new List<string> { ".docx", ".pdf" });
picker.FileTypeChoices.Add("C) All Files", new List<string> { "*" });
```

**Alternative: Remove "All Files" Option**

If "All Files" keeps appearing first, omit it entirely:

```csharp
var picker = new FileOpenPicker();

// Only add specific types, no catch-all
picker.FileTypeFilter.Add(".jpg");
picker.FileTypeFilter.Add(".png");
picker.FileTypeFilter.Add(".gif");
picker.FileTypeFilter.Add(".docx");
picker.FileTypeFilter.Add(".pdf");

// Users can still manually change filter in picker UI
```

**Fix for FileSavePicker: Use Descriptive Names**

Make the first item the most important:

```csharp
var picker = new FileSavePicker();

// Put most common format first
picker.FileTypeChoices.Add("JPEG Image (Recommended)", new List<string> { ".jpg", ".jpeg" });
picker.FileTypeChoices.Add("PNG Image", new List<string> { ".png" });
picker.FileTypeChoices.Add("GIF Animation", new List<string> { ".gif" });

picker.DefaultFileExtension = ".jpg";
```

**Verification:**
```csharp
// Log the order items were added
public static async Task VerifyFileTypeOrder()
{
    var picker = new FileOpenPicker();
    
    var types = new List<(string Name, List<string> Extensions)>
    {
        ("Images", new List<string> { ".jpg", ".png" }),
        ("Documents", new List<string> { ".docx", ".pdf" }),
        ("All Files", new List<string> { "*" })
    };
    
    foreach (var (name, extensions) in types)
    {
        Debug.WriteLine($"Adding: {name}");
        
        if (picker is FileOpenPicker openPicker)
        {
            foreach (var ext in extensions)
                openPicker.FileTypeFilter.Add(ext);
        }
    }
    
    // User must manually verify order in UI (no API to read back)
}
```

---

## Additional Context

### Affected SDK Versions

| Issue | SDK Versions | OS Versions |
|-------|--------------|-------------|
| Language Mismatch | All versions | Windows 11 24H2+ |
| Default Extension | 1.6+ | All Windows 10/11 |
| Order Issue | All versions | All Windows 10/11 |

### Impact

- **Poor User Experience:** Users must manually configure settings that should work by default
- **Localization Problems:** Multilingual apps can't provide fully localized experience
- **Workflow Friction:** Extra steps required for common file operations
- **Accessibility:** Screen readers may announce items in unexpected order

### Relationship to Windows OS Changes

The language issue (#6105) is caused by a Windows 11 OS change, not a Windows App SDK bug. Microsoft has confirmed this is the new expected behavior as of Windows 11 24H2.

The other issues (default extension, order) appear to be Windows App SDK bugs or limitations that need fixing.

---

## Related Documentation

- [FileOpenPicker Class](https://learn.microsoft.com/uwp/api/windows.storage.pickers.fileopenpicker)
- [FileSavePicker Class](https://learn.microsoft.com/uwp/api/windows.storage.pickers.filesavepicker)
- [Windows App SDK File Pickers](https://learn.microsoft.com/windows/apps/develop/file-pickers)
- [Globalization and Localization](https://learn.microsoft.com/windows/apps/design/globalizing/)

---

**Last Updated:** January 5, 2026  
**Confidence Score:** 0.85  
**Status:** Language issue is OS behavior change; Default extension and order issues need SDK fixes
