---
id: wcr_build_failure_package_references
title: "Build failure with duplicate publish output files when using WinUI and AI packages"
area: DeveloperTools
symptoms:
  - "Found multiple publish output files with the same relative path"
  - "Build error with Microsoft.WindowsAppSDK.WinUI and Microsoft.WindowsAppSDK.AI packages"
  - "Duplicate .winmd files in publish output"
errorCodes:
  - "Multiple publish output files"
keywords:
  - "NuGet"
  - "package reference"
  - "WinUI"
  - "AI"
  - "duplicate"
  - "winmd"
  - "build error"
  - "Microsoft.WindowsAppSDK.WinUI"
  - "Microsoft.WindowsAppSDK.AI"
appliesTo:
  windows: ">=10.0.19041"
  winappsdk: "1.8.250515001-experimental2"
projectType: "packaged"
severity: "common"
lastVerified: "2025-12-23"
references:
  - "https://github.com/microsoft/WindowsAppSDK/issues/5439"
---

# Problem

When referencing both `Microsoft.WindowsAppSDK.WinUI` and `Microsoft.WindowsAppSDK.AI` NuGet packages without the main metapackage, the build fails with an error about duplicate publish output files. The error message lists multiple `.winmd` files (Microsoft.Graphics.Imaging.winmd, Microsoft.Windows.AI.*.winmd, etc.) appearing in both metadata and runtimes-framework folders.

# Quick Diagnosis

1. Check your .csproj file for PackageReferences
2. Verify you have both `Microsoft.WindowsAppSDK.WinUI` and `Microsoft.WindowsAppSDK.AI` referenced
3. Check if you're missing the main `Microsoft.WindowsAppSDK` metapackage
4. Look for build error mentioning "Found multiple publish output files with the same relative path"

# Root Cause

The `Microsoft.WindowsAppSDK.AI` and `Microsoft.WindowsAppSDK.WinUI` packages both contain shared Windows Runtime metadata files (.winmd). When used together without the main `Microsoft.WindowsAppSDK` metapackage, the build system encounters duplicate files during publish operation because both packages include copies of the same WinRT components.

The main `Microsoft.WindowsAppSDK` metapackage (or the `Microsoft.WindowsAppSDK.Packages` package) contains dependency resolution logic that properly handles these shared components and prevents duplication.

# Fix / Workaround

**Add the Microsoft.WindowsAppSDK or Microsoft.WindowsAppSDK.Packages NuGet package:**

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.8.250515001-experimental2" />
  <PackageReference Include="Microsoft.WindowsAppSDK.WinUI" Version="1.8.250507002-experimental" />
  <PackageReference Include="Microsoft.WindowsAppSDK.AI" Version="1.8.135-experimental" />
</ItemGroup>
```

**OR use just the metapackage (simplest approach):**

```xml
<ItemGroup>
  <!-- The metapackage includes both WinUI and AI components -->
  <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.8.250515001-experimental2" />
</ItemGroup>
```

**Alternative - Add Packages package explicitly:**

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.WindowsAppSDK.WinUI" Version="1.8.250507002-experimental" />
  <PackageReference Include="Microsoft.WindowsAppSDK.AI" Version="1.8.135-experimental" />
  <PackageReference Include="Microsoft.WindowsAppSDK.Packages" Version="1.8.250515001-experimental2" />
</ItemGroup>
```

# Verification

1. Add one of the PackageReference configurations shown above
2. Clean solution: `Build > Clean Solution`
3. Rebuild solution: `Build > Rebuild Solution`
4. Verify build succeeds without duplicate file errors
5. Deploy and test your application

# Deep Dive

## Package Architecture

Windows App SDK uses a modular package structure:

- **Microsoft.WindowsAppSDK** (metapackage): References all component packages
- **Microsoft.WindowsAppSDK.WinUI**: WinUI 3 UI framework components
- **Microsoft.WindowsAppSDK.AI**: Windows Copilot Runtime AI APIs
- **Microsoft.WindowsAppSDK.Packages**: Runtime binaries and deployment components
- Other component packages (Foundation, etc.)

## Why Duplication Occurs

The error occurs because:

1. **Microsoft.WindowsAppSDK.AI** package includes:
   - `metadata/Microsoft.Windows.AI.*.winmd`
   - `runtimes-framework/win-{arch}/native/Microsoft.Windows.AI.*.winmd`

2. **Microsoft.WindowsAppSDK.WinUI** package may include references to shared components

3. Without proper dependency resolution from the metapackage or Packages package, MSBuild sees both copies and fails

## Duplicate Files Listed

Common duplicates in the error:
- Microsoft.Graphics.Imaging.winmd
- Microsoft.Windows.AI.ContentSafety.winmd
- Microsoft.Windows.AI.Foundation.winmd
- Microsoft.Windows.AI.Imaging.winmd
- Microsoft.Windows.AI.Text.winmd
- Microsoft.Windows.AI.winmd
- Microsoft.Windows.SemanticSearch.winmd
- Microsoft.Windows.Vision.winmd
- Microsoft.Windows.Workloads.winmd

## Best Practices

**Recommended approach:**

Just use the main metapackage unless you have specific size optimization requirements:

```xml
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.8.250515001-experimental2" />
```

This automatically includes:
- ✅ WinUI 3 components
- ✅ Windows Copilot Runtime APIs
- ✅ All other Windows App SDK features
- ✅ Proper dependency resolution

**Advanced scenario (granular control):**

If you need to reference packages individually (for app size optimization or partial feature usage), always include the Packages package:

```xml
<PackageReference Include="Microsoft.WindowsAppSDK.WinUI" Version="..." />
<PackageReference Include="Microsoft.WindowsAppSDK.AI" Version="..." />
<PackageReference Include="Microsoft.WindowsAppSDK.Packages" Version="..." />
```

## Impact

This is a **build-time issue** that completely blocks compilation. It affects:
- New projects using experimental AI features with WinUI
- Developers trying to optimize package references
- Migration scenarios from older SDK versions

## Resolution Timeline

- **Reported:** May 19, 2025 (Issue #5439)
- **Status:** Closed as documented behavior on July 9, 2025
- **Workaround:** Add Packages reference (as described above)

# References

- [GitHub Issue #5439](https://github.com/microsoft/WindowsAppSDK/issues/5439)
- [Windows App SDK Packages Documentation](https://learn.microsoft.com/windows/apps/windows-app-sdk/downloads)
- [NuGet Package Reference](https://learn.microsoft.com/nuget/consume-packages/package-references-in-project-files)
