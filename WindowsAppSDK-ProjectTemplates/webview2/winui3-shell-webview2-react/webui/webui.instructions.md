# webui.instructions

Purpose
- Vite + React frontend served by Vite dev server; outputs built assets to ../native/Winshell/Web.

Daily dev (hot reload)
- pwsh ../scripts/start-dev.ps1 -Configuration Debug -Platform x64 -Port 5173
  - Installs deps if missing, starts/reuses Vite, sets WINSHELL_DEV_URL, launches host.
  - Do not run npm run build during hot reload. Use -Platform ARM64 on ARM.

Structure for testability
- Keep bridge/data access in thin modules (e.g., src/bridge/**, src/data/<feature>/...) so components stay lean and mockable; organize data by feature/domain, not by pages.
- Export pure helpers for transforms (date math, storage serialization) and reuse them in components.
- In tests, mock the bridge/data modules (vi.mock) rather than UI components.

Bridge usage and mocking
- Real calls: src/bridge/native.ts. Mock that module while native/bridge is pending; switch back before packaged builds.
- For UI work without native changes, keep mocks in a separate module and toggle via vi.mock in tests.

Logging
- useLogger/app.log sends structured messages to native logging when inside WebView; falls back to console otherwise.

Tests (unit)
- Add/extend tests when changing logic under src/data/** or src/bridge/** (storage, date math, bridge adapters, derived calculations). Styling-only changes typically do not need tests.
- Run scoped tests: npm run test -- path/to/file (or npx vitest run path/to/file).
- Run full suite: npm run test.
- Environment: vitest + jsdom; use vi.mock/vi.spyOn for bridge/native calls or localStorage/fetch stubs.

When to build web assets
- Only for packaged exe, CI, or embedding updated web assets: npm run build.
