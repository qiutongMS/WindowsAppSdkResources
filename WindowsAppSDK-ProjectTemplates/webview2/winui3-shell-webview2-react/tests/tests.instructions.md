# tests.instructions

Scope
- E2E harness (Winshell.E2E.WebDriver) runs against the packaged exe.

Prereqs
- Matching packaged build present (x64/Debug or ARM64/Debug).
- Set TEST_PLATFORM if you need ARM64 on x64.

Run E2E (use for end-to-end validation, not every edit)
- pwsh .\scripts\test-e2e.ps1 -Configuration Debug -Platform x64
  - Builds, downloads EdgeDriver if missing, sets env vars, runs MSTest.

Debugging
- Native logs: x64/Debug/logs or ARM64/Debug/logs.
- Adjust waits/selectors in tests/Winshell.E2E.WebDriver/BasicE2ETests.cs as needed.
