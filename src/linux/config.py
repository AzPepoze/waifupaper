import os
import json

APP_NAME = "BrowserAsWallpaper"
APP_ID = "com.azpepoze.browser-as-wallpaper"

try:
    current_dir = os.path.dirname(os.path.abspath(__file__))
    # Try finding config.json in the same directory or parent directory
    possible_paths = [
        os.path.join(current_dir, "config.json"),
        os.path.join(os.path.dirname(current_dir), "config.json")
    ]
    
    for config_path in possible_paths:
        if os.path.exists(config_path):
            with open(config_path, "r") as f:
                data = json.load(f)
                APP_NAME = data.get("app_name", APP_NAME)
                APP_ID = data.get("app_id", APP_ID)
            break
except Exception:
    pass
