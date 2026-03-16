# Sparse Template Notes

This sparse WinUI 3 template mirrors the packaged template’s layered architecture, samples, and tests but ships **unpackaged** by default. Key points:

- **Unpackaged app**: Uses `BlankApp.dev.manifest.xml` and `PackageIdentity/` helpers for sparse identity. MSIX packaging files are intentionally absent here.
- **Samples toggle**: Build or test with `-p:IncludeSamples=true` to include sample code and sample tests. Default excludes `*.Sample*` from compilation.
- **Tests**: Run from `BlankApp` folder:
  - `dotnet test .\Tests\BlankApp.Tests.csproj -p:Platform=x64` (no samples)
  - `dotnet test .\Tests\BlankApp.Tests.csproj -p:Platform=x64 -p:IncludeSamples=true` (runs sample tests)
- **DI & logging**: Configured in `App.xaml.cs` via `AddAppLogging()` (Serilog file + Event Log) and DI registrations behind `INCLUDE_SAMPLES`.
- **Layout**: Same layered folders and instructions as packaged: Infrastructure, Data, Services, Models, ViewModels, Views, Tests with co-located `*.Test.cs`.

## Sparse identity
- For usage steps (manifest registration or MSIX path), see `BlankApp/App.instructions.md` under “Sparse identity quick start”.
- This template is unpackaged by default; use packaged template if you need full MSIX packaging.
