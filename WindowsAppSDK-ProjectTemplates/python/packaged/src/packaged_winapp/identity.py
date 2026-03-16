"""
Windows App Package Identity Information Tool

Displays comprehensive package identity information for the current process.
Useful for debugging packaged applications.
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


def has_package_identity():
    """Check if the current process has package identity."""
    return get_package_full_name() is not None


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
        print("Package Identity: DETECTED ✓")
        print(f"  Package Full Name: {package_full_name}")
        print(f"  Package Family Name: {package_family_name}")
        print(f"  Package Path: {package_path}")
        print()
        print("This process is running with MSIX package identity.")
        print("Windows App SDK features are available.")
    else:
        print("Package Identity: NOT DETECTED ✗")
        print()
        print("This process is running without package identity (unpackaged).")
        print("Some Windows App SDK features may not be available.")
    
    print()
    print("=" * 70)
