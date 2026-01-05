using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;

namespace BrowserAsWallpaper.Main;

static class Program
{
	private static List<Process> childProcesses = new List<Process>();
	private static NotifyIcon? trayIcon;

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

		ApplicationConfiguration.Initialize();

		string baseDir = AppContext.BaseDirectory;
		string webviewExe = Path.Combine(baseDir, "browser-as-wallpaper-webview.exe");

		// 1. Start WebView with --no-tray
		StartProcess(webviewExe, "--no-tray");

		// 2. Create Main Tray
		trayIcon = Tray.CreateTray("BrowserAsWallpaper", OnExit);

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
		trayIcon!.Visible = false;
		
		// Kill children
		foreach (var proc in childProcesses)
		{
			try { proc.Kill(); } catch { }
		}

		// Also try to find and kill by name just in case
		foreach (var proc in Process.GetProcessesByName("browser-as-wallpaper-webview")) try { proc.Kill(); } catch { }

		Application.Exit();
	}
}