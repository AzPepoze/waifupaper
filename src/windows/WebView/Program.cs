using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Linq;
using WaifuPaper;

namespace WaifuPaper.WebView;

static class Program
{
	private static List<WaifuPaperWindow> windows = new List<WaifuPaperWindow>();
	private static NotifyIcon? trayIcon;

	[STAThread]
	static void Main(string[] args)
	{
		try
		{
			string logPath = Path.Combine(AppContext.BaseDirectory, "webview.log");
			var fileStream = new FileStream(logPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
			var writer = new StreamWriter(fileStream) { AutoFlush = true };
			Console.SetOut(writer);
			Console.SetError(writer);
		}
		catch { }

		SharedLib.AttachConsole(-1);
		Console.WriteLine($"--- {Constants.AppName} WebView Started at {DateTime.Now} ---");

		ApplicationConfiguration.Initialize();

		foreach (Screen screen in Screen.AllScreens)
		{
			WaifuPaperWindow window = new WaifuPaperWindow(screen);
			window.Show();
			windows.Add(window);
		}

		// Only show tray if not disabled by main launcher
		if (!args.Contains("--no-tray"))
		{
			trayIcon = Tray.CreateTray("WaifuPaper WebView", (s, e) => {
				trayIcon!.Visible = false;
				Application.Exit();
			});
		}

		Application.Run();
	}
}