import os
import sys
import ctypes
import subprocess
import gi

os.environ["GDK_BACKEND"] = "wayland"

try:
    ctypes.CDLL("libgtk4-layer-shell.so")
except OSError:
    pass

gi.require_version("Gtk", "4.0")
gi.require_version("WebKit", "6.0")

try:
    gi.require_version("Gtk4LayerShell", "1.0")
    from gi.repository import Gtk4LayerShell as LayerShell
except ValueError:
    print("Error: gtk4-layer-shell not found.")
    sys.exit(1)

from gi.repository import Gtk, Gio, WebKit
from constants import APP_ID, PORT

class BrowserAsWallpaperApp(Gtk.Application):
    def __init__(self):
        super().__init__(application_id=APP_ID, flags=Gio.ApplicationFlags.FLAGS_NONE)
        self.tray_process = None
        self.server_process = None
        self.project_dir = os.path.dirname(os.path.abspath(__file__))

    def do_activate(self):
        self.start_service("server.py")
        self.start_service("tray.py", str(os.getpid()))

        window = Gtk.Window(application=self)
        self.setup_layershell(window)
        self.setup_webview(window)
        window.present()

    def start_service(self, script_name, *args):
        script_path = os.path.join(self.project_dir, script_name)
        if not os.path.exists(script_path):
            return
        try:
            cmd = [sys.executable, script_path] + list(args)
            proc = subprocess.Popen(cmd)
            if script_name == "tray.py":
                self.tray_process = proc
            elif script_name == "server.py":
                self.server_process = proc
        except Exception as e:
            print(f"Error starting {script_name}: {e}")

    def setup_layershell(self, window):
        LayerShell.init_for_window(window)
        LayerShell.set_layer(window, LayerShell.Layer.BOTTOM)

        try:
            LayerShell.set_keyboard_mode(window, LayerShell.KeyboardMode.ON_DEMAND)
        except AttributeError:
            pass

        LayerShell.set_anchor(window, LayerShell.Edge.TOP, True)
        LayerShell.set_anchor(window, LayerShell.Edge.BOTTOM, True)
        LayerShell.set_anchor(window, LayerShell.Edge.LEFT, True)
        LayerShell.set_anchor(window, LayerShell.Edge.RIGHT, True)
        LayerShell.set_exclusive_zone(window, -1)

    def setup_webview(self, window):
        web_view = WebKit.WebView()
        settings = web_view.get_settings()
        settings.set_enable_developer_extras(True)
        web_view.load_uri(f"http://localhost:{PORT}/")
        window.set_child(web_view)

    def do_shutdown(self):
        if self.tray_process:
            self.tray_process.terminate()
        if self.server_process:
            self.server_process.terminate()
        Gio.Application.do_shutdown(self)

if __name__ == "__main__":
    app = BrowserAsWallpaperApp()
    app.run(None)