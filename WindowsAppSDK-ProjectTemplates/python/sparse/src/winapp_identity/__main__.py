"""
Windows App Package Identity Information Tool

Displays comprehensive package identity information for the current process.
Useful for debugging sparse packages and MSIX applications.
"""
import ctypes
import ctypes.wintypes
import sys
import os


APPMODEL_ERROR_NO_PACKAGE = 15700
ERROR_INSUFFICIENT_BUFFER = 122


def get_package_full_name():
    """Return the package full name if this process has identity, else None."""
    kernel32 = ctypes.windll.kernel32
    length = ctypes.wintypes.UINT(0)
    hr = kernel32.GetCurrentPackageFullName(ctypes.byref(length), None)

    if hr == APPMODEL_ERROR_NO_PACKAGE:
        return None
    if hr not in (0, ERROR_INSUFFICIENT_BUFFER):
        return None

    buffer = ctypes.create_unicode_buffer(length.value)
    hr = kernel32.GetCurrentPackageFullName(ctypes.byref(length), buffer)
    if hr == APPMODEL_ERROR_NO_PACKAGE:
        return None
    if hr != 0:
        return None

    return buffer.value


def get_package_family_name():
    """Return the package family name if this process has identity, else None."""
    kernel32 = ctypes.windll.kernel32
    length = ctypes.wintypes.UINT(0)
    hr = kernel32.GetCurrentPackageFamilyName(ctypes.byref(length), None)

    if hr == APPMODEL_ERROR_NO_PACKAGE:
        return None
    if hr not in (0, ERROR_INSUFFICIENT_BUFFER):
        return None

    buffer = ctypes.create_unicode_buffer(length.value)
    hr = kernel32.GetCurrentPackageFamilyName(ctypes.byref(length), buffer)
    if hr == APPMODEL_ERROR_NO_PACKAGE:
        return None
    if hr != 0:
        return None

    return buffer.value


def get_package_path():
    """Return the package installation path if this process has identity, else None."""
    kernel32 = ctypes.windll.kernel32
    length = ctypes.wintypes.UINT(0)
    hr = kernel32.GetCurrentPackagePath(ctypes.byref(length), None)

    if hr == APPMODEL_ERROR_NO_PACKAGE:
        return None
    if hr not in (0, ERROR_INSUFFICIENT_BUFFER):
        return None

    buffer = ctypes.create_unicode_buffer(length.value)
    hr = kernel32.GetCurrentPackagePath(ctypes.byref(length), buffer)
    if hr == APPMODEL_ERROR_NO_PACKAGE:
        return None
    if hr != 0:
        return None

    return buffer.value


def print_identity_info():
    """Print comprehensive package identity information."""
    print("=" * 70)
    print("Windows App Package Identity Information")
    print("=" * 70)
    print()
    
    print(f"Process Information:")
    print(f"  Executable: {sys.executable}")
    print(f"  PID: {os.getpid()}")
    print(f"  Working Directory: {os.getcwd()}")
    print()

    package_full_name = get_package_full_name()
    package_family_name = get_package_family_name()
    package_path = get_package_path()

    if package_full_name:
        print("Package Identity: DETECTED")
        print(f"  Package Full Name: {package_full_name}")
        print(f"  Package Family Name: {package_family_name}")
        print(f"  Package Path: {package_path}")
        print()
        print("This process is running with MSIX/Sparse package identity.")
        print("The Windows App SDK runtime is available via the package.")
    else:
        print("Package Identity: NOT DETECTED")
        print()
        print("This process is running without package identity (unpackaged).")
        print("To use Windows App SDK features like AppNotifications in sparse mode:")
        print("  1. Ensure appxmanifest.xml is properly configured")
        print("  2. Register the package: winapp create-debug-identity <python.exe>")
        print("  3. Run with the same interpreter specified in the manifest")
    
    print()
    print("=" * 70)


def main():
    print_identity_info()


if __name__ == "__main__":
    main()
