# Unpackaged Python WinUI 3 Sample

This sample demonstrates a standard unpackaged Python application using Windows App SDK APIs, specifically a File Picker.

## Prerequisites

- Ensure you have completed the setup steps in the [root README](../README.md).

## Setup

Prepare the development environment:

```powershell
uv sync
```

## Running the Sample

Run the file picker sample using the defined entry point:

```powershell
uv run winapp-pick
```

Alternatively, run it as a module:

```powershell
uv run python -m winapp_pick
```

> **Note**: The project name is `winapp-pick`, but the Python module name is `winapp_pick` (with an underscore) because Python module names cannot contain hyphens.