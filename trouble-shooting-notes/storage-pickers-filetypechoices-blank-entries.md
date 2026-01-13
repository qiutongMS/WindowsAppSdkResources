# FileOpenPicker File Type Dropdown Shows Blank Entries

**Keywords:** FileOpenPicker, FileTypeFilters, blank entries, file type dropdown, Hide file extensions, FileTypeChoices, Windows File Explorer settings, file type list empty

**Symptom Example:**
When using `Microsoft.Windows.Storage.Pickers.FileOpenPicker`, the file type dropdown in the picker dialog shows mostly blank entries, with only "All files (*.*)" visible.

**Visual symptom:**
```
File type dropdown:
  [blank entry]
  [blank entry]
  [blank entry]
  All files (*.*)
```

**Expected:**
```
  Images (*.jpg, *.png)
  Documents (*.pdf, *.docx)
  All files (*.*)
```

---

## Quick Match

**You're seeing this if:**
- Using `FileOpenPicker` or `FileSavePicker` from `Microsoft.Windows.Storage.Pickers`
- File type dropdown shows blank entries (no labels visible)
- Only "All files" entry shows text
- Using `FileTypeFilters` API (not `FileTypeChoices`)
- Windows File Explorer has "Hide file extensions" enabled (default)

→ Root cause: Implementation hides label before parentheses; nothing to show when extensions hidden

---

## Related Issues

- [#5837](https://github.com/microsoft/WindowsAppSDK/issues/5837) - Most entries in FileOpenPicker file type list are blank

---

## Root Cause

**Why it happens:**

The `FileTypeFilters` implementation in `Microsoft.Windows.Storage.Pickers` uses a display format that assumes file extensions will be visible in the dropdown. The current behavior:

1. **Implementation logic:**
   - Expects format: `Label (*.ext1, *.ext2)`
   - Hides everything before opening parenthesis `(`
   - Shows only content inside parentheses

2. **Windows Explorer interaction:**
   - When Windows "Hide file extensions" is **enabled** (default for most users)
   - File extensions inside `(*.ext1, *.ext2)` are hidden by the OS
   - Result: Nothing visible in dropdown

**Example breakdown:**

```csharp
picker.FileTypeFilters.Add(".jpg");
picker.FileTypeFilters.Add(".png");

// Internal display generation might be:
// "Images (*.jpg, *.png)"
//        ^-- hidden by implementation
//             ^-- hidden by Windows setting
// Result: Empty entry
```

**This affects:**
- WindowsAppSDK 1.8.0+
- All Windows versions where "Hide extensions" is enabled (default)
- Both `FileOpenPicker` and `FileSavePicker`

**Important:** Filtering still works! Files are correctly filtered, only the dropdown **display** is broken.

---

## Solutions

### ✅ Solution 1: User Workaround - Show File Extensions (Immediate)

**Enable file extension display in Windows:**

1. Open File Explorer
2. Click "View" tab → "Options" (or "Folder Options")
3. Go to "View" tab
4. **Uncheck** "Hide extensions for known file types"
5. Click "OK"

**Or via PowerShell:**
```powershell
# Show file extensions for current user
Set-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" `
                 -Name "HideFileExt" -Value 0

# Restart Explorer
Stop-Process -Name explorer -Force
```

**Pros:**
- ✅ Immediate fix
- ✅ Applies to all apps

**Cons:**
- ❌ Requires user action
- ❌ Changes global Windows setting
- ❌ Not a developer-side fix

---

### ✅ Solution 2: Use FileTypeChoices API (Recommended for Developers)

**Available in WindowsAppSDK 2.0-experimental2+**

Use `FileTypeChoices` instead of `FileTypeFilters` for custom labels:

```csharp
using Microsoft.Windows.Storage.Pickers;

var picker = new FileOpenPicker();

// OLD (breaks with hidden extensions):
// picker.FileTypeFilters.Add(".jpg");
// picker.FileTypeFilters.Add(".png");

// NEW (works with custom labels):
picker.FileTypeChoices.Add("Image files", new List<string> { ".jpg", ".png", ".gif" });
picker.FileTypeChoices.Add("Document files", new List<string> { ".pdf", ".docx", ".txt" });
picker.FileTypeChoices.Add("All files", new List<string> { "*" });

var file = await picker.PickSingleFileAsync();
```

**Why this works:**
- Provides explicit labels ("Image files") that aren't hidden
- Extensions shown in parentheses: `Image files (*.jpg, *.png, *.gif)`
- Works regardless of Windows extension visibility setting

**Pros:**
- ✅ Developer-controlled labels
- ✅ User-friendly descriptions
- ✅ Works on all Windows settings

**Cons:**
- ❌ Requires WindowsAppSDK 2.0-experimental2+
- ❌ API not available in 1.8

**When to use:** Any new code targeting WindowsAppSDK 2.0+

---

### Solution 3: Wait for Fix to FileTypeFilters

**Status:** ✅ Fix planned by team

The WindowsAppSDK team plans to update `FileTypeFilters` to:
- Add label back to dropdown (before parentheses)
- Show both label and extensions
- Work correctly with Windows "Hide extensions" setting

**Expected behavior after fix:**
```
picker.FileTypeFilters.Add(".jpg");
// Will display: "JPEG Image (*.jpg)" or similar
```

**Timeline:** TBD (not yet released as of 2026-01)

---

## Code Examples

### Current Issue (FileTypeFilters - Broken Display)

```csharp
var picker = new FileOpenPicker();
picker.FileTypeFilters.Add(".jpg");
picker.FileTypeFilters.Add(".png");
picker.FileTypeFilters.Add(".pdf");

var file = await picker.PickSingleFileAsync();
// Dropdown shows blank entries (but filtering works)
```

### Recommended Pattern (FileTypeChoices - Works Now)

```csharp
var picker = new FileOpenPicker();

// Group by category with descriptive labels
picker.FileTypeChoices.Add("Images", new List<string> { ".jpg", ".jpeg", ".png", ".gif", ".bmp" });
picker.FileTypeChoices.Add("Documents", new List<string> { ".pdf", ".docx", ".txt", ".rtf" });
picker.FileTypeChoices.Add("All supported files", new List<string> { ".jpg", ".png", ".pdf", ".docx" });
picker.FileTypeChoices.Add("All files", new List<string> { "*" });

var file = await picker.PickSingleFileAsync();
// Dropdown shows: "Images (*.jpg, *.jpeg, ...)", "Documents (*.pdf, ...)", etc.
```

---

## Prevention Best Practices

1. **Use FileTypeChoices API** - When targeting WindowsAppSDK 2.0+
2. **Test with default Windows settings** - Most users have "Hide extensions" enabled
3. **Provide descriptive labels** - "Image files" is better than just file extensions
4. **Include "All files" option** - Good UX practice

**Version-specific recommendations:**

```csharp
#if WINDOWSAPPSDK_2_0_OR_GREATER
    // Use FileTypeChoices
    picker.FileTypeChoices.Add("Images", new List<string> { ".jpg", ".png" });
#else
    // Fall back to FileTypeFilters (with known display issue)
    picker.FileTypeFilters.Add(".jpg");
    picker.FileTypeFilters.Add(".png");
    // Document workaround in app help/docs
#endif
```

---

## Verification

**Test on clean Windows install:**

```csharp
// Test code
var picker = new FileOpenPicker();
picker.FileTypeFilters.Add(".jpg");

// Check Windows setting
var hideExt = Microsoft.Win32.Registry.GetValue(
    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
    "HideFileExt", 1);

// If hideExt == 1, dropdown will show blank entries
```

**Check if FileTypeChoices is available:**

```csharp
// Runtime check for FileTypeChoices support
var picker = new FileOpenPicker();
var hasFileTypeChoices = picker.GetType()
    .GetProperty("FileTypeChoices") != null;

if (hasFileTypeChoices)
{
    // Use FileTypeChoices
}
else
{
    // Fall back to FileTypeFilters
    // Note: Display may be blank for some users
}
```

---

## Impact Assessment

**Filtering still works:**
- ✅ Files are correctly filtered by extension
- ✅ User can still select correct file types
- ❌ User cannot see which filter is active (poor UX)

**User impact severity:**
- **High:** Confusing UX, users don't know what they're selecting
- **Medium:** Filtering still functional
- **Low:** "All files" option always visible as fallback

**Affected scenarios:**
- Apps using `FileOpenPicker.FileTypeFilters`
- Users with default Windows settings (extensions hidden)
- WindowsAppSDK 1.8.0 through 2.0-experimental1

---

## Workaround Status

- ✅ **Root cause identified** - Display logic + Windows setting interaction
- ✅ **Developer solution available** - Use FileTypeChoices API (2.0+)
- ✅ **User workaround available** - Show file extensions in Windows
- 🔧 **Fix planned** - Team will update FileTypeFilters display

**Recommended migration path:**
1. **Short-term (WindowsAppSDK 1.8):** Document user workaround in app help
2. **Medium-term (WindowsAppSDK 2.0+):** Migrate to FileTypeChoices API
3. **Long-term:** Wait for FileTypeFilters fix, then choose preferred API

---

**Updated:** 2026-01-12 | **Confidence:** 0.97
