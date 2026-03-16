# Sparse Package Python Sample

This sample demonstrates how to use a **Sparse Package** (Package with External Location) to grant Package Identity to a Python application. This allows the app to use features require identity, while still running from a local folder to make debug easier.

## Prerequisites

- Ensure you have completed the setup steps in the [root README](../README.md).

## Setup

Sparse packages require a dedicated Python interpreter to properly register the debug identity. Standard `uv` virtual environments often use a global cached interpreter or a launcher, which can conflict with identity registration.

Run the setup script to install a local Python interpreter and configure the environment:

```powershell
.\scripts\setup.ps1
```

This script will:
1. Install a dedicated Python version into `local_python`.
2. Sync dependencies using this local Python.
3. Register the debug identity for this specific Python executable.

> **Note**: Since the `winapp` CLI handles adding the Windows App SDK package dependency to the debug package, manual bootstrapping is not required (and not supported) in this mode.

## Running the Sample

To run the notification demo:

```powershell
uv run python -m winapp_identity
uv run python -m winapp_pick
```
