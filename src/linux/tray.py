import sys
import os
import signal
import gi

gi.require_version('Gtk', '3.0')
from gi.repository import Gtk, GLib

# Default App Name
APP_NAME = "BrowserAsWallpaper"

# Get info from arguments if available
# argv[1] = parent_pid, argv[2] = app_name
if len(sys.argv) > 2:
    APP_NAME = sys.argv[2]

GLib.set_prgname(APP_NAME)
GLib.set_application_name(APP_NAME)

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
    if len(sys.argv) > 1:
        parent_pid = int(sys.argv[1])
        try:
            os.kill(parent_pid, signal.SIGTERM)
        except ProcessLookupError:
            pass
    Gtk.main_quit()

def build_menu():
    menu = Gtk.Menu()
    item_quit = Gtk.MenuItem(label=f"Quit {APP_NAME}")
    item_quit.connect('activate', quit_app)
    menu.append(item_quit)
    menu.show_all()
    return menu

def main():
    if APP_INDICATOR is None:
        return

    indicator = APP_INDICATOR.Indicator.new(
        "browser-as-wallpaper-tray",
        "preferences-desktop-wallpaper",
        APP_INDICATOR.IndicatorCategory.APPLICATION_STATUS
    )
    indicator.set_status(APP_INDICATOR.IndicatorStatus.ACTIVE)
    indicator.set_menu(build_menu())
    indicator.set_label(APP_NAME, APP_NAME)
    
    signal.signal(signal.SIGINT, signal.SIG_DFL)
    Gtk.main()

if __name__ == "__main__":
    main()