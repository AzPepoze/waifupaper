namespace WaifuPaper;

static class Program
{
	private static NotifyIcon? trayIcon;
	private static List<WaifuPaperWindow> windows = new List<WaifuPaperWindow>();

	[STAThread]
	static void Main(string[] args)
	{
		// Redirect console output to a file
		try
		{
			string logPath = Path.Combine(AppContext.BaseDirectory, "debug.log");
			var fileStream = new FileStream(logPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
			var writer = new StreamWriter(fileStream) { AutoFlush = true };
			Console.SetOut(writer);
			Console.SetError(writer);
		}
		catch { }

		// Try to attach to the parent process's console (terminal)
		NativeMethods.AttachConsole(-1);

		Console.WriteLine($"--- WaifuPaper Started at {DateTime.Now} ---");
		ApplicationConfiguration.Initialize();

		foreach (Screen screen in Screen.AllScreens)
		{
			WaifuPaperWindow window = new WaifuPaperWindow(screen);
			window.Show();
			windows.Add(window);
		}

		ContextMenuStrip trayMenu = new ContextMenuStrip();
		trayMenu.Items.Add("Exit WaifuPaper", null, OnExit);

		trayIcon = new NotifyIcon();
		trayIcon.Text = "WaifuPaper";
		trayIcon.Icon = SystemIcons.Application;
		trayIcon.ContextMenuStrip = trayMenu;
		trayIcon.Visible = true;

		Application.Run();
	}

	private static void OnExit(object? sender, EventArgs e)
	{
		trayIcon!.Visible = false;
		WaifuPaperWindow.StopServer();
		Application.Exit();
	}
}