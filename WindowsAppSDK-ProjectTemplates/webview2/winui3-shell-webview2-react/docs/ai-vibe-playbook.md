# AI / Vibe Coding Playbook

Quick reference for AI-assisted development: bring up the front end + native bridge, log to pinpoint issues, and validate with E2E.

## AI Agent Task Loop: Follow These Steps for Every Feature
**Instructions for AI coding assistants: Execute these steps in order for all feature implementations.**

- Implement the requested change first.
- Add focused debug logs (front end via `useLogger`/`app.log`, native via Serilog) around the new logic.
 - **Build everything**: `pwsh .\scripts\build-all.ps1 -Configuration Debug -Platform arm64` (or your arch). 
  - This runs `npm run build` (frontend) + compiles native code (embeds frontend into exe).
  - **Critical**: Changes to frontend code require BOTH `npm run build` AND native compilation to take effect in the exe.
  - Fix any build errors before proceeding.
- Add/adjust E2E coverage for the new behavior in `tests/Winshell.E2E.WebDriver/BasicE2ETests.cs`.
 - Run the full E2E helper: `pwsh .\scripts\test-e2e.ps1 -Configuration Debug -Platform arm64`.
  - This automatically builds the project, downloads EdgeDriver, sets environment variables, and runs tests.
  - Tests use the compiled exe, not the dev server.
 - Review test output in console and native logs in `arm64/Debug/logs/` (or `x64/Debug/logs/`); iterate until green.
- Ship the feature.

**Key insight**: The exe embeds the frontend assets. Running only `npm run build` updates files in `native/Winshell/Web/` but the exe won't use them until you recompile with `build.ps1` or `build-all.ps1`.

## Dev workflow
 - **Hot reload (recommended for development)**: `pwsh .\scripts\start-dev.ps1 -Configuration Debug -Platform arm64` (use `-Platform x64` on x64).
  - Starts Vite dev server on port 5173 with hot module replacement.
  - Sets `WINSHELL_DEV_URL` environment variable and launches the native exe.
  - Frontend changes reload instantly without recompiling native code.
  - Press Ctrl+C to stop both dev server and app.
 - **Full build (for testing/release)**: `pwsh .\scripts\build-all.ps1 -Configuration Debug -Platform arm64` (or `Release`/`x64`).
  - Runs `npm install` (if needed) → `npm run build` → compiles native project.
  - Frontend assets are embedded into the exe in `arm64/Debug/` or `x64/Debug/`.
  - Use `-SkipWebBuild` to skip the web build step if frontend hasn't changed.
 - **Native-only build**: `pwsh .\scripts\build.ps1 -Configuration Debug -Platform arm64`.
  - Only compiles native C# code, assumes `native/Winshell/Web/` already has built frontend assets.
 - **Run without building**: `pwsh .\scripts\run.ps1 -Configuration Debug -Platform arm64`.
  - Launches the exe without building anything.
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
- **Important**: E2E tests run against the compiled exe (not the dev server), so changes must be built with `build-all.ps1` first.
- Prereqs: 
  - Build the matching Debug package first (`x64/Debug` or `arm64/Debug`).
  - Optionally set `TEST_PLATFORM=arm64` to force architecture.
  - Set `MSEDGEDRIVER_DIR` if EdgeDriver isn't on PATH (script auto-downloads if needed).
- **Recommended**: Use the helper script which handles building and environment setup:
  ```pwsh
  pwsh ./scripts/test-e2e.ps1 -Configuration Debug -Platform arm64
  ```
  This automatically:
  1. Compiles the test project
  2. Downloads EdgeDriver if missing
  3. Sets environment variables (`TEST_PLATFORM`, `MSEDGEDRIVER_DIR`)
  4. Runs all tests
- Manual run (if needed):
  ```pwsh
  pwsh -c "dotnet test tests/Winshell.E2E.WebDriver/Winshell.E2E.WebDriver.csproj -c Debug"
  ```
- Writing new test cases:
  1) Add test methods to `BasicE2ETests.cs` (or create new test classes).
  2) Reuse the existing `ClassInitialize` to start the app and WebDriver.
  3) Locate UI elements via XPath/CSS selectors (e.g., `By.XPath("//button[contains(text(),'Submit')]")`).
  4) Use explicit waits (`WebDriverWait`) to ensure UI is ready before assertions.
  5) Test both happy paths and error scenarios.
  6) Clean up resources in `finally` blocks if needed.
- Debugging failed tests:
  - Check test output for exception stack traces and assertion messages.
  - Review native logs in `arm64/Debug/logs/` or `x64/Debug/logs/` for backend errors.
  - Use `Console.WriteLine()` in tests to output debug information.
  - Increase timeout values if operations take longer than expected.

## Front-end responsibilities
- Keep UI state/validation in the front end; route cross-process/system capabilities through the bridge.
- Stack: Vite + React; bridge calls are wrapped in `webui/src/bridge/`—avoid touching `window.chrome.webview` directly.
- **Build process**: 
  - `npm run build` outputs to `native/Winshell/Web/` (these are just files on disk).
  - Native compilation embeds these files into the exe as resources.
  - **The exe only sees the files that were present during compilation**, not subsequent `npm run build` outputs.
  - For development with hot reload, use `start-dev.ps1` which uses a dev server instead of embedded assets.

## Common troubleshooting paths
- UI misbehavior: `app.log` the inputs first, then check native logs; if missing, verify method name and bridge registration.
- Native issues: add structured logs inside handlers; use debugger/`Debug.WriteLine` as needed.
- E2E failures: ensure correct-arch build, EdgeDriver availability, and free port; update waits/selectors to match current UI.
 - Identity issues while debugging: if your unpackaged Win32 binary doesn't activate with the sparse package identity during a debug session, build the package identity and register the sparse MSIX pointing `-ExternalLocation` at your debug output folder (for example `x64/Debug` or `arm64/Debug`). This forces Windows to associate the package identity with the exact debug binaries. See `PackageIdentity/readme.md` for step-by-step commands.