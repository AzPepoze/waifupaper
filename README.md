<p align="center">
  <h1 align="center">🌸 BrowserAsWallpaper 🌸</h1>
  <img src="https://img.shields.io/badge/platform-linux%20%7C%20windows-blue" />
</p>

A modern, cross-platform application that hosts any web page as your desktop wallpaper.

## Features

-    **Cross-Platform:** Native support for Linux (Wayland/GTK4) and Windows (WinForms/WebView2).
-    **Web-Based:** Use any URL or local web page as a live wallpaper.
-    **System Tray:** Minimal footprint with a system tray icon.
-    **Lightweight:** Low resource usage using system native web engines.

# Linux (Wayland)

Requires `GTK4`, `WebKitGTK`, and `gtk4-layer-shell`. These dependencies must be installed via your system's package manager.

**Example (Arch Linux):**

```bash
sudo pacman -S gtk4 webkitgtk-6.0 gtk4-layer-shell libayatana-appindicator python-gobject
```

1. Run the launcher:
     ```bash
     ./browser-as-wallpaper.sh
     ```

# Windows

**Requirement:** [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) must be installed.

1. Run `browser-as-wallpaper.exe`.

# Development

## Prerequisites

To build this project, you need:

-    **[Python 3](https://www.python.org/)**
-    **[.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)**

## Build Instructions

Run the build script from the project root:

```bash
python build.py
```

-    **`dist/`**: Contains unzipped ready-to-run folders.
-    **`release/`**: Contains compressed `.zip` archives.

# Special Thanks & Appreciation

-    All the open-source contributors whose libraries made this project possible.