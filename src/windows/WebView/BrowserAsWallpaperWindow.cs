using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Net.Http;

namespace BrowserAsWallpaper;

public class BrowserAsWallpaperWindow : Form
{
	private WebView2 webView;
	private Screen currentScreen;

	private IntPtr _mouseHookID = IntPtr.Zero;
	private Lib.LowLevelMouseProc _proc;
	private IntPtr _chromeRenderWidgetHostHWND = IntPtr.Zero;

	public BrowserAsWallpaperWindow(Screen screen)
	{
		this.currentScreen = screen;
		Console.WriteLine($"[Window] Initializing for screen: {screen.DeviceName} (Bounds: {screen.Bounds})");

		this.FormBorderStyle = FormBorderStyle.None;
		this.WindowState = FormWindowState.Normal;
		this.StartPosition = FormStartPosition.Manual;
		this.ShowInTaskbar = false;

		this.Location = screen.Bounds.Location;
		this.Size = screen.Bounds.Size;

		webView = null!;

		Console.WriteLine("[Input] Setting up low-level mouse hook...");
		_proc = HookCallback;
		_mouseHookID = SetHook(_proc);
		Console.WriteLine($"[Input] Hook set. ID: 0x{_mouseHookID.ToInt64():X}");
	}

	protected override bool ShowWithoutActivation => true;

	protected override CreateParams CreateParams
	{
		get
		{
			CreateParams cp = base.CreateParams;
			cp.ExStyle |= Lib.WS_EX_TOOLWINDOW;
			cp.ExStyle |= Lib.WS_EX_NOACTIVATE;
			return cp;
		}
	}

	protected override void OnFormClosing(FormClosingEventArgs e)
	{
		Console.WriteLine("[Window] Form closing. Cleaning up...");
		_chromeRenderWidgetHostHWND = IntPtr.Zero;
		if (_mouseHookID != IntPtr.Zero)
		{
			Lib.UnhookWindowsHookEx(_mouseHookID);
			Console.WriteLine("[Input] Mouse hook unhooked.");
		}
		base.OnFormClosing(e);
	}

	protected override void OnLoad(EventArgs e)
	{
		base.OnLoad(e);
		InitializeWebViewAsync();
	}

	private async void InitializeWebViewAsync()
	{
		try
		{
			if (this.IsDisposed) return;

			Console.WriteLine("[WebView] Creating WebView2 control...");
			webView = new WebView2();
			webView.Dock = DockStyle.Fill;
			webView.DefaultBackgroundColor = Color.Black;
			this.Controls.Add(webView);

			Console.WriteLine("[WebView] Starting CoreWebView2 initialization...");
			await webView.EnsureCoreWebView2Async();

			if (webView == null || webView.IsDisposed || webView.CoreWebView2 == null)
			{
				Console.WriteLine("[WebView] Initialization aborted: WebView is disposed or null.");
				return;
			}

			Console.WriteLine("[WebView] CoreWebView2 initialized successfully.");
			webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
			webView.CoreWebView2.Settings.AreDevToolsEnabled = false;

			string url = Constants.DefaultUrl;
			Console.WriteLine($"[WebView] Navigating to: {url}");
			webView.CoreWebView2.Navigate(url);

			FindChromeWindow();
			PinToDesktop();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[WebView] Unexpected error: {ex.Message}");
		}
	}

	private void FindChromeWindow()
	{
		if (webView == null || webView.IsDisposed || !webView.IsHandleCreated) return;
		try
		{
			Lib.EnumChildWindows(this.webView.Handle, (hWnd, lParam) =>
			{
				StringBuilder sb = new StringBuilder(256);
				Lib.GetClassName(hWnd, sb, sb.Capacity);
				if (sb.ToString() == "Chrome_RenderWidgetHostHWND")
				{
					_chromeRenderWidgetHostHWND = hWnd;
					Console.WriteLine($"[WebView] Found Chrome window: 0x{_chromeRenderWidgetHostHWND.ToInt64():X}");
					return false;
				}
				return true;
			}, IntPtr.Zero);
		}
		catch { }
	}

	private IntPtr SetHook(Lib.LowLevelMouseProc proc)
	{
		using (Process curProcess = Process.GetCurrentProcess())
		using (ProcessModule curModule = curProcess.MainModule!)
		{
			return Lib.SetWindowsHookEx(Lib.WH_MOUSE_LL, proc,
				Lib.GetModuleHandle(curModule.ModuleName), 0);
		}
	}

	private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
	{
		if (nCode >= 0 && _chromeRenderWidgetHostHWND != IntPtr.Zero && !this.IsDisposed && webView != null && !webView.IsDisposed)
		{
			try
			{
				Lib.MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<Lib.MSLLHOOKSTRUCT>(lParam);

				IntPtr windowUnderMouse = Lib.WindowFromPoint(hookStruct.pt);
				StringBuilder className = new StringBuilder(256);
				Lib.GetClassName(windowUnderMouse, className, className.Capacity);

				string cls = className.ToString();
				if (cls == "SysListView32" || cls == "SHELLDLL_DefView" || cls == "WorkerW" || cls == "Progman")
				{
					Point clientPoint = webView.PointToClient(hookStruct.pt);
					IntPtr mousePosParam = Lib.MakeLParam(clientPoint.X, clientPoint.Y);
					uint msg = (uint)wParam;
					IntPtr interactionFlags = IntPtr.Zero;

					if (msg == Lib.WM_LBUTTONDOWN) interactionFlags = (IntPtr)0x0001;
					else if (msg == Lib.WM_MOUSEMOVE)
					{
						bool isLeftDown = (Lib.GetAsyncKeyState(0x01) & 0x8000) != 0;
						interactionFlags = isLeftDown ? (IntPtr)0x0001 : IntPtr.Zero;
					}

					if (msg == Lib.WM_LBUTTONDOWN || msg == Lib.WM_LBUTTONUP || msg == Lib.WM_MOUSEMOVE)
					{
						Lib.PostMessage(_chromeRenderWidgetHostHWND, msg, interactionFlags, mousePosParam);
					}
				}
			}
			catch { }
		}
		return Lib.CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
	}

	private void PinToDesktop()
	{
		IntPtr progman = Lib.FindWindow("Progman", null);
		if (progman == IntPtr.Zero) return;

		Lib.SendMessageTimeout(progman, 0x052C, new IntPtr(0), IntPtr.Zero, 0x0000, 1000, out _);

		IntPtr workerW = IntPtr.Zero;
		if (Environment.OSVersion.Version.Build >= 22000)
		{
			IntPtr child = IntPtr.Zero;
			do
			{
				child = Lib.FindWindowEx(progman, child, "WorkerW", null);
				if (child != IntPtr.Zero) { workerW = child; break; }
			} while (child != IntPtr.Zero);
		}
		else
		{
			IntPtr resultWorkerW = IntPtr.Zero;
			Lib.EnumWindows((toplevelHandle, param) =>
			{
				IntPtr shellView = Lib.FindWindowEx(toplevelHandle, IntPtr.Zero, "SHELLDLL_DefView", null);
				if (shellView != IntPtr.Zero) resultWorkerW = Lib.FindWindowEx(IntPtr.Zero, toplevelHandle, "WorkerW", null);
				return true;
			}, IntPtr.Zero);
			workerW = resultWorkerW;
		}

		if (workerW != IntPtr.Zero)
		{
			Lib.SetParent(this.Handle, workerW);
			int style = Lib.GetWindowLong(this.Handle, Lib.GWL_STYLE);
			style |= Lib.WS_CHILD;
			style &= ~Lib.WS_POPUP;
			Lib.SetWindowLong(this.Handle, Lib.GWL_STYLE, style);
			this.Location = new Point(0, 0);
			this.Size = currentScreen.Bounds.Size;
			Lib.SetWindowPos(this.Handle, Lib.HWND_BOTTOM, 0, 0, currentScreen.Bounds.Width, currentScreen.Bounds.Height, 0x0040 | 0x0010);
		}
	}
}