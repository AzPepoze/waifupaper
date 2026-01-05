using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;

namespace WaifuPaper.Main;

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
		string serverExe = Path.Combine(baseDir, "waifupaper-server.exe");
		string webviewExe = Path.Combine(baseDir, "waifupaper-webview.exe");

		// 1. Start Server with --no-tray
		if (!IsServerRunning(Constants.ServerUrl))
		{
			StartProcess(serverExe, "--no-tray");
			
			int attempts = 0;
			while (!IsServerRunning(Constants.ServerUrl) && attempts < 50)
			{
				Thread.Sleep(100);
				attempts++;
			}
		}

		// 2. Start WebView with --no-tray
		StartProcess(webviewExe, "--no-tray");

		// 3. Create Main Tray
		trayIcon = Tray.CreateTray("WaifuPaper", OnExit);

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
		foreach (var proc in Process.GetProcessesByName("waifupaper-server")) try { proc.Kill(); } catch { }
		foreach (var proc in Process.GetProcessesByName("waifupaper-webview")) try { proc.Kill(); } catch { }

		Application.Exit();
	}

	private static bool IsServerRunning(string url)
	{
		try
		{
			using (var client = new System.Net.Http.HttpClient())
			{
				client.Timeout = TimeSpan.FromMilliseconds(200);
				var response = client.GetAsync(url).Result;
				return response.IsSuccessStatusCode;
			}
		}
		catch { return false; }
	}
}