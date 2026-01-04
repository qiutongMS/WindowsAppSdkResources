# WCR Namespace Not Found - SDK Version Too Old

**Error Code:** CS0246 (Namespace not found)  
**Affected Area:** WCR APIs - SDK version requirements  
**Common Platforms:** All platforms

---

## Symptom Overview

The namespace `Microsoft.Windows.AI.Generative` (or `.Vision`, `.Imaging`) cannot be found because you're using an older Windows App SDK version that doesn't include WCR APIs.

**You might see:**
- Error CS0246: The type or namespace name 'Microsoft.Windows.AI.Generative' could not be found
- IntelliSense shows no `Microsoft.Windows.AI.*` namespaces
- Documentation references APIs that don't exist in your NuGet packages
- "Where is this API?" confusion despite following documentation

---

## Related Issues

- [#4816](https://github.com/microsoft/WindowsAppSDK/issues/4816) - Where is Microsoft.Windows.AI.Generative?
- [#4902](https://github.com/microsoft/WindowsAppSDK/issues/4902) - Cannot find the namespace
- [#5028](https://github.com/microsoft/WindowsAppSDK/issues/5028) - Phi Silica missing in Experimental 2

---

## Root Cause

The `Microsoft.Windows.AI.Generative` namespace and Phi Silica APIs were introduced in **Windows App SDK 1.7-experimental3** (released February 2025). Earlier versions do NOT contain these APIs:

- ❌ 1.6.x and earlier - No WCR
- ❌ 1.7.0 (stable) - No WCR  
- ❌ 1.7-experimental1 - No WCR (announced but not released)
- ❌ 1.7-experimental2 - No WCR (still in development)
- ✅ **1.7-experimental3** - WCR APIs available
- ✅ 2.0-experimental1+ - WCR APIs available

### Documentation vs Reality

Microsoft published API documentation for Phi Silica **before the APIs were available** in released SDK packages, causing confusion:

- 📄 Documentation published: November 2024 (Ignite announcement)
- 📦 Actual API availability: February 2025 (SDK 1.7-exp3)

---

## Solution

### Step 1: Check Current SDK Version

```xml
<!-- Look in your .csproj file -->
<PackageReference Include="Microsoft.WindowsAppSDK" Version="?" />
```

Or check via command line:
```powershell
dotnet list package | Select-String "WindowsAppSDK"
```

### Step 2: Update to 1.7-experimental3 or Later

**Update your .csproj:**
```xml
<PackageReference Include="Microsoft.WindowsAppSDK" 
                  Version="1.7.250127003-experimental3" />

<!-- Or use latest experimental/stable with WCR support -->
<!-- Check: https://github.com/microsoft/WindowsAppSDK/releases -->
```

### Step 3: Enable Experimental NuGet Feed (if needed)

Create or edit `nuget.config` in your solution root:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="WindowsAppSDK-Experimental" 
         value="https://pkgs.dev.azure.com/microsoft/ProjectReunion/_packaging/WindowsAppSDK-Experimental/nuget/v3/index.json" />
  </packageSources>
</configuration>
```

### Step 4: Clean and Restore

```powershell
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages
dotnet restore

# Rebuild project
dotnet clean
dotnet build
```

### Step 5: Verify Namespace Is Available

```csharp
using Microsoft.Windows.AI.Generative;
using Microsoft.Windows.Vision;
using Microsoft.Windows.Imaging;

// This should compile now
var isAvailable = LanguageModel.IsAvailable();
Console.WriteLine($"Phi Silica available: {isAvailable}");
```

---

## Verification

### Confirm Correct Package Version

```powershell
# Check installed version
dotnet list package | Select-String "WindowsAppSDK"

# Expected output:
# Microsoft.WindowsAppSDK  1.7.250127003-experimental3
```

### Verify DLLs Contain WCR APIs

```powershell
# Check package contents
$pkgPath = "$env:USERPROFILE\.nuget\packages\microsoft.windowsappsdk\1.7.250127003-experimental3"

# Look for AI namespaces
Get-ChildItem "$pkgPath\lib\**\*.dll" -Recurse | ForEach-Object {
    $content = Select-String -Path $_.FullName -Pattern "AI.Generative" -Quiet
    if ($content) {
        Write-Host "✅ Found in: $($_.Name)"
    }
}
```

### Test IntelliSense

Type in Visual Studio/VS Code:
```csharp
using Microsoft.Windows.
```

IntelliSense should show:
- `AI`
- `AI.Generative`  
- `Vision`
- `Imaging`

---

## API Availability Timeline

| SDK Version | WCR APIs | Release Date | Notes |
|-------------|----------|--------------|-------|
| 1.6.x | ❌ No | 2024 | Stable, no WCR |
| 1.7.0 (stable) | ❌ No | 2024 | Stable, no WCR |
| 1.7-exp1 | ❌ No | N/A | Announced but not released |
| 1.7-exp2 | ❌ No | Early 2025 | In development |
| **1.7-exp3** | ✅ **Yes** | Feb 2025 | First with WCR |
| 2.0-exp1 | ✅ Yes | 2025 | Continued support |
| Future stable | ⏳ TBD | 2025/2026 | WCR to stable channel |

---

## Available WCR Namespaces

Once on SDK 1.7-exp3+, you'll have access to:

```csharp
// Language Models (Phi Silica)
using Microsoft.Windows.AI.Generative;
// - LanguageModel
// - LanguageModelChat
// - LanguageModelResponse

// OCR / Text Recognition
using Microsoft.Windows.Vision;
// - TextRecognizer
// - TextRecognitionResult

// Image AI
using Microsoft.Windows.Imaging;
// - ImageDescriber
// - ImageDescription

// Base AI types
using Microsoft.Windows.AI;
// - AIFeatureReadyState
// - AIFeatureAvailabilityResult
```

---

## Troubleshooting

### Still can't find namespace after update?

1. **Verify you're using experimental channel:**
   ```powershell
   # Check nuget.config has experimental feed
   Get-Content nuget.config
   ```

2. **Clear ALL caches:**
   ```powershell
   # NuGet cache
   dotnet nuget locals all --clear
   
   # Build cache
   Remove-Item bin, obj -Recurse -Force
   
   # VS cache (if using Visual Studio)
   Remove-Item .vs -Recurse -Force
   ```

3. **Check for package source authentication:**
   ```powershell
   # If experimental feed requires auth
   dotnet nuget add source https://pkgs.dev.azure.com/... --name WindowsAppSDK-Experimental --username YOUR_EMAIL --password YOUR_PAT
   ```

4. **Try installing directly:**
   ```powershell
   dotnet add package Microsoft.WindowsAppSDK --version 1.7.250127003-experimental3 --source WindowsAppSDK-Experimental
   ```

### Using stable channel only?

If you cannot use experimental packages (company policy, production apps):

- WCR APIs are **NOT available in stable channel yet** (as of Jan 2026)
- Expected in stable channel later in 2025/2026
- No workaround - must wait for stable release

---

## Production Readiness Warning

⚠️ **Experimental Channel APIs:**
- Not recommended for production apps
- Subject to breaking changes
- May have bugs or incomplete features
- Use for prototyping/development only

✅ **For production apps:**
- Wait for WCR APIs in stable channel
- Monitor [Windows App SDK releases](https://github.com/microsoft/WindowsAppSDK/releases)
- Plan migration timeline

---

## References

- [Issue #4816: Where is Microsoft.Windows.AI.Generative](https://github.com/microsoft/WindowsAppSDK/issues/4816)
- [Issue #4902: Cannot find namespace](https://github.com/microsoft/WindowsAppSDK/issues/4902)  
- [Issue #5028: Phi Silica missing in Exp 2](https://github.com/microsoft/WindowsAppSDK/issues/5028)
- [Windows App SDK Releases](https://github.com/microsoft/WindowsAppSDK/releases)
- [Experimental Channel Documentation](https://learn.microsoft.com/windows/apps/windows-app-sdk/experimental-channel)
- [Windows Copilot Runtime APIs](https://learn.microsoft.com/windows/ai/apis/)

---

**Last Updated:** 2026-01-04  
**Confidence:** 0.92

## Changelog

**2026-01-04:**
- Split from namespace_not_found.md for better MCP resource targeting
- Updated SDK version information
- Enhanced verification steps

**2025-12-25:**
- Initial version in consolidated TSG
