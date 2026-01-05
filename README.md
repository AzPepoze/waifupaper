<p align="center">
  <h1 align="center">🌸 WaifuPaper 🌸</h1>
  <img src="https://img.shields.io/badge/platform-linux%20%7C%20windows-blue" />
</p>

A modern, cross-platform live wallpaper application that displays random high-quality anime wallpapers from Konachan directly on your desktop.

![Preview](showcase/screen1.png)

## Features

-    **Cross-Platform:** Native support for Linux (Wayland/GTK4) and Windows (WinForms/WebView2).
-    **Interactive UI:** Smooth crossfade transitions and a modern "Random" button with backdrop blur.
-    **System Tray:** Minimal footprint with a system tray icon for easy exit.
-    **Lightweight:** Low resource usage, serving content via a local embedded proxy.

# Linux (Wayland)

Requires `GTK4`, `WebKitGTK`, and `gtk4-layer-shell`. These dependencies must be installed via your system's package manager.

**Example (Arch Linux):**

```bash
sudo pacman -S gtk4 webkitgtk-6.0 gtk4-layer-shell libayatana-appindicator python-gobject
```

1. Download the latest `linux.zip` from the [Releases](https://github.com/AzPepoze/waifupaper/releases/latest) page.
2. Extract the archive.
3. Run the launcher:
     ```bash
     ./waifupaper.sh
     ```

# Windows

**Requirement:** [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) must be installed.

1. Download the latest `windows.zip` from the [Releases](https://github.com/AzPepoze/waifupaper/releases/latest) page.
2. Extract the archive.
3. Run `waifupaper.exe`.

# Wallpaper Engine

If you prefer to use **Wallpaper Engine** instead of the built-in desktop overlay:

1. Ensure `waifupaper-server.exe` is running (you can run `waifupaper.exe` once, or run the server standalone).
2. In Wallpaper Engine, click on **"Open Wallpaper"** at the bottom left.
3. Select **"Open from URL"**.
4. Enter the following URL: `http://localhost:49555/`.

# Development

## Prerequisites

To build this project, you need:

-    **[Node.js](https://nodejs.org/en/download)** & **[pnpm](https://pnpm.io/)**
-    **[Python 3](https://www.python.org/)**
-    **[.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)**

## Build Instructions

Run the parallel build script from the project root:

```bash
python build.py
```

-    **`dist/`**: Contains unzipped ready-to-run folders.
-    **`release/`**: Contains compressed `.zip` archives.

## Run in developing mode

1. **Install Python Dependencies**:

     ```bash
     pip install -r requirements.txt
     ```

2. **Launch the Auto-Dev System**:
     ```bash
     python dev.py
     ```

# Special Thanks & Appreciation

We would like to express our gratitude to the following services and communities:

-    **[Konachan](https://konachan.net)**: For providing an amazing collection of high-quality anime wallpapers.
-    All the open-source contributors whose libraries made this project possible.
