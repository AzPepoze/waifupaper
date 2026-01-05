import os
import sys
import subprocess
import time
import psutil
from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler

# --- Configuration ---
WATCH_DIRECTORY = "src"
DEBOUNCE_INTERVAL_SECONDS = 3
IGNORE_PATTERNS = [
    "node_modules", "dist", "bin", "obj", ".git", 
    "build", "release", ".swp", "~"
]
TARGET_PROCESS_NAMES = ["BrowserAsWallpaper.exe", "BrowserAsWallpaper", "python3"]

# Global process handle
app_process = None

def kill_browser-as-wallpaper_processes():
    """Cleanly stops any running instances of the application."""
    _kill_process_tree()
    _kill_lingering_processes_by_name()

def _kill_process_tree():
    global app_process
    if app_process:
        print("[Dev] Stopping active application process...")
        try:
            parent = psutil.Process(app_process.pid)
            for child in parent.children(recursive=True):
                child.kill()
            parent.kill()
        except psutil.NoSuchProcess:
            pass
        app_process = None

def _kill_lingering_processes_by_name():
    for proc in psutil.process_iter(['name', 'cmdline']):
        try:
            if _is_target_process(proc):
                print(f"[Dev] Killing lingering process: {proc.info['name']} ({proc.pid})")
                proc.kill()
        except (psutil.NoSuchProcess, psutil.AccessDenied):
            pass

def _is_target_process(proc):
    name = proc.info['name']
    cmdline = " ".join(proc.info['cmdline'] or [])
    
    is_matching_name = name in TARGET_PROCESS_NAMES
    is_linux_script = "browser-as-wallpaper.py" in cmdline
    
    return is_matching_name or is_linux_script

def run_build_script():
    """Executes the build.py script with the --no-pack flag."""
    print("\n[Dev] Running build.py...")
    try:
        subprocess.run([sys.executable, "build.py", "--no-pack"], check=True)
        return True
    except subprocess.CalledProcessError:
        print("[Dev] Build failed!")
        return False

def launch_application():
    """Starts the application based on the current operating system."""
    global app_process
    print("[Dev] Launching application...")
    
    if sys.platform == "win32":
        app_process = _launch_windows()
    else:
        app_process = _launch_linux()

def _launch_windows():
    exe_path = os.path.join(os.getcwd(), "build", "windows_publish", "BrowserAsWallpaper.exe")
    if os.path.exists(exe_path):
        return subprocess.Popen([exe_path], cwd=os.path.dirname(exe_path))
    
    print(f"[Dev] Binary not found. Using 'dotnet run' fallback.")
    win_project_dir = os.path.join(os.getcwd(), "src", "windows")
    return subprocess.Popen(["dotnet", "run"], cwd=win_project_dir, shell=True)

def _launch_linux():
    linux_pkg_dir = os.path.join(os.getcwd(), "build", "linux_pkg")
    launcher = os.path.join(linux_pkg_dir, "browser-as-wallpaper.sh")
    
    if os.path.exists(launcher):
        os.chmod(launcher, 0o755)
        return subprocess.Popen([launcher], cwd=linux_pkg_dir)
    
    print(f"[Dev] Package not found. Using 'browser-as-wallpaper.py' fallback.")
    linux_src_dir = os.path.join(os.getcwd(), "src", "linux")
    return subprocess.Popen([sys.executable, "browser-as-wallpaper.py"], cwd=linux_src_dir)

def restart_environment():
    """Orchestrates the full stop-build-start cycle."""
    kill_browser-as-wallpaper_processes()
    if run_build_script():
        launch_application()

class SourceCodeWatcher(FileSystemEventHandler):
    def __init__(self):
        self.last_triggered = 0

    def on_any_event(self, event):
        if event.is_directory or self._is_ignored(event.src_path):
            return

        if self._should_debounce():
            print(f"\n[Dev] Change detected: {event.src_path}")
            self.last_triggered = time.time()
            time.sleep(0.5) # Wait for file write completion
            restart_environment()

    def _is_ignored(self, path):
        return any(pattern in path for pattern in IGNORE_PATTERNS)

    def _should_debounce(self):
        return (time.time() - self.last_triggered) > DEBOUNCE_INTERVAL_SECONDS

def ensure_dependencies_installed():
    try:
        import watchdog
        import psutil
    except ImportError:
        print("[Dev] Installing required Python packages...")
        subprocess.run([sys.executable, "-m", "pip", "install", "-r", "requirements.txt"], shell=True)

if __name__ == "__main__":
    print("--- BrowserAsWallpaper Auto-Dev System ---")
    
    ensure_dependencies_installed()
    restart_environment()

    event_handler = SourceCodeWatcher()
    observer = Observer()
    observer.schedule(event_handler, WATCH_DIRECTORY, recursive=True)
    observer.start()

    print(f"\n[Dev] Watching '{WATCH_DIRECTORY}' for changes. Press Ctrl+C to stop.")
    try:
        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        print("\n[Dev] Shutting down...")
        observer.stop()
        kill_browser-as-wallpaper_processes()
    
    observer.join()