# Winshell WebView2 WebDriver E2E

This suite drives the real WebView2 inside the native app via Edge WebDriver.

## Prereqs
 - Build app: `pwsh ../scripts/build.ps1 -Configuration Debug -Platform x64` or `-Platform arm64` (tests will pick x64 first; set `TEST_PLATFORM=arm64` to force arm64).
- Edge WebDriver: place `msedgedriver.exe` in PATH, or set `MSEDGEDRIVER_PATH`/`MSEDGEDRIVER_DIR` to the downloaded location. Version must match your Edge/WebView2 runtime. You can fetch it via `pwsh ../scripts/get-edgedriver.ps1 -Channel Stable -Architecture ARM64 -OutputDir C:\Users\leilzh\Downloads\edgedriver_arm64` (swap `Architecture`/`OutputDir` as needed).

## How it works
- App is launched with `WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS=--remote-debugging-port=9222` to expose CDP.
- EdgeDriver attaches to the running WebView2 using `DebuggerAddress` and `webview2` option.
- Tests post BridgeProtocol messages (`app.getInfo`) via `chrome.webview.postMessage` and assert responses.

## Run
```
# optional: set platform/driver explicitly
set TEST_PLATFORM=ARM64
set MSEDGEDRIVER_DIR=C:\Users\leilzh\Downloads\edgedriver_arm64

dotnet test ..\..\winshell.slnx -c Debug -p:Platform=x64 --filter TestCategory!=Skip
```
(MSTest runner; ensure driver is locatable via PATH or env.)

### Shortcut
From repo root:
```pwsh
pwsh ./scripts/test-e2e.ps1 -Configuration Debug -Platform ARM64
```
- Checks for EdgeDriver under `tests/Winshell.E2E.WebDriver/edgedriver_<arch>`; downloads if missing via `get-edgedriver.ps1`.
- Sets `TEST_PLATFORM` and `MSEDGEDRIVER_DIR` automatically, then runs the E2E suite.
