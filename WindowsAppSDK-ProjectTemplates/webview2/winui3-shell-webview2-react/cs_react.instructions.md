# Winshell (WinUI 3 + WebView2 + Vite React)

What this is
- WinUI 3 host with a WebView2 surface that loads a Vite + React frontend; bridge connects web to native WinAppSDK APIs.

Start here
- One-time setup: native/Winshell/winshell.instructions.md
- Daily dev (hot reload): webui/webui.instructions.md
- Scripts (build/run/test): scripts/scripts.instructions.md
- Packaged build + E2E: tests/tests.instructions.md

Repo map
- native/Winshell: WinUI host, XAML UI, bridge handlers, logging.
- webui: Vite + React frontend; dev server by default, built assets output to native/Winshell/Web.
- scripts: PowerShell entrypoints for dev, build, run, and E2E helpers.
- tests: WebDriver E2E suite targeting the packaged exe.

Fast paths
- Hot reload (daily dev, builds and runs Winshell host + Vite dev server): pwsh .\scripts\start-dev.ps1 -Configuration Debug -Platform x64 -Port 5173 (ARM → -Platform ARM64)
- Packaged build (release/E2E payload, embeds web assets): pwsh .\scripts\build-all.ps1 -Configuration Release -Platform x64
- E2E (WebDriver against packaged exe; use when validating flows, not every edit): pwsh .\scripts\test-e2e.ps1 -Configuration Debug -Platform x64
