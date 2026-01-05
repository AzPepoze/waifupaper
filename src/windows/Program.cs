namespace WaifuPaper;

static class Program
{
	private static NotifyIcon? trayIcon;
	private static List<WaifuPaperWindow> windows = new List<WaifuPaperWindow>();

	[STAThread]
	static void Main()
	{
		// Try to attach to the parent process's console (terminal)
		NativeMethods.AttachConsole(-1);

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