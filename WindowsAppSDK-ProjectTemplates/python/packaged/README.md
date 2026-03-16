# Packaged Python WinUI 3 Sample

This sample demonstrates how to create a fully packaged MSIX application using Python and the Windows App SDK.

## Prerequisites

- Ensure you have completed the setup steps in the [root README](../README.md).

## Build and Install

To bundle the application, pack it into an MSIX, and install it, run the following script:

```powershell
.\scripts\bundle_and_depoly.ps1
```

This script performs the following:
1. Cleans previous builds.
2. Bundles the Python environment and application.
3. Packs the application into `packaged-winapp.msix`.
4. Installs the package using `Add-AppxPackage`.

## Running the App

Once installed, you can find the application in your Start Menu (look for "Packaged WinApp").