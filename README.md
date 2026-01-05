<p align="center">
  <h1 align="center">🌸 WaifuPaper 🌸</h1>
  <img src="https://img.shields.io/badge/platform-linux%20%7C%20windows-blue" />
</p>

A modern, cross-platform live wallpaper application that displays random high-quality anime wallpapers from Konachan directly on your desktop.

## Contents

-    [Features](#features)
-    [Installation & Usage](#installation--usage)
     -    [Linux](#linux-wayland)
     -    [Windows](#windows)
-    [Development & Building](#development--building)
-    [Acknowledgments](#acknowledgments)

## Features

-    **Cross-Platform:** Native support for Linux (Wayland/GTK4) and Windows (WinForms/WebView2).
-    **Interactive UI:** Smooth crossfade transitions and a modern "Random" button with backdrop blur.
-    **System Tray:** Minimal footprint with a system tray icon for easy exit.
-    **Lightweight:** Low resource usage, serving content via a local embedded proxy.
-    **Bypass Blocks:** Integrated proxy system to ensure images load even if the source is restricted by ISP.

## Installation & Usage

### Linux (Wayland)

Requires `GTK4`, `WebKitGTK`, and `gtk4-layer-shell`.

**Arch Linux:**

```bash
sudo pacman -S gtk4 webkitgtk-6.0 gtk4-layer-shell libayatana-appindicator python-gobject
```

1. Download the `linux.zip` from the Releases page.
2. Extract the archive.
3. Run the launcher:
     ```bash
     ./waifupaper.sh
     ```

### Windows

1. Download the `windows.zip` from the Releases page.
2. Extract and run `WaifuPaper.exe`.

## Development & Building

### Prerequisites

-    **Node.js & pnpm:** For building the frontend.
-    **Python 3:** For running/packaging the Linux version.
-    **.NET 8 SDK:** For compiling the Windows version.

### Build Instructions

Run the automated build script to generate releases for both platforms:

```bash
python3 build.py
```

Outputs will be generated in the `release/` directory.

## Acknowledgments

-    Wallpapers provided by [Konachan](https://konachan.net).
