# XAML Hot Reload Not Working in .NET 10 Preview with Visual Studio 2026

**Error Codes:** N/A (Feature Not Working)  
**Affected Area:** XAML Hot Reload, Developer Tools  
**Common Platforms:** Windows 11 with .NET 10 Preview + VS 2026 Preview

---

## Symptom Overview

When developing WinUI 3 or Windows App SDK applications using .NET 10 Preview (.NET Preview 9-13, now rebranded as .NET 10) with Visual Studio 2026 Preview, XAML Hot Reload does not function. Changes to XAML files are not reflected in the running application without a full restart. This significantly impacts development productivity.

**You might see:**
- Hot Reload icon in VS toolbar is disabled or shows "Not available"
- XAML changes require full app restart to see updates
- Output window shows "Hot Reload not supported for this project type"
- Feature works in .NET 8/9 but broken in .NET 10 Preview
- Works for other project types (WPF, UWP) but not WinUI 3

---

## Related Issues

- [#6006](https://github.com/microsoft/WindowsAppSDK/issues/6006) - XAML Hot Reload not working in .NET 10 Preview + VS 2026

---

## Quick Diagnosis

1. **Verify .NET version**
   ```xml
   <!-- Check .csproj -->
   <TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>
   ```
   ```powershell
   # Check installed SDK
   dotnet --list-sdks
   # Look for: 10.0.x-preview
   ```

2. **Check Visual Studio version**
   - Help → About Microsoft Visual Studio
   - Look for: Visual Studio 2026 Preview (version 17.x-preview)

3. **Check Hot Reload availability in VS**
   - Run app (F5 or Ctrl+F5)
   - Look at toolbar: Hot Reload icon should be enabled (🔥)
   - If grayed out or missing, feature is unavailable

4. **Check Output window for Hot Reload messages**
   - View → Output
   - Show output from: "Hot Reload"
   - Look for error messages or "Not supported" warnings

5. **Test with .NET 8/9 for comparison**
   ```xml
   <!-- Temporarily change to: -->
   <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
   ```
   - Run app and test Hot Reload
   - If it works, issue is .NET 10 specific

---

## Common Scenarios & Solutions

### Scenario 1: XAML Hot Reload Not Available in .NET 10 Preview

**Root Cause:** The .NET 10 Preview SDK and Visual Studio 2026 Preview have incomplete support for XAML Hot Reload with WinUI 3 projects. This is a known limitation of preview releases. The Hot Reload infrastructure needs to be updated to support the new .NET version, and this work may not be complete in early preview builds.

**Related Issue(s):** [#6006](https://github.com/microsoft/WindowsAppSDK/issues/6006)

**Environment:**
- .NET 10.0.x-preview (previously called .NET Preview 9-13)
- Visual Studio 2026 Preview (17.x-preview)
- WinUI 3 / Windows App SDK projects
- Windows 11

**Symptoms:**
- Hot Reload toolbar button is grayed out or missing
- XAML changes don't apply when pressing Hot Reload button
- Output window shows: "Hot Reload is not supported for this project type"

**Workaround Option 1: Use .NET 8 or .NET 9 (Recommended)**

The most reliable solution is to downgrade to a stable .NET version:

1. **Change target framework to .NET 8**
   ```xml
   <!-- In .csproj, change from: -->
   <TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>
   
   <!-- To: -->
   <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
   ```

2. **Or use .NET 9 if you need newer features**
   ```xml
   <TargetFramework>net9.0-windows10.0.19041.0</TargetFramework>
   ```

3. **Restore and rebuild**
   ```powershell
   dotnet restore
   dotnet build
   ```

4. **Verify Hot Reload works**
   - Press F5 to start debugging
   - Make a change to XAML (e.g., change a button's Content)
   - Click Hot Reload button or press Alt+F10
   - Change should apply immediately

**Workaround Option 2: Use Edit and Continue Instead of Hot Reload**

Edit and Continue may work when Hot Reload doesn't:

1. **Enable Edit and Continue in VS**
   - Tools → Options → Debugging → General
   - ✅ Enable Edit and Continue
   - ✅ Enable Hot Reload on File Save

2. **Enable for C#**
   - Tools → Options → Debugging → Edit and Continue
   - ✅ Enable Edit and Continue

3. **Test Edit and Continue**
   - Start debugging (F5)
   - Make a small C# code change (not XAML)
   - Save file
   - Code change should apply without restart

**Note:** Edit and Continue for C# code may work, but XAML Hot Reload specifically may still not function in .NET 10 Preview.

**Workaround Option 3: Use Debug Without Debugging (Ctrl+F5)**

Run app without debugger and manually restart for changes:

1. **Run without debugging**
   ```
   Press Ctrl+F5 (Start Without Debugging)
   ```

2. **Install LiveXAML (Third-Party Tool)**
   - Commercial tool that provides enhanced Hot Reload
   - May support .NET 10 Preview before official support
   - https://www.livexaml.com/

3. **Create keyboard shortcut for quick restart**
   - Tools → Options → Environment → Keyboard
   - Search for "Debug.RestartNoDebug"
   - Assign shortcut (e.g., Ctrl+Shift+F5)

**Workaround Option 4: Use Visual Studio 2025 (If Available)**

If a stable VS 2025 is available:

1. **Install Visual Studio 2025**
   - Keep VS 2026 Preview for testing
   - Use VS 2025 for daily development

2. **Switch projects between VS versions**
   ```powershell
   # Both VS versions can open same .csproj
   # Just ensure .NET SDK versions are compatible
   ```

---

### Scenario 2: Hot Reload Works Intermittently or Only for Some Files

**Root Cause:** Even when Hot Reload is partially enabled in .NET 10 Preview, certain XAML changes may not be supported, or the feature may work inconsistently depending on the type of change made.

**Environment:**
- .NET 10 Preview with VS 2026 Preview
- Hot Reload button appears but doesn't always work

**Observations:**
- Simple property changes (text, colors) sometimes work
- Adding new controls doesn't work
- Changes to code-behind or resources fail
- Hot Reload works after clean build but not after editing multiple files

**Fix: Understand Hot Reload Limitations**

1. **Check Hot Reload supported changes**
   
   **Typically Supported:**
   - Changing Text, Content properties
   - Changing colors, fonts, margins
   - Toggling Visibility
   
   **Typically NOT Supported:**
   - Adding new controls or elements
   - Changing control types (TextBlock → TextBox)
   - Modifying resources or styles
   - Changes involving code-behind

2. **Use Hot Reload for supported changes only**
   ```xml
   <!-- Supported Hot Reload change: -->
   <Button Content="Click Me" />
   <!-- Change to: -->
   <Button Content="Click Here Now" />
   <!-- Hot Reload should work -->
   ```
   
   ```xml
   <!-- NOT supported Hot Reload change: -->
   <Button Content="Test" />
   <!-- Change to: -->
   <ToggleButton Content="Test" />
   <!-- Requires rebuild -->
   ```

3. **For unsupported changes, do a quick rebuild**
   ```powershell
   # Use keyboard shortcuts for fast iteration
   Ctrl+Shift+B    # Build
   Ctrl+F5         # Start without debugging
   ```

---

### Scenario 3: Hot Reload Causes App Crashes or Unexpected Behavior

**Root Cause:** In preview SDKs, Hot Reload may have bugs that cause runtime instability, especially when reloading complex XAML or when the app state is modified during reload.

**Environment:**
- .NET 10 Preview
- Hot Reload enabled and functioning
- App crashes or behaves strangely after Hot Reload

**Symptoms:**
- App crashes when Hot Reload is applied
- UI elements render incorrectly after reload
- Bindings stop working
- Event handlers fire multiple times

**Fix: Disable Hot Reload or Restart App**

1. **Disable Hot Reload on File Save**
   - Tools → Options → Debugging → Hot Reload
   - ❌ Uncheck "Enable Hot Reload on File Save"

2. **Use Manual Hot Reload Only**
   - Keep Hot Reload available but don't auto-apply
   - Only trigger Hot Reload when you're ready
   - If app crashes, restart (Ctrl+Shift+F5)

3. **Report Bugs to Microsoft**
   ```
   Help → Send Feedback → Report a Problem
   Include:
   - VS version (2026 Preview x.x.x)
   - .NET SDK version (10.0.x-preview.x)
   - Minimal XAML that causes crash
   - Steps to reproduce
   ```

4. **Use .NET 8/9 for stable development**
   - Switch back to .NET 8 for production work
   - Only use .NET 10 Preview for testing new features

---

## Additional Context

### .NET Versioning Note

The .NET team rebranded ".NET Preview 9-13" as ".NET 10 Preview" during the preview cycle. References to both names may appear in documentation and issue trackers.

### Visual Studio and .NET Compatibility

| VS Version | Recommended .NET | Hot Reload Status |
|------------|------------------|-------------------|
| VS 2022 | .NET 8 | ✅ Fully working |
| VS 2022 17.11+ | .NET 9 | ✅ Fully working |
| VS 2026 Preview | .NET 10 Preview | ⚠️ Limited/broken |

### Impact

- **Developer Productivity:** Reduced iteration speed (must restart app for changes)
- **Preview Limitations:** Expected behavior for early preview releases
- **Migration Risk:** Teams should not migrate to .NET 10 until stable

### Microsoft Response

This issue is tracked by Microsoft as a known limitation in .NET 10 Preview releases. Hot Reload support for WinUI 3 is expected to be restored in later preview builds and the final .NET 10 release.

### Expected Timeline

- **.NET 10 Preview:** Hot Reload may not work reliably
- **.NET 10 RC (Release Candidate):** Expected to have working Hot Reload
- **.NET 10 GA (General Availability):** Should have full Hot Reload support

---

## Related Documentation

- [XAML Hot Reload Overview](https://learn.microsoft.com/visualstudio/xaml-tools/xaml-hot-reload)
- [.NET 10 Preview Announcement](https://devblogs.microsoft.com/dotnet/)
- [Visual Studio 2026 Preview Features](https://learn.microsoft.com/visualstudio/releases/2026/release-notes-preview)
- [WinUI 3 Development Tools](https://learn.microsoft.com/windows/apps/winui/winui3/)

---

**Last Updated:** January 5, 2026  
**Confidence Score:** 0.90  
**Status:** Known limitation in preview releases; Use .NET 8/9 for stable development
