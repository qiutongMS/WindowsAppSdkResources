# AI / Vibe Coding Playbook

Quick reference for AI-assisted development: bring up the front end + native bridge, log to pinpoint issues, and validate with E2E.

## Task loop (when AI is coding)
- Implement the requested change first.
- Add focused debug logs (front end via `useLogger`/`app.log`, native via Serilog) around the new logic.
- Build end-to-end: `pwsh .\scripts\build-all.ps1 -Configuration Debug -Platform ARM64` (or your arch). Fix any build errors.
- Add/adjust E2E coverage for the new behavior.
- Run the full E2E helper: `pwsh .\scripts\test-e2e.ps1 -Configuration Debug -Platform ARM64` (auto-downloads EdgeDriver, sets envs, runs tests).
- Review test output and native logs; iterate until green.
- Ship the feature.

## Dev workflow
- Hot reload: `pwsh .\scripts\start-dev.ps1 -Configuration Debug -Platform x64 -Port 5173` (use `-Platform ARM64` on ARM). The script sets `WINSHELL_DEV_URL` and launches the native host.
- One-shot build: `pwsh .\scripts\build-all.ps1 -Configuration Debug -Platform ARM64` (or Release/x64). Add `-SkipWebBuild` to skip the web build.
- Native-only run: `pwsh .\scripts\run.ps1 -Configuration Debug -Platform ARM64` (assumes web assets already built into `native/Winshell/Web`).
- Repo overview and quick commands: see `README.md` (AI/vibe section links to this playbook).

## Bridge usage and extension
- JS calls use `invoke` (`webui/src/bridge/native.ts`):
  ```ts
  import { invoke } from "../bridge/native";
  const result = await invoke("ai.echo", { text: "hi" });
  ```
- Current method names live in `native/Winshell/Bridge/BridgeMethods.cs` (e.g., `app.getInfo`, `ai.echo`, `ai.removeBackground`, `app.log`).
- To add a native capability:
  1) Add a constant in `BridgeMethods`;
  2) Implement a handler in `native/Winshell/Bridge/Handlers`;
  3) Register it in `BridgeRouter`;
  4) Call from the front end via `invoke("your.method", params)`.

## Logging and triage
- JS -> native log: use the hook-based logger (`useLogger`) which wraps `app.log`, so messages go to both the console and Serilog.
  ```ts
  import { useLogger } from "../hooks/useLogger";
  const log = useLogger("background");
  log.info("clicked", { button: "submit" });
  log.error("upload failed", { code: 500 });
  ```
  Under the hood it calls `app.log` (`level: info|warn|error`; `meta` serialized as JSON) only when running inside WebView.
- Native logging: use `Serilog.Log.ForContext<T>()` or existing loggers. Prefer structured fields (`Log.Information("AI call {Method} status={Status}", method, status);`) and avoid dumping large objects.
- WebView2 debugging: set `WEBVIEW2_REMOTE_DEBUGGING_PORT=9222` before launch and attach via Edge DevTools.

## E2E tests (WebDriver)
- Project: `tests/Winshell.E2E.WebDriver` (MSTest + Edge WebDriver using WebView2 remote debugging).
- Prereqs: build the matching Debug package first (`x64/Debug` or `ARM64/Debug`); optionally set `TEST_PLATFORM=ARM64` to force architecture; set `MSEDGEDRIVER_DIR` if EdgeDriver isn’t on PATH.
- Run example:
  ```pwsh
  pwsh -c "dotnet test tests/Winshell.E2E.WebDriver/Winshell.E2E.WebDriver.csproj -c Debug"
  ```
- Shortcut (auto driver download + envs):
  ```pwsh
  pwsh ./scripts/test-e2e.ps1 -Configuration Debug -Platform ARM64
  ```
- Writing new cases:
  1) Reuse the existing `ClassInitialize` to start the app and driver;
  2) Locate UI elements via XPath/CSS and trigger bridge calls;
  3) Use explicit waits to assert UI/results (see `BasicE2ETests.cs`).

## Front-end responsibilities
- Keep UI state/validation in the front end; route cross-process/system capabilities through the bridge.
- Stack: Vite + React; bridge calls are wrapped in `webui/src/bridge/`—avoid touching `window.chrome.webview` directly.
- Build: `npm run build` outputs to `native/Winshell/Web`, then the native host serves it.

## Common troubleshooting paths
- UI misbehavior: `app.log` the inputs first, then check native logs; if missing, verify method name and bridge registration.
- Native issues: add structured logs inside handlers; use debugger/`Debug.WriteLine` as needed.
- E2E failures: ensure correct-arch build, EdgeDriver availability, and free port; update waits/selectors to match current UI.