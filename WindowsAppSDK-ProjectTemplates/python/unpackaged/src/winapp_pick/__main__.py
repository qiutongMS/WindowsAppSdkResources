import ctypes
import asyncio
from winappsdk.bootstrap import initialize_windows_app_sdk, BootstrapInitializeOptions
from winappsdk.microsoft.ui import WindowId
from winappsdk.microsoft.windows.storage.pickers import FileOpenPicker


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
        print(f"Selected file: {result.path}")
        return result
    else:
        print("No file was selected.")
        return None


def main():
    print("Hello from minimum-python-app!!")
    with initialize_windows_app_sdk(
        options=BootstrapInitializeOptions.ON_ERROR_SHOW_UI
    ):
        print("\nOpening file picker...")
        _ = asyncio.run(pick_single_file())


if __name__ == "__main__":
    main()
