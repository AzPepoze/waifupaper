import sys
import os
import signal
import gi

# Ensure GTK3 is used for AppIndicator (standard compatibility)
gi.require_version('Gtk', '3.0')
from gi.repository import Gtk

# Try importing AppIndicator (supports Ayatana or standard AppIndicator3)
APP_INDICATOR = None
try:
    gi.require_version('AyatanaAppIndicator3', '0.1')
    from gi.repository import AyatanaAppIndicator3 as AppIndicator
    APP_INDICATOR = AppIndicator
except ValueError:
    try:
        gi.require_version('AppIndicator3', '0.1')
        from gi.repository import AppIndicator3 as AppIndicator
        APP_INDICATOR = AppIndicator
    except ValueError:
        print("[Tray] Error: AppIndicator library not found.")
        sys.exit(1)

def quit_app(source):
    """
    Handles the Quit menu action.
    Kills the parent process (if PID provided) and exits.
    """
    if len(sys.argv) > 1:
        parent_pid = int(sys.argv[1])
        try:
            os.kill(parent_pid, signal.SIGTERM)
        except ProcessLookupError:
            pass # Parent already dead
    
    Gtk.main_quit()

def build_menu():
    """Creates the system tray context menu."""
    menu = Gtk.Menu()
    
    item_quit = Gtk.MenuItem(label="Quit WaifuPaper")
    item_quit.connect('activate', quit_app)
    menu.append(item_quit)
    
    menu.show_all()
    return menu

def main():
    if APP_INDICATOR is None:
        return

    indicator = APP_INDICATOR.Indicator.new(
        "waifupaper-tray",
        "preferences-desktop-wallpaper", # Fallback system icon
        APP_INDICATOR.IndicatorCategory.APPLICATION_STATUS
    )
    indicator.set_status(APP_INDICATOR.IndicatorStatus.ACTIVE)
    indicator.set_menu(build_menu())
    
    # Enable Ctrl+C to kill the tray process gracefully
    signal.signal(signal.SIGINT, signal.SIG_DFL)
    
    Gtk.main()

if __name__ == "__main__":
    main()