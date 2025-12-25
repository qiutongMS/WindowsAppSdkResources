# Build and Package Configuration Failures with WCR

**Error Codes:** Various MSBuild errors, publish output conflicts  
**Affected Area:** Build system, NuGet package configuration  
**Common Platforms:** All platforms during build/packaging

---

## Symptom Overview

Build or packaging failures when referencing Windows App SDK WCR/AI packages. These manifest as duplicate file errors, missing assembly references, or self-contained deployment issues.

**You might see:**
- Build error: "Found multiple publish output files with the same relative path"
- Error: "Cannot find Microsoft.Windows.AI.* assemblies"
- Runtime error: "Could not load file or assembly 'Microsoft.Windows.AI...'"
- Deployment error with WindowsAppSDKSelfContained setting

---

## Related Issues

This troubleshooting guide consolidates multiple related reports:
- [#5439](https://github.com/microsoft/WindowsAppSDK/issues/5439) - Build failure when referencing WinUI and AI packages
- [#5280](https://github.com/microsoft/WindowsAppSDK/issues/5280) - WindowsAppSDKSelfContained deployment error

---

## Quick Diagnosis

Run through these checks to identify your specific scenario:

1. **Check for duplicate .winmd files in build output**
   ```powershell
   # Look at build error message for patterns like:
   # "Microsoft.Graphics.Imaging.winmd" appears multiple times
   ```
   → If you see `.winmd` file conflicts, see [Scenario 1: Multiple Package Reference Conflicts](#scenario-1-multiple-package-reference-conflicts)

2. **Check which WindowsAppSDK packages you're referencing**
   ```xml
   <!-- In .csproj file, look for: -->
   <PackageReference Include="Microsoft.WindowsAppSDK" />
   <PackageReference Include="Microsoft.WindowsAppSDK.WinUI" />
   <PackageReference Include="Microsoft.WindowsAppSDK.AI" />
   ```
   → If referencing **individual split packages**, see [Scenario 1](#scenario-1-multiple-package-reference-conflicts)

3. **Check self-contained deployment setting**
   ```xml
   <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
   ```
   → If this setting causes errors, see [Scenario 2: Self-Contained Deployment Issues](#scenario-2-self-contained-deployment-issues)

---

## Common Scenarios & Solutions

### Scenario 1: Multiple Package Reference Conflicts

**Root Cause:** When referencing both individual WindowsAppSDK packages (like `Microsoft.WindowsAppSDK.WinUI` and `Microsoft.WindowsAppSDK.AI`) separately, the NuGet package structure causes duplicate `.winmd` metadata files to be included in the publish output. The files exist in both the `metadata/` folder and the `runtimes-framework/` folder within each package.

**Related Issue(s):** [#5439](https://github.com/microsoft/WindowsAppSDK/issues/5439)

**Fix:** Use metapackage or add explicit package references

**Solution A: Use the main metapackage (Recommended)**

Replace individual package references with the main metapackage:

```xml
<!-- REMOVE these individual references: -->
<!-- <PackageReference Include="Microsoft.WindowsAppSDK.WinUI" Version="1.8.xxx" /> -->
<!-- <PackageReference Include="Microsoft.WindowsAppSDK.AI" Version="1.8.xxx" /> -->

<!-- USE the metapackage instead: -->
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.8.250507002-experimental" />
```

**Solution B: Add explicit Microsoft.Windows.SDK.BuildTools reference**

If you must use split packages, add the BuildTools package:

```xml
<PackageReference Include="Microsoft.WindowsAppSDK.WinUI" Version="1.8.250507002-experimental" />
<PackageReference Include="Microsoft.WindowsAppSDK.AI" Version="1.8.135-experimental" />

<!-- Add this to resolve conflicts: -->
<PackageReference Include="Microsoft.Windows.SDK.BuildTools" Version="10.0.22621.3233" />
```

**Solution C: Exclude duplicate files**

Add this to your .csproj to exclude duplicate metadata files:

```xml
<ItemGroup>
  <Content Remove="**\metadata\*.winmd" />
  <None Remove="**\metadata\*.winmd" />
</ItemGroup>
```

**Verification:**
```powershell
# Clean and rebuild
dotnet clean
dotnet build -c Release

# Check build succeeds with no duplicate file warnings
# Build output should show: Build succeeded. 0 Warning(s)
```

---

### Scenario 2: Self-Contained Deployment Issues

**Root Cause:** The `WindowsAppSDKSelfContained` setting affects how the Windows App SDK runtime is deployed with your app. Incorrect usage can cause:
- Missing runtime DLLs at deployment
- Conflicts with framework-dependent deployment
- Issues in unpackaged scenarios

**Related Issue(s):** [#5280](https://github.com/microsoft/WindowsAppSDK/issues/5280)

**Fix:** Configure self-contained deployment correctly based on scenario

**For MSIX-packaged apps (most WCR scenarios):**

```xml
<PropertyGroup>
  <!-- For packaged apps, use framework-dependent (default) -->
  <WindowsAppSDKSelfContained>false</WindowsAppSDKSelfContained>
  
  <!-- OR omit the setting entirely - defaults to false for packaged -->
  
  <WindowsPackageType>MSIX</WindowsPackageType>
</PropertyGroup>
```

**For unpackaged/console apps (limited WCR support):**

```xml
<PropertyGroup>
  <!-- For unpackaged, you need self-contained -->
  <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
  
  <!-- Important: WCR APIs require MSIX as of 1.7-exp2+ -->
  <!-- So this config only works for non-WCR scenarios -->
</PropertyGroup>
```

**If seeing errors with self-contained setting:**

1. **Check SDK version compatibility:**
```xml
<!-- Ensure you're using a recent stable or experimental version -->
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.8.250507002-experimental" />
```

2. **Verify runtime identifiers are set:**
```xml
<PropertyGroup>
  <RuntimeIdentifiers>win-x64;win-arm64</RuntimeIdentifiers>
  <RuntimeIdentifier Condition="'$(Platform)' == 'x64'">win-x64</RuntimeIdentifier>
  <RuntimeIdentifier Condition="'$(Platform)' == 'ARM64'">win-arm64</RuntimeIdentifier>
</PropertyGroup>
```

3. **Clean NuGet cache if issues persist:**
```powershell
dotnet nuget locals all --clear
dotnet restore
dotnet build -c Release
```

**Verification:**
```powershell
# For self-contained: Check WinAppSDK DLLs in output
Get-ChildItem "bin\Release\net8.0-windows\win-x64" -Filter "*Microsoft.Windows*" -Recurse

# For framework-dependent: Check app references SDK package
dotnet list package | Select-String "WindowsAppSDK"
```

---

### Scenario 3: Missing WCR Assemblies at Runtime

**Root Cause:** Even if build succeeds, runtime may fail to find WCR assemblies if deployment is misconfigured.

**Fix:** Ensure proper deployment configuration

1. **For MSIX apps - verify framework dependencies in manifest:**
```xml
<!-- Package.appxmanifest should reference WindowsAppRuntime -->
<Dependencies>
  <PackageDependency Name="Microsoft.WindowsAppRuntime.1.8" 
    MinVersion="8000.xxx.xxx.0" 
    Publisher="CN=Microsoft Corporation, O=Microsoft Corporation, L=Redmond, S=Washington, C=US" />
</Dependencies>
```

2. **For self-contained - verify DLLs are deployed:**
```powershell
# These should exist in your output folder:
Get-ChildItem bin\Release\net8.0-windows\win-x64 -Filter "Microsoft.Windows.AI.*.dll"
Get-ChildItem bin\Release\net8.0-windows\win-x64 -Filter "Microsoft.Graphics.*.dll"
```

3. **Add explicit runtime configuration if needed:**
```xml
<!-- In .csproj -->
<ItemGroup>
  <RuntimeHostConfigurationOption Include="System.Runtime.Loader.UseRidGraph" Value="true" />
</ItemGroup>
```

---

## Additional Context

### WindowsAppSDK Package Structure

The SDK has split into multiple packages:
- **Microsoft.WindowsAppSDK** - Main metapackage (includes everything)
- **Microsoft.WindowsAppSDK.WinUI** - Just WinUI 3
- **Microsoft.WindowsAppSDK.AI** - WCR AI APIs
- **Microsoft.WindowsAppSDK.Foundation** - Core runtime

**Recommendation:** Use the metapackage unless you have specific size constraints.

### Self-Contained vs Framework-Dependent

| Deployment Mode | MSIX Packaged | Unpackaged | WCR Support |
|-----------------|---------------|------------|-------------|
| Framework-Dependent | ✅ Recommended | ⚠️ Requires Bootstrap | ❌ No (needs MSIX) |
| Self-Contained | ⚠️ Larger package | ✅ Works | ❌ No (needs MSIX) |

**Key Point:** WCR APIs require MSIX regardless of self-contained setting.

### Version Alignment

Always use matching versions across packages:
```xml
<!-- GOOD: All 1.8-exp -->
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.8.250507002-experimental" />

<!-- BAD: Mismatched versions -->
<!-- <PackageReference Include="Microsoft.WindowsAppSDK.WinUI" Version="1.8.250507002-experimental" /> -->
<!-- <PackageReference Include="Microsoft.WindowsAppSDK.AI" Version="1.7.135-experimental" /> -->
```

---

## References

- [Issue #5439: Build failure with package references](https://github.com/microsoft/WindowsAppSDK/issues/5439)
- [Issue #5280: Self-contained deployment error](https://github.com/microsoft/WindowsAppSDK/issues/5280)
- [Windows App SDK Deployment Guide](https://learn.microsoft.com/windows/apps/windows-app-sdk/deploy-overview)
- [Package Configuration Documentation](https://learn.microsoft.com/windows/apps/windows-app-sdk/package-configuration)

---

**Last Updated:** 2025-12-25  
**Confidence:** 0.87
