# scripts.instructions

Entry points (run from repo root)
- start-dev.ps1 — hot reload (Vite + host). Example: pwsh .\scripts\start-dev.ps1 -Configuration Debug -Platform x64 -Port 5173
- build-all.ps1 — web build + native build (embeds web assets). Example: pwsh .\scripts\build-all.ps1 -Configuration Release -Platform x64
- build.ps1 — native-only (assumes web assets already built). Example: pwsh .\scripts\build.ps1 -Configuration Release -Platform x64
- run.ps1 — run packaged app without rebuilding. Example: pwsh .\scripts\run.ps1 -Configuration Release -Platform x64
- test-e2e.ps1 — build prereqs and run E2E. Example: pwsh .\scripts\test-e2e.ps1 -Configuration Debug -Platform x64

Usage notes
- Use -Platform ARM64 on ARM devices.
- build-all.ps1 runs npm install if needed; add -SkipWebBuild when web assets are unchanged.
- Daily dev: prefer start-dev.ps1 (it builds and runs the host + dev server).
- Packaged/release/E2E payloads: use build-all.ps1; avoid for routine UI iteration.
