# Infrastructure Layer - Cross-cutting Concerns

## Purpose
Application infrastructure: logging configuration, DI setup, and cross-cutting concerns.

## Files

### LoggingConfiguration.cs
Configures logging with multiple destinations:

**Destinations:**
- **File**: All log levels, 4KB rotation, daily rolling
- **Windows Event Log**: Warning and above only

**File Locations:**
- **Debug**: `{ProjectFolder}/logs/app-{date}.log`
- **Release**: `%LocalAppData%/BlankApp/logs/app-{date}.log`

## Code Samples
- Logging configuration: [`LoggingConfiguration.cs`](LoggingConfiguration.cs) sets up Serilog file logging (4KB roll) and Windows Event Log.

## App Startup Example
- Wire global exception logging in `App.xaml.cs` using `services.AddAppLogging()` and hook:
	- `App.UnhandledException`
	- `TaskScheduler.UnobservedTaskException`
	- `AppDomain.CurrentDomain.UnhandledException`
	to log critical failures.

## What Belongs Here
- Logging configuration and providers
- Dependency injection composition/root setup helpers
- Cross-cutting concerns (telemetry, configuration bootstrapping, feature flags)

## Usage
- Register logging via `services.AddAppLogging()` in your app startup.
- Keep cross-cutting helpers in this folder to avoid polluting feature layers.

## Logging Best Practices
- Log entry/exit with duration and errors.
- Use file + event log sinks as configured in logging setup.

## Log Levels

| Level | Use For | Goes To |
|-------|---------|---------|
| Trace | Method entry/exit | File only |
| Debug | Detailed diagnostics | File only |
| Information | Normal operations | File only |
| Warning | Potential issues | File + Event Log |
| Error | Failures | File + Event Log |
| Critical | App-breaking issues | File + Event Log |

## Packages Used

- `Microsoft.Extensions.Logging` - Core abstraction (Microsoft)
- `Microsoft.Extensions.Logging.EventLog` - Windows Event Log (Microsoft)
- `Serilog.Extensions.Logging` - Integrates Serilog with MS Logging
- `Serilog.Sinks.File` - File logging with rotation
