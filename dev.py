import os
import sys
import subprocess
import time
import psutil
from watchdog.observers import Observer
from watchdog.events import PatternMatchingEventHandler

# -------------------------------------------------------
# Configuration
# -------------------------------------------------------
WATCH_DIRECTORY = "src"
DEBOUNCE_DELAY_SECONDS = 1.0
POLLING_INTERVAL = 0.5

# Glob patterns to ignore (More accurate than substring matching)
IGNORE_PATTERNS = ["*/node_modules/*", "*/dist/*", "*/bin/*", "*/obj/*", "*/.git/*", "*/build/*", "*/release/*", "*/__pycache__/*", "*.swp", "*~", "*.pyc", "*.tmp"]

TARGET_PROCESS_NAMES = ["WaifuPaper.exe", "WaifuPaper", "python3"]


class DevServer:
    def __init__(self):
        self.app_process = None
        self.last_change_time = 0
        self.last_rebuild_time = 0
        self.needs_restart = True

    # -------------------------------------------------------
    # Process Management
    # -------------------------------------------------------
    def kill_browser_as_wallpaper_processes(self):
        """Cleanly stops any running instances of the application."""
        self._kill_process_tree()
        self._kill_lingering_processes_by_name()

    def _kill_process_tree(self):
        if self.app_process:
            print("[Dev] Stopping active application process...")
            try:
                if self.app_process.poll() is None:  # Check if still running
                    parent = psutil.Process(self.app_process.pid)
                    for child in parent.children(recursive=True):
                        child.kill()
                    parent.kill()
            except psutil.NoSuchProcess:
                pass
            self.app_process = None

    def _kill_lingering_processes_by_name(self):
        for proc in psutil.process_iter(["name", "cmdline"]):
            try:
                if self._is_target_process(proc):
                    print(f"[Dev] Killing lingering process: {proc.info['name']} ({proc.pid})")
                    proc.kill()
            except (psutil.NoSuchProcess, psutil.AccessDenied):
                pass

    def _is_target_process(self, proc):
        name = proc.info["name"]
        cmdline = " ".join(proc.info["cmdline"] or [])

        is_matching_name = name in TARGET_PROCESS_NAMES
        is_linux_script = "webview.py" in cmdline

        # Don't kill the dev script itself or the watcher
        is_self = proc.pid == os.getpid()

        return (is_matching_name or is_linux_script) and not is_self

    # -------------------------------------------------------
    # Build & Launch
    # -------------------------------------------------------
    def run_build_script(self):
        """Executes the build.py script with the --no-pack flag."""
        print("\n[Dev] Running build.py...")
        try:
            # flush stdout to ensure order of logs
            sys.stdout.flush()
            subprocess.run([sys.executable, "build.py", "--no-pack"], check=True)
            return True
        except subprocess.CalledProcessError:
            print("[Dev] Build failed!")
            return False

    def launch_application(self):
        """Starts the application based on the current operating system."""
        print("[Dev] Launching application...\n")

        if sys.platform == "win32":
            self.app_process = self._launch_windows()
        else:
            self.app_process = self._launch_linux()

    def _launch_windows(self):
        exe_path = os.path.join(os.getcwd(), "build", "windows_publish", "BrowserAsWallpaper.exe")
        if os.path.exists(exe_path):
            return subprocess.Popen([exe_path], cwd=os.path.dirname(exe_path))

        print(f"[Dev] Binary not found. Using 'dotnet run' fallback.")
        win_project_dir = os.path.join(os.getcwd(), "src", "windows")
        return subprocess.Popen(["dotnet", "run"], cwd=win_project_dir, shell=True)

    def _launch_linux(self):
        linux_pkg_dir = os.path.join(os.getcwd(), "build", "linux_pkg")
        launcher = os.path.join(linux_pkg_dir, "run.sh")

        if os.path.exists(launcher):
            os.chmod(launcher, 0o755)
            return subprocess.Popen([launcher], cwd=linux_pkg_dir)

        print(f"[Dev] Package not found. Using 'webview.py' fallback.")
        linux_src_dir = os.path.join(os.getcwd(), "src", "linux")
        return subprocess.Popen([sys.executable, "webview.py"], cwd=linux_src_dir)

    def restart_environment(self):
        """Orchestrates the full stop-build-start cycle."""
        self.kill_browser_as_wallpaper_processes()
        if self.run_build_script():
            self.launch_application()
        self.last_rebuild_time = time.time()
        self.needs_restart = False

    # -------------------------------------------------------
    # Main Loop
    # -------------------------------------------------------
    def trigger_change(self, path):
        print(f"\n[Dev] Change detected: {path}")
        self.last_change_time = time.time()
        self.needs_restart = True

    def run(self):
        print("--- BrowserAsWallpaper Dev System ---")
        self.ensure_dependencies_installed()

        # Start File Watcher
        event_handler = SourceCodeWatcher(self)
        observer = Observer()
        observer.schedule(event_handler, WATCH_DIRECTORY, recursive=True)
        observer.start()

        print(f"\n[Dev] Watching '{WATCH_DIRECTORY}' for changes. Press Ctrl+C to stop.")

        try:
            while True:
                current_time = time.time()

                # Logic: If flagged for restart AND enough time has passed since last file change (Debounce)
                if self.needs_restart and (current_time - self.last_change_time > DEBOUNCE_DELAY_SECONDS):
                    self.restart_environment()

                time.sleep(POLLING_INTERVAL)

        except KeyboardInterrupt:
            print("\n[Dev] Shutting down...")
            observer.stop()
            self.kill_browser_as_wallpaper_processes()

        observer.join()

    def ensure_dependencies_installed(self):
        try:
            import watchdog
            import psutil
        except ImportError:
            print("[Dev] Installing required Python packages...")
            subprocess.run([sys.executable, "-m", "pip", "install", "-r", "requirements.txt"], shell=True)


class SourceCodeWatcher(PatternMatchingEventHandler):
    """
    Watches for changes using glob patterns for better accuracy.
    Delegates the 'reaction' to the DevServer instance via trigger_change.
    """

    def __init__(self, server_instance):
        super().__init__(ignore_patterns=IGNORE_PATTERNS, case_sensitive=False)
        self.server = server_instance

    def on_modified(self, event):
        if not event.is_directory:
            self.server.trigger_change(event.src_path)

    def on_created(self, event):
        if not event.is_directory:
            self.server.trigger_change(event.src_path)

    def on_deleted(self, event):
        if not event.is_directory:
            self.server.trigger_change(event.src_path)

    def on_moved(self, event):
        if not event.is_directory:
            self.server.trigger_change(event.src_path)


if __name__ == "__main__":
    server = DevServer()
    server.run()
