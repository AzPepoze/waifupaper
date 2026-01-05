using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text.Json;

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

		LoadConfig();
		Console.WriteLine($"[Main] Starting {appName}...");

		ApplicationConfiguration.Initialize();

		string baseDir = AppContext.BaseDirectory;
		string webviewExe = Path.Combine(baseDir, "browser-as-wallpaper-webview.exe");

		// 1. Start WebView with --no-tray
		StartProcess(webviewExe, "--no-tray");

		// 2. Create Main Tray
		trayIcon = Tray.CreateTray(appName, OnExit);

		Application.Run();
	}

	private static void LoadConfig()
	{
		string configPath = Path.Combine(AppContext.BaseDirectory, "config.json");
		if (!File.Exists(configPath))
		{
			// Try src folder (relative to bin/Debug/net8.0-windows/win-x64)
			string devPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "config.json");
			if (File.Exists(devPath)) configPath = devPath;
		}

		if (File.Exists(configPath))
		{
			try
			{
				string jsonString = File.ReadAllText(configPath);
				var config = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);
				if (config != null && config.ContainsKey("app_name"))
				{
					appName = config["app_name"];
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[Config] Error loading config: {ex.Message}");
			}
		}
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
