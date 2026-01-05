using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Runtime.InteropServices;
using System.Text;

namespace WaifuPaper;

public class WaifuPaperWindow : Form
{
	private static EmbeddedServer? sharedServer;
	private static readonly object serverLock = new object();
	private WebView2 webView;
	private Screen currentScreen;

	public WaifuPaperWindow(Screen screen)
	{
		this.currentScreen = screen;

		this.FormBorderStyle = FormBorderStyle.None;
		this.WindowState = FormWindowState.Normal;
		this.StartPosition = FormStartPosition.Manual;
		this.ShowInTaskbar = false;

		this.Location = screen.Bounds.Location;
		this.Size = screen.Bounds.Size;

		lock (serverLock)
		{
			if (sharedServer == null)
			{
				sharedServer = new EmbeddedServer("http://127.0.0.1:43210/");
				Task.Run(() => sharedServer.Start());
			}
		}

		webView = new WebView2();
		webView.Dock = DockStyle.Fill;
		webView.DefaultBackgroundColor = Color.Black;
		this.Controls.Add(webView);

		InitializeWebViewAsync();
	}

	protected override bool ShowWithoutActivation => true;

	protected override async void OnLoad(EventArgs e)
	{
		base.OnLoad(e);
		PinToDesktop();
	}

	private async void InitializeWebViewAsync()
	{
		try
		{
			await webView.EnsureCoreWebView2Async();
			webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
			webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
			webView.CoreWebView2.Navigate("http://127.0.0.1:43210/");
		}
		catch { }
	}

	private void PinToDesktop()
	{
		IntPtr progman = NativeMethods.FindWindow("Progman", null);
		if (progman == IntPtr.Zero) return;

		// Trigger the creation of WorkerW
		NativeMethods.SendMessageTimeout(progman, 0x052C, new IntPtr(0), IntPtr.Zero, 0x0000, 1000, out _);

		IntPtr workerW = IntPtr.Zero;
		IntPtr child = IntPtr.Zero;

		// Find the WorkerW child of Progman
		do
		{
			child = NativeMethods.FindWindowEx(progman, child, "WorkerW", null);
			if (child != IntPtr.Zero)
			{
				workerW = child;
				break;
			}
		} while (child != IntPtr.Zero);

		if (workerW != IntPtr.Zero)
		{
			Console.WriteLine($"[Debug] Parenting to WorkerW: 0x{workerW.ToInt64():X}");
			NativeMethods.SetParent(this.Handle, workerW);

			this.Location = new Point(0, 0);
			this.Size = currentScreen.Bounds.Size;

			NativeMethods.SetWindowPos(this.Handle, NativeMethods.HWND_BOTTOM, 0, 0, currentScreen.Bounds.Width, currentScreen.Bounds.Height, 0x0040 | 0x0010);
		}
	}

	public static void StopServer()
	{
		sharedServer?.Stop();
	}
}