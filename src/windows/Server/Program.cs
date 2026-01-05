using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Linq;
using WaifuPaper;

namespace WaifuPaper.Server;

static class Program
{
	private static NotifyIcon? trayIcon;

	[STAThread]
	static void Main(string[] args)
	{
		try
		{
			string logPath = Path.Combine(AppContext.BaseDirectory, "server.log");
			var fileStream = new FileStream(logPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
			var writer = new StreamWriter(fileStream) { AutoFlush = true };
			Console.SetOut(writer);
			Console.SetError(writer);
		}
		catch { }

		SharedLib.AttachConsole(-1);
		Console.WriteLine($"--- WaifuPaper Server Started at {DateTime.Now} ---");
		
		ApplicationConfiguration.Initialize();

		string url = Constants.ServerUrl;
		
		if (IsServerRunning(url))
		{
			Console.WriteLine("[Server] Already running. Exiting.");
			return;
		}

		EmbeddedServer server = new EmbeddedServer(url);
		Task.Run(() => server.Start());

		// Only show tray if not disabled by main launcher
		if (!args.Contains("--no-tray"))
		{
			trayIcon = Tray.CreateTray("WaifuPaper Server", (s, e) => {
				trayIcon!.Visible = false;
				server.Stop();
				Application.Exit();
			});
		}

		Application.Run();
	}

	private static bool IsServerRunning(string url)
	{
		try
		{
			using (var client = new System.Net.Http.HttpClient())
			{
				client.Timeout = TimeSpan.FromMilliseconds(200);
				var response = client.GetAsync(url).Result;
				return true;
			}
		}
		catch { return false; }
	}
}