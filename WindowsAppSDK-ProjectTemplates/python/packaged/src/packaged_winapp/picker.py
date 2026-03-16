"""
Windows App SDK File Picker Integration

Provides file and folder picking functionality using Windows App SDK.
"""

import ctypes
import asyncio
from winappsdk.microsoft.ui import WindowId
from winappsdk.microsoft.windows.storage.pickers import FileOpenPicker, FolderPicker


async def pick_single_file():
    """Open a file picker dialog and let user select a single file."""
    # Get the current window handle (console window)
    hwnd = ctypes.windll.kernel32.GetConsoleWindow()

    # Create a WindowId from the HWND
    window_id = WindowId(hwnd)

    # Create the file picker with the window ID
    picker = FileOpenPicker(window_id)

    # Set file type filters
    picker.file_type_filter.append("*")  # Allow all file types

    # Show the picker and get the selected file result
    result = await picker.pick_single_file_async()

    if result and result.path:
        print(f"✓ Selected file: {result.path}")
        return result
    else:
        print("✗ No file was selected.")
        return None


async def pick_multiple_files():
    """Open a file picker dialog and let user select multiple files."""
    # Get the current window handle (console window)
    hwnd = ctypes.windll.kernel32.GetConsoleWindow()

    # Create a WindowId from the HWND
    window_id = WindowId(hwnd)

    # Create the file picker with the window ID
    picker = FileOpenPicker(window_id)

    # Set file type filters
    picker.file_type_filter.append("*")  # Allow all file types

    # Show the picker and get the selected files
    results = await picker.pick_multiple_files_async()

    if results and len(results) > 0:
        print(f"✓ Selected {len(results)} file(s):")
        for file in results:
            print(f"  - {file.path}")
        return results
    else:
        print("✗ No files were selected.")
        return None


async def pick_folder():
    """Open a folder picker dialog and let user select a folder."""
    # Get the current window handle (console window)
    hwnd = ctypes.windll.kernel32.GetConsoleWindow()
    # Create a WindowId from the HWND
    window_id = WindowId(hwnd)

    # Create the folder picker with the window ID
    picker = FolderPicker(window_id)

    # Show the picker and get the selected folder
    result = await picker.pick_single_folder_async()

    if result and result.path:
        print(f"✓ Selected folder: {result.path}")
        return result
    else:
        print("✗ No folder was selected.")
        return None


# Note: Use these async functions directly with await in an async context
# Do not wrap with asyncio.run() multiple times in a loop
