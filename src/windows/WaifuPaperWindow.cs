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

	protected override async void OnLoad(EventArgs e)
	{
		base.OnLoad(e);
		await Task.Delay(1000);
		PinToDesktop();
	}

	protected override CreateParams CreateParams
	{
		get
		{
			CreateParams cp = base.CreateParams;
			cp.ExStyle |= 0x80 | 0x08000000;
			return cp;
		}
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
		if (progman == IntPtr.Zero)
		{
			MessageBox.Show("Debug: Progman not found");
			return;
		}

		NativeMethods.SendMessageTimeout(progman, 0x052C, new IntPtr(0), IntPtr.Zero, 0x0000, 1000, out _);

		IntPtr workerW = IntPtr.Zero;
		IntPtr shellDll = IntPtr.Zero;

		NativeMethods.EnumWindows((tophandle, topparamhandle) =>
		{
			IntPtr foundShell = NativeMethods.FindWindowEx(tophandle, IntPtr.Zero, "SHELLDLL_DefView", null);
			if (foundShell != IntPtr.Zero)
			{
				shellDll = foundShell;
				workerW = NativeMethods.FindWindowEx(IntPtr.Zero, tophandle, "WorkerW", null);
			}
			return true;
		}, IntPtr.Zero);

		if (workerW == IntPtr.Zero)
		{
			NativeMethods.EnumWindows((tophandle, topparamhandle) =>
			{
				StringBuilder className = new StringBuilder(256);
				NativeMethods.GetClassName(tophandle, className, className.Capacity);
				if (className.ToString() == "WorkerW")
				{
					IntPtr foundShell = NativeMethods.FindWindowEx(tophandle, IntPtr.Zero, "SHELLDLL_DefView", null);
					if (foundShell == IntPtr.Zero)
					{
						workerW = tophandle;
					}
				}
				return true;
			}, IntPtr.Zero);
		}

		if (workerW != IntPtr.Zero)
		{
			MessageBox.Show($"Debug: Found WorkerW {workerW}. Parenting window...");
			
			NativeMethods.SetParent(this.Handle, workerW);
			
			int style = NativeMethods.GetWindowLong(this.Handle, NativeMethods.GWL_STYLE);
			style |= NativeMethods.WS_CHILD;
			style &= ~NativeMethods.WS_POPUP;
			NativeMethods.SetWindowLong(this.Handle, NativeMethods.GWL_STYLE, style);

			this.Location = new Point(0, 0);
			this.Size = currentScreen.Bounds.Size;

			NativeMethods.ShowWindow(this.Handle, 5);
			NativeMethods.SetWindowPos(this.Handle, (IntPtr)0, 0, 0, currentScreen.Bounds.Width, currentScreen.Bounds.Height, 0x0040);
		}
		else
		{
			MessageBox.Show("Debug: Could not find any WorkerW layer.");
		}
	}

	public static void StopServer()
	{
		sharedServer?.Stop();
	}
}