import argparse
import sys

from winappsdk.bootstrap import initialize_windows_app_sdk, BootstrapInitializeOptions
from winappsdk.microsoft.windows.appnotifications import AppNotificationManager

from .notifications import show_notification, NOTIFICATION_TYPES


def main():
    parser = argparse.ArgumentParser(
        description="Show Windows App SDK toast notifications",
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    parser.add_argument(
        "type",
        nargs="?",
        default="simple",
        choices=list(NOTIFICATION_TYPES.keys()),
        help="Notification type (default: simple)",
    )
    
    args = parser.parse_args()
    
    try:
        with initialize_windows_app_sdk(options=BootstrapInitializeOptions.ON_ERROR_SHOW_UI):
            AppNotificationManager.default.register()
            show_notification(args.type)
            AppNotificationManager.default.unregister()
            
    except OSError as e:
        if "package identity" in str(e).lower():
            print("Error: No package identity detected.")
            print("Install Python from Microsoft Store or use sparse package registration.")
            sys.exit(1)
        raise
    except Exception as e:
        print(f"Error: {e}")
        sys.exit(1)


if __name__ == "__main__":
    main()
