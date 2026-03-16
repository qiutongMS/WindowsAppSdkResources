# Python Windows App SDK Samples

This directory contains samples demonstrating how to use the Windows App SDK with Python.

> **Note**: Currently, these samples primarily support the framework deployment model.

## Prerequisites

- [uv](https://github.com/astral-sh/uv): An extremely fast Python package installer and resolver.

## Setup

Before running any samples, you must download the necessary projection wheels from [working in progress projection releases](https://github.com/Hong-Xiang/PyWinAppSDK/releases), as the Python packages are not yet published to PyPI.

A script is provided to automate this process:

1. Open a PowerShell terminal in this directory.
2. Run the download script:
   ```powershell
   .\scripts\download-wheels.ps1
   ```
   This will download the latest wheels to the `wheels` folder. The samples are configured to use this folder as a local package source.

## Samples

- **[Unpackaged](unpackaged/README.md)**: A standard Python script using Windows App SDK APIs.
- **[Packaged](packaged/README.md)**: A fully packaged MSIX application.
- **[Sparse](sparse/README.md)**: A Python application using a sparse package (Package with External Location) for identity.
