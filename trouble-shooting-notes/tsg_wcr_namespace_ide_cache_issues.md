# WCR Namespace Not Found - IDE Cache Issues

**Error Code:** CS0246 (Namespace not found)  
**Affected Area:** WCR APIs - IDE/IntelliSense caching  
**Common Platforms:** All platforms

---

## Symptom Overview

You have the correct SDK version, packages, and target framework, but IntelliSense still doesn't show WCR namespaces. The project builds successfully from command line but IDE doesn't recognize the APIs.

**You might see:**
- Red squiggles under `using Microsoft.Windows.AI.Generative` in IDE
- `dotnet build` succeeds from terminal
- IntelliSense doesn't show WCR namespaces in autocomplete
- Namespace appears after IDE restart but disappears again

---

## Root Cause

IDEs (Visual Studio, VS Code, Rider) cache assembly metadata and NuGet package information. After updating packages or changing SDK versions, stale cache can prevent proper namespace discovery even when everything is configured correctly.

---

## Solution

### For Visual Studio

**Method 1: Standard Cache Clear**
1. Close Visual Studio completely
2. Delete `.vs` folder in solution directory
3. Delete `bin` and `obj` folders in all projects
4. Reopen Visual Studio
5. **Tools** → **Options** → **NuGet Package Manager** → **General**
6. Click **Clear All NuGet Cache(s)**
7. **Build** → **Clean Solution**
8. **Build** → **Rebuild Solution**

**Method 2: Deep Clean (if Method 1 fails)**
```powershell
# Close Visual Studio first
$solutionDir = "C:\Path\To\Your\Solution"
cd $solutionDir

# Delete cache directories
Remove-Item .vs -Recurse -Force -ErrorAction SilentlyContinue
Get-ChildItem -Recurse -Directory -Filter bin | Remove-Item -Recurse -Force
Get-ChildItem -Recurse -Directory -Filter obj | Remove-Item -Recurse -Force

# Clear NuGet cache
dotnet nuget locals all --clear

# Clear Visual Studio component cache (careful with this path)
Remove-Item "$env:LOCALAPPDATA\Microsoft\VisualStudio\17.0_*\ComponentModelCache" -Recurse -Force -ErrorAction SilentlyContinue

# Rebuild
dotnet clean
dotnet restore
dotnet build
```

**Method 3: Reset IntelliSense Database**
1. Close Visual Studio
2. Navigate to: `%LOCALAPPDATA%\Microsoft\VisualStudio\<version>\`
3. Delete `ComponentModelCache` folder
4. Reopen Visual Studio
5. Let it rebuild IntelliSense database (may take 2-5 minutes)

---

### For VS Code

**Method 1: Standard Reload**
1. **Ctrl+Shift+P** → **Developer: Reload Window**
2. Wait for C# extension to re-analyze project

**Method 2: Clear OmniSharp Cache**
```powershell
# Close VS Code first

# Clear OmniSharp cache
Remove-Item "$env:USERPROFILE\.omnisharp" -Recurse -Force -ErrorAction SilentlyContinue

# Clear .NET build cache
dotnet clean
Remove-Item bin, obj -Recurse -Force

# Clear VS Code workspace cache
Remove-Item .vscode -Recurse -Force -ErrorAction SilentlyContinue

# Restore and rebuild
dotnet restore
dotnet build
```

**Method 3: Restart OmniSharp Server**
1. **Ctrl+Shift+P** → **OmniSharp: Restart OmniSharp**
2. Check **Output** panel → **OmniSharp Log** for errors
3. Wait for "OmniSharp server started" message

---

### For JetBrains Rider

**Method 1: Invalidate Caches**
1. **File** → **Invalidate Caches...**
2. Select all options:
   - ✅ Clear file system cache and Local History
   - ✅ Clear downloaded shared indexes
   - ✅ Clear Rider & NuGet caches
3. Click **Invalidate and Restart**

**Method 2: Manual Cache Clear**
```powershell
# Close Rider first

# Clear Rider cache
Remove-Item "$env:LOCALAPPDATA\JetBrains\Rider*\cache" -Recurse -Force -ErrorAction SilentlyContinue

# Clear project cache
dotnet clean
Remove-Item bin, obj -Recurse -Force

# Restore
dotnet restore
```

---

## Verification

### Test IntelliSense After Cache Clear

1. **Type partial namespace:**
   ```csharp
   using Microsoft.Windows.
   ```
   IntelliSense should show: `AI`, `Vision`, `Imaging`

2. **Hover over API:**
   ```csharp
   var model = LanguageModel.IsAvailable();
   ```
   Hovering over `LanguageModel` should show documentation

3. **Go to Definition:**
   - Right-click `LanguageModel` → **Go to Definition**
   - Should open decompiled or metadata view

---

## Common Scenarios

### Scenario 1: Just Updated NuGet Package

After updating `Microsoft.WindowsAppSDK` to 1.7-exp3:

```powershell
# Standard workflow
dotnet clean
dotnet nuget locals all --clear
dotnet restore
dotnet build

# Then reload IDE
```

### Scenario 2: Switched Git Branches

After switching branches with different SDK versions:

**Visual Studio:**
1. Close VS
2. Delete `.vs`, `bin`, `obj`
3. Reopen and rebuild

**VS Code:**
1. Reload window (Ctrl+Shift+P → Reload)
2. If still fails, restart OmniSharp

### Scenario 3: IntelliSense Works But Build Fails

This is the **opposite problem** - indicates IDE has newer cache than build:

```powershell
# Force build cache update
dotnet clean
dotnet restore --force
dotnet build --no-incremental
```

---

## Troubleshooting

### IntelliSense still broken after all cache clears?

1. **Verify from terminal first:**
   ```powershell
   dotnet build
   # If this succeeds, it's definitely IDE cache issue
   ```

2. **Check IDE language service is running:**
   
   **VS Code:**
   - Check **Output** → **C#** for errors
   - Look for "Successfully loaded project..."

   **Visual Studio:**
   - Check **Output** → **Build** for warnings
   - Look in **Error List** for project load errors

3. **Try different IDE:**
   - If VS Code fails, try Visual Studio
   - Helps isolate IDE-specific vs project issues

4. **Update IDE extensions:**
   - **VS Code:** Update C# extension
   - **Visual Studio:** Update to latest version
   - **Rider:** Update Rider and ReSharper

---

## Prevention

### Best Practices to Avoid Cache Issues

1. **Always clean before major package updates:**
   ```powershell
   dotnet clean && dotnet nuget locals all --clear
   ```

2. **Add to .gitignore:**
   ```gitignore
   bin/
   obj/
   .vs/
   .vscode/
   *.user
   ```

3. **Use consistent SDK versions across team:**
   - Commit `global.json` to lock SDK version
   - Commit `nuget.config` for package sources

4. **Automate cache clear in build scripts:**
   ```powershell
   # build.ps1
   dotnet clean
   dotnet nuget locals all --clear
   dotnet restore
   dotnet build
   ```

---

## References

- [Visual Studio Cache Locations](https://learn.microsoft.com/visualstudio/ide/reference/cache-locations)
- [OmniSharp Troubleshooting](https://github.com/OmniSharp/omnisharp-roslyn/wiki/Configuration-Options)
- [Rider Cache Management](https://www.jetbrains.com/help/rider/Invalidate_Caches.html)

---

**Last Updated:** 2026-01-04  
**Confidence:** 0.86

## Changelog

**2026-01-04:**
- Split from namespace_not_found.md
- Added IDE-specific detailed steps
- Enhanced troubleshooting section
