# Troubleshooting Guide: Storage Pickers Language and RTL Layout Issues

## Symptom
Storage pickers display incorrect language or unwanted RTL layout:
- Picker dialog changes when app language changes
- Picker switches to RTL layout for RTL languages (Persian, Arabic, Hebrew)
- Cannot force specific language/layout for picker

## Affected Components
- Microsoft.Windows.Storage.Pickers, Windows.Storage.Pickers
- Windows App SDK: 1.8.3+
- Issue [#6105](https://github.com/microsoft/WindowsAppSDK/issues/6105)

## Root Cause

When using `ApplicationLanguages.PrimaryLanguageOverride` to change app language, pickers inherit the app's language and layout. No API exists to override this specifically for pickers.

## Solutions

### Solution 1: Temporarily Reset Language

Reset app language before showing picker:

```csharp
// Save current language
string currentLanguage = Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride;

// Reset to system language
Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = string.Empty;

// Show picker
var picker = new FileSavePicker(AppWindow.Id);
picker.FileTypeChoices.Add("Text", new[] { ".txt" });
var file = await picker.PickSaveFileAsync();

// Restore app language
Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = currentLanguage;
```

**Pros:** Ensures picker uses system language  
**Cons:** May cause UI flicker if app refreshes

### Solution 2: Use ResourceContext for UI Only

Avoid `PrimaryLanguageOverride` for UI localization:

```csharp
// Instead of:
// ApplicationLanguages.PrimaryLanguageOverride = language;

// Use ResourceContext for your UI only
ResourceContext resourceContext = this.resourceManager.CreateResourceContext();
resourceContext.QualifierValues["Language"] = selectedLanguage;

// Pickers won't be affected
```

**Note:** Only works if you can manage UI localization through ResourceContext.

### Solution 3: Thread Culture (Limited)

Attempt to set thread culture (may not work reliably):

```csharp
using System.Globalization;

var originalCulture = Thread.CurrentThread.CurrentCulture;
var originalUICulture = Thread.CurrentThread.CurrentUICulture;

try
{
    var enCulture = new CultureInfo("en-US");
    Thread.CurrentThread.CurrentCulture = enCulture;
    Thread.CurrentThread.CurrentUICulture = enCulture;

    var picker = new FileSavePicker(AppWindow.Id);
    picker.FileTypeChoices.Add("Text", new[] { ".txt" });
    var file = await picker.PickSaveFileAsync();
}
finally
{
    Thread.CurrentThread.CurrentCulture = originalCulture;
    Thread.CurrentThread.CurrentUICulture = originalUICulture;
}
```

**Note:** May not work reliably as pickers use `ApplicationLanguages` not thread culture.

## Verification

1. Change app language using `PrimaryLanguageOverride`
2. Open picker and verify language
3. Test with RTL languages (fa-IR, ar-SA, he-IL)
4. Check if picker layout is LTR or RTL

## Known Limitations

- **No API** to override picker language independently
- **Pickers always inherit** app language (by design)
- **RTL layout automatic** - cannot force LTR with RTL languages
- No properties like `picker.Language` or `picker.FlowDirection`

## Alternative: Win32 IFileDialog

For more control, use Win32 APIs (complex, desktop apps only):

```csharp
using Windows.Win32.UI.Shell;

IFileOpenDialog dialog;
PInvoke.CoCreateInstance(typeof(FileOpenDialog).GUID, ...);
// Win32 dialogs use system language by default
```

## Feature Requests

Users have requested:
- Ability to set picker language independently
- Option to force LTR layout
- Properties like `picker.Language` or `picker.FlowDirection`

Please upvote issue [#6105](https://github.com/microsoft/WindowsAppSDK/issues/6105) if you need this functionality.

## Related Resources
- [ApplicationLanguages.PrimaryLanguageOverride](https://learn.microsoft.com/uwp/api/windows.globalization.applicationlanguages.primarylanguageoverride)
- [ResourceContext](https://learn.microsoft.com/uwp/api/windows.applicationmodel.resources.core.resourcecontext)
- [File pickers documentation](https://learn.microsoft.com/windows/apps/develop/files/using-file-folder-pickers)
