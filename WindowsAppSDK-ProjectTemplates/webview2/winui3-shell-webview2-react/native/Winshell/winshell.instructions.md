# winshell.instructions

Purpose
- WinUI 3 host, bridge handlers, and XAML/C# UI.

When to build
- Rebuild when native code or Bridge API changes, or when embedding fresh web assets.
- Native-only: pwsh ../../scripts/build.ps1 -Configuration Release -Platform x64
- Web + native: pwsh ../../scripts/build-all.ps1 -Configuration Release -Platform x64

Bridge API changes
- Add constant in Bridge/BridgeMethods.cs, handler under Bridge/Handlers, register in BridgeRouter.
- Frontend can mock bridge calls; after native is updated, rebuild to integrate.

Debug/logging
- Serilog logs: ../../x64/Debug/logs or ../../ARM64/Debug/logs.
- Attach Visual Studio to the packaged exe for native debugging.

Packaging/run
- Packaged exe: ../../x64/Release/Winshell.exe (or ARM64/Release on ARM).
- Hot reload uses dev server assets; no native rebuild for web-only edits.
