# Winshell (WinUI 3 + WebView2 + Vite React)

WinUI 3 host with a WebView2 surface that loads a Vite + React front end. The JS app talks to native WinAppSDK/Windows APIs through a small bridge, including AI-powered helpers.

## Prerequisites

- Windows 11 with Edge WebView2 runtime installed
- Visual Studio 2022 (or Build Tools) with MSBuild and WinAppSDK workloads
- .NET SDK 9.0.306 (per `global.json`)
- Node.js 18+ and npm

## Layout

```
repo-root/
├─ native/                  # WinUI 3 host
│  └─ Winshell/
│     ├─ App.xaml
│     ├─ MainWindow.xaml
│     ├─ Bridge/            # JS ↔ Native bridge
│     └─ Winshell.csproj
│
├─ webui/                   # Vite + React front end
│  ├─ index.html
│  ├─ package.json
│  ├─ src/
│  │   ├─ bridge/
│  │   ├─ pages/
│  │   ├─ components/
│  │   ├─ hooks/
│  │   └─ styles/
│  └─ vite.config.ts        # outputs to native/Winshell/Web
│
├─ scripts/
│  ├─ build.ps1             # build native via VS MSBuild
│  ├─ run.ps1               # build + run native exe
│  └─ start-dev.ps1         # start/reuse Vite dev server + native (HMR)
│
└─ docs/
  ├─ bridge-protocol.md
  └─ ai-vibe-playbook.md
```

## Dev loop (hot reload)

```pwsh
pwsh .\scripts\start-dev.ps1 -Configuration Debug -Platform x64 -Port 5173
```

- Installs `webui` deps if missing, then starts or reuses the Vite dev server at the chosen port.
- Sets `WINSHELL_DEV_URL` so WebView2 navigates to the dev server, builds, and launches the WinUI host.
- If the script started Vite, it stops it on exit. Use `-Platform ARM64` if you are building on ARM hardware.

## Build + run (packaged)

```pwsh
# 1) Build front-end assets into native/Winshell/Web
cd .\webui
npm install
npm run build
cd ..

# 2) Build native host
pwsh .\scripts\build.ps1 -Configuration Release -Platform x64

# 3) Run the packaged app
pwsh .\scripts\run.ps1 -Configuration Release -Platform x64
# Executable lives at x64\Release\Winshell.exe (or ARM64\Release on ARM)
```

## Bridge protocol

- Doc: `docs/bridge-protocol.md`
- JS usage (in the Vite app):

```ts
import { invoke } from "./bridge/native";

const info = await invoke("app.getInfo");
```

Built-in methods today include `app.getInfo`, `clipboard.getText`, `clipboard.setText`, and `ai.echo`.

## AI / vibe coding

- Playbook (AI-assisted steps, logging, E2E flow): `docs/ai-vibe-playbook.md`
- Quick commands:
  - One-shot build: `pwsh .\scripts\build-all.ps1 -Configuration Debug -Platform ARM64` (or x64; `-SkipWebBuild` to skip web).
  - E2E helper (auto EdgeDriver + envs): `pwsh .\scripts\test-e2e.ps1 -Configuration Debug -Platform ARM64`.
  - Dev HMR: `pwsh .\scripts\start-dev.ps1 -Configuration Debug -Platform x64 -Port 5173` (use ARM64 on ARM).
