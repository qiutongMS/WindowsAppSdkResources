# Windows App SDK 1.8 Localization and Resource Issues

**Error Codes:** N/A (Configuration/Behavior Issues)  
**Affected Area:** Localization, Resource Management, MRTCore  
**Common Platforms:** Packaged (MSIX), Unpackaged

---

## Symptom Overview

After upgrading to Windows App SDK 1.8, applications experience various localization and resource loading issues including multilingual resource files not being recognized, `PrimaryLanguageOverride` not persisting across app restarts, and `ResourceLoader` failures in unpackaged applications.

**You might see:**
- Application only displays in English despite having multiple language resources
- `ApplicationLanguages.ManifestLanguages` only shows "en" after MSIX packaging
- `PrimaryLanguageOverride` returns null/empty after app relaunch
- `ResourceLoader` throws `System.IO.FileNotFoundException` in unpackaged apps
- Resources worked fine in SDK 1.7 but broken in 1.8

---

## Related Issues

This troubleshooting guide consolidates multiple related reports:
- [#6118](https://github.com/microsoft/WindowsAppSDK/issues/6118) - PrimaryLanguageOverride always returns null/empty
- [#5817](https://github.com/microsoft/WindowsAppSDK/issues/5817) - 1.8 has disrupted localization (manifest languages lost)
- [#5832](https://github.com/microsoft/WindowsAppSDK/issues/5832) - Upgrading to 1.8 breaks ResourceLoader in unpackaged apps

---

## Quick Diagnosis

1. **Check if languages are recognized during debug vs packaged**
   ```csharp
   // Add this code to check language configuration
   Debug.WriteLine($"Manifest Languages: {string.Join(",", ApplicationLanguages.ManifestLanguages)}");
   Debug.WriteLine($"Current Language: {string.Join(",", ApplicationLanguages.Languages)}");
   ```
   → If debugging shows all languages but packaged shows only "en", see [Scenario 1](#scenario-1-multilingual-resources-lost-after-packaging-in-18)

2. **Check PrimaryLanguageOverride persistence**
   ```csharp
   // Set language
   Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = "en-US";
   // Restart app and check
   var savedLang = Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride;
   // If null or empty, see Scenario 2
   ```

3. **Check for ResourceLoader errors in unpackaged apps**
   ```csharp
   try {
       var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
   } catch (System.IO.FileNotFoundException) {
       // See Scenario 3
   }
   ```

4. **Check PRI file names in output directory**
   ```powershell
   # In bin/Release or bin/Debug, check for:
   Get-ChildItem *.pri
   # SDK 1.7 produces: resources.pri
   # SDK 1.8 produces: YourAppName.pri
   ```

---

## Common Scenarios & Solutions

### Scenario 1: Multilingual Resources Lost After Packaging in 1.8

**Root Cause:** Windows App SDK 1.8 introduced changes to the build process that can cause language resource files (.resw) to not be properly included in the packaged MSIX, resulting in only the default English resources being available.

**Related Issue(s):** [#5817](https://github.com/microsoft/WindowsAppSDK/issues/5817)

**Environment:**
- Windows App SDK 1.8.0+
- Packaged (MSIX) applications
- Multiple language .resw files in project

**Debugging shows:**
```
ApplicationLanguages.ManifestLanguages: en,pt,fr,ja,zh-hans,zh-hant,es
ApplicationLanguages.Languages: zh-hans
```

**After MSIX installation shows:**
```
ApplicationLanguages.ManifestLanguages: en
ApplicationLanguages.Languages: en
```

**Fix Option 1: Verify Resource File Build Actions**

1. **Check .resw files are set to PRIResource**
   ```xml
   <!-- In .csproj, ensure .resw files have: -->
   <PRIResource Include="Strings\**\*.resw" />
   ```

2. **Explicitly include language folders**
   ```xml
   <ItemGroup>
     <PRIResource Include="Strings\en\Resources.resw" />
     <PRIResource Include="Strings\pt\Resources.resw" />
     <PRIResource Include="Strings\fr\Resources.resw" />
     <!-- Add all language folders -->
   </ItemGroup>
   ```

3. **Clean and rebuild**
   ```powershell
   Remove-Item -Path "bin", "obj" -Recurse -Force
   dotnet build -c Release
   ```

**Fix Option 2: Verify Package Manifest Resources**

1. **Check Package.appxmanifest includes all languages**
   ```xml
   <Resources>
     <Resource Language="en" />
     <Resource Language="pt" />
     <Resource Language="fr" />
     <Resource Language="ja" />
     <Resource Language="zh-hans" />
     <Resource Language="zh-hant" />
     <Resource Language="es" />
   </Resources>
   ```

2. **Rebuild package**
   ```powershell
   dotnet publish -c Release
   ```

**Fix Option 3: Add MakePri Configuration (Advanced)**

1. **Create priconfig.xml if missing**
   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <resources targetOsVersion="10.0.0" majorVersion="1">
     <index root="\" startIndexAt="\">
       <default>
         <qualifier name="Language" value="en;pt;fr;ja;zh-hans;zh-hant;es" />
       </default>
       <indexer-config type="RESW" convertDotsToSlashes="true" />
       <indexer-config type="PRI" />
     </index>
   </resources>
   ```

2. **Reference in project file**
   ```xml
   <PropertyGroup>
     <AppxPriConfigXmlPath>priconfig.xml</AppxPriConfigXmlPath>
   </PropertyGroup>
   ```

**Verification:**
```powershell
# After packaging, check PRI file contents
makepri dump /if YourApp.msix /of output.xml
# Verify all languages are listed
```

---

### Scenario 2: PrimaryLanguageOverride Not Persisting (WinUI 3 vs UWP Behavior)

**Root Cause:** In Windows App SDK/WinUI 3, `PrimaryLanguageOverride` behaves differently than in UWP. The value is not automatically persisted across application restarts. This appears to be a design difference, not a bug, but is undocumented.

**Related Issue(s):** [#6118](https://github.com/microsoft/WindowsAppSDK/issues/6118)

**Environment:**
- WinUI 3 Desktop applications
- Windows 11 latest
- Using `Microsoft.Windows.Globalization.ApplicationLanguages`

**UWP behavior (expected):**
- Set `PrimaryLanguageOverride = "en-US"`
- Restart app
- Value persists and can be read

**WinUI 3 behavior (actual):**
- Set `PrimaryLanguageOverride = "en-US"`
- Restart app
- Returns null or empty string

**Workaround: Implement Manual Persistence**

1. **Save language preference to local settings**
   ```csharp
   using Windows.Storage;
   
   public class LanguageService
   {
       private const string LANGUAGE_KEY = "AppLanguagePreference";
       
       public static void SetLanguage(string languageTag)
       {
           // Set for current session
           Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = languageTag;
           
           // Persist to local settings
           ApplicationData.Current.LocalSettings.Values[LANGUAGE_KEY] = languageTag;
       }
       
       public static string GetSavedLanguage()
       {
           if (ApplicationData.Current.LocalSettings.Values.TryGetValue(LANGUAGE_KEY, out var value))
           {
               return value as string;
           }
           return null;
       }
       
       public static void RestoreLanguage()
       {
           var savedLanguage = GetSavedLanguage();
           if (!string.IsNullOrEmpty(savedLanguage))
           {
               Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = savedLanguage;
           }
       }
   }
   ```

2. **Call RestoreLanguage on app startup**
   ```csharp
   // In App.xaml.cs constructor or OnLaunched
   public App()
   {
       this.InitializeComponent();
       
       // Restore language before UI loads
       LanguageService.RestoreLanguage();
   }
   ```

3. **Use SetLanguage when user changes preference**
   ```csharp
   // When user selects language
   LanguageService.SetLanguage("en-US");
   
   // Restart app to apply
   Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
   ```

**Verification:**
```csharp
// After restart, check if language is restored
var currentLang = Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride;
var savedLang = LanguageService.GetSavedLanguage();
Debug.Assert(currentLang == savedLang, "Language should match saved preference");
```

---

### Scenario 3: ResourceLoader Fails in Unpackaged Apps After 1.8 Upgrade

**Root Cause:** Windows App SDK 1.8 changed the PRI file naming convention. In 1.7, unpackaged apps used `resources.pri`, but in 1.8 they use `{AppName}.pri`. The `ResourceLoader` API expects to find `resources.pri`, causing a file not found exception.

**Related Issue(s):** [#5832](https://github.com/microsoft/WindowsAppSDK/issues/5832)

**Environment:**
- Unpackaged WinUI 3 applications
- Windows App SDK 1.8.0+
- Using `Windows.ApplicationModel.Resources.ResourceLoader`

**Error:**
```
System.IO.FileNotFoundException: Unable to find the specified file.
   at WinRT.ExceptionHelpers.ThrowExceptionForHR(Int32 hr)
   at Windows.ApplicationModel.Resources.ResourceLoader..ctor()
```

**File outputs:**
- SDK 1.7: `resources.pri`
- SDK 1.8: `YourAppName.pri`

**Fix: Copy/Rename PRI File**

1. **Add post-build event to create resources.pri**
   ```xml
   <!-- Add to .csproj -->
   <Target Name="CopyPriFile" AfterTargets="Build">
     <Copy SourceFiles="$(OutDir)$(AssemblyName).pri" 
           DestinationFiles="$(OutDir)resources.pri" 
           Condition="!Exists('$(OutDir)resources.pri')" />
   </Target>
   ```

2. **Or use MSBuild Exec task**
   ```xml
   <Target Name="EnsureResourcesPri" AfterTargets="Build">
     <Exec Command="copy /Y &quot;$(OutDir)$(AssemblyName).pri&quot; &quot;$(OutDir)resources.pri&quot;" 
           Condition="!Exists('$(OutDir)resources.pri')" />
   </Target>
   ```

3. **Rebuild project**
   ```powershell
   dotnet build -c Release
   ```

**Verification:**
```powershell
# Check both files exist in output
Get-ChildItem "bin\Release\net8.0-windows*\*.pri"
# Should show both YourAppName.pri and resources.pri
```

**Alternative: Update to Use Correct PRI Name (Future)**

If Microsoft provides an API update:
```csharp
// Future API might allow specifying PRI file name
var loader = new ResourceLoader("YourAppName.pri");
```

---

## Additional Context

### SDK 1.7 vs 1.8 Changes

| Aspect | SDK 1.7 | SDK 1.8 |
|--------|---------|---------|
| PRI File Name (Unpackaged) | resources.pri | {AppName}.pri |
| Language Persistence | Inconsistent | Still inconsistent |
| Resource Build Process | Legacy | Updated (may have regressions) |

### Impact

- **Localization Broken:** Users see only English regardless of system language
- **Development Friction:** Different behavior between debug and packaged builds
- **Migration Issues:** Apps require code changes when upgrading from 1.7 to 1.8

### Microsoft Tracking

These issues are being tracked and investigated. Monitor the related GitHub issues for updates on official fixes and potential breaking change documentation.

---

## Related Documentation

- [Windows App SDK Localization](https://learn.microsoft.com/windows/apps/design/globalizing/globalizing-portal)
- [Resource Management System](https://learn.microsoft.com/windows/apps/windows-app-sdk/mrtcore/mrtcore-overview)
- [ApplicationLanguages API](https://learn.microsoft.com/uwp/api/windows.globalization.applicationlanguages)

---

**Last Updated:** January 5, 2026  
**Confidence Score:** 0.90  
**Status:** Multiple workarounds available; Awaiting official fixes and documentation updates
