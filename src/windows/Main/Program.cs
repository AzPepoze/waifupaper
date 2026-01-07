using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text.Json;
using BrowserAsWallpaper;

namespace BrowserAsWallpaper.Main;

static class Program
{
	private static List<Process> childProcesses = new List<Process>();
	private static NotifyIcon? trayIcon;
	private static string appName = "BrowserAsWallpaper";

	[STAThread]
	static void Main(string[] args)
	{
		try
		{
			string logPath = Path.Combine(AppContext.BaseDirectory, "main.log");
			var fileStream = new FileStream(logPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
			var writer = new StreamWriter(fileStream) { AutoFlush = true };
			Console.SetOut(writer);
			Console.SetError(writer);
		}
		catch { }

		var config = ConfigLoader.Load();
		appName = config.app_name;
		Console.WriteLine($"[Main] Starting {appName}...");

		ApplicationConfiguration.Initialize();

		string baseDir = AppContext.BaseDirectory;
		string webviewExe = Path.Combine(baseDir, $"{config.binary_name}-webview.exe");

		// 1. Start WebView with --no-tray
		StartProcess(webviewExe, "--no-tray");

		// 2. Create Main Tray
		trayIcon = Tray.CreateTray(appName, OnExit);

		Application.Run();
	}

	private static void StartProcess(string path, string args)
	{
		if (File.Exists(path))
		{
			Process? proc = Process.Start(new ProcessStartInfo(path) { 
				Arguments = args,
				UseShellExecute = true 
			});
			if (proc != null) childProcesses.Add(proc);
		}
	}

	private static void OnExit(object? sender, EventArgs e)
	{
		if (trayIcon != null) trayIcon.Visible = false;
		
		foreach (var proc in childProcesses)
		{
			try { proc.Kill(); } catch { }
		}

		foreach (var proc in Process.GetProcessesByName("browser-as-wallpaper-webview")) try { proc.Kill(); } catch { }

		Application.Exit();
	}
}
