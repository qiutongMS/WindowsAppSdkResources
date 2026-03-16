"""
Windows App SDK Packaged Application Demo

A REPL-style demo application showcasing various Windows App SDK features:
- Toast Notifications
- File/Folder Pickers
- Package Identity Information

This is a packaged MSIX application with full Windows App SDK capabilities.
"""
import time
import sys
import asyncio

from .identity import (
    has_package_identity,
    print_identity_info,
    get_package_full_name,
)

from .picker import (
    pick_single_file,
    pick_multiple_files,
    pick_folder,
)


def print_menu():
    """Print the interactive menu."""
    print("\n" + "=" * 70)
    print("Windows App SDK Packaged Application - Interactive Demo")
    print("=" * 70)
    print("\nAvailable Commands:")
    print("\n  Package Identity:")
    print("    1 - Show Package Identity Information")
    print("\n  File Pickers:")
    print("    2 - Pick Single File")
    print("    3 - Pick Multiple Files")
    print("    4 - Pick Folder")
    print("\n  Other:")
    print("    h - Show this help menu")
    print("    q - Quit")
    print("=" * 70)


async def run_interactive_mode():
    """Run the interactive REPL mode (async)."""
    print("\n" + "=" * 70)
    print("🚀 Windows App SDK Packaged Application")
    print("=" * 70)
    
    # Check package identity on startup
    if has_package_identity():
        package_name = get_package_full_name()
        print(f"\n✓ Running with MSIX package identity")
        print(f"  Package: {package_name}")
    else:
        print("\n✗ WARNING: No package identity detected!")
        print("  Some features may not work correctly.")
    
    print_menu()
    
    while True:
        try:
            # Use asyncio.to_thread to avoid blocking the event loop
            command = await asyncio.to_thread(input, "\nEnter command: ")
            command = command.strip().lower()
            
            if not command:
                continue
            
            if command == 'q' or command == 'quit' or command == 'exit':
                print("\nGoodbye!")
                break
            
            elif command == 'h' or command == 'help':
                print_menu()
            
            elif command == '1':
                print_identity_info()
            
            elif command == '2':
                try:
                    print("\nOpening file picker (single file)...")
                    await pick_single_file()
                except Exception as e:
                    print(f"✗ Error opening file picker: {e}")
            
            elif command == '3':
                try:
                    print("\nOpening file picker (multiple files)...")
                    await pick_multiple_files()
                except Exception as e:
                    print(f"✗ Error opening file picker: {e}")
            
            elif command == '4':
                try:
                    print("\nOpening folder picker...")
                    await pick_folder()
                except Exception as e:
                    print(f"✗ Error opening folder picker: {e}")
            
            else:
                print(f"✗ Unknown command: '{command}'")
                print("  Type 'h' for help or 'q' to quit")
        
        except KeyboardInterrupt:
            print("\n\nInterrupted. Type 'q' to quit or press Ctrl+C again.")
            try:
                time.sleep(0.5)
            except KeyboardInterrupt:
                print("\nGoodbye!")
                break
        
        except EOFError:
            print("\n\nGoodbye!")
            break
        
        except Exception as e:
            print(f"\n✗ Unexpected error: {e}")
            import traceback
            traceback.print_exc()


def main():
    """Main entry point."""
    try:
        asyncio.run(run_interactive_mode())
    except Exception as e:
        print(f"\n❌ Fatal Error: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)


if __name__ == "__main__":
    main()
