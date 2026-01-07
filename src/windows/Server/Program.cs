using System;
using System.Threading;
using System.Windows.Forms;
using WaifuPaper;

namespace WaifuPaper.Server;

class Program
{
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

		// Load Config
		var config = ConfigLoader.Load();
		string url = $"http://localhost:{config.port}/";

		Console.WriteLine($"[Server] Starting on {url}...");

		// Initialize EmbeddedServer
		var server = new EmbeddedServer(url);

		// Handle process exit
		AppDomain.CurrentDomain.ProcessExit += (s, e) => 
		{
			Console.WriteLine("[Server] Stopping...");
			server.Stop();
		};

		// Start Server
		server.Start();
	}
}