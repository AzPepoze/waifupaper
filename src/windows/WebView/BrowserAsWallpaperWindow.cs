using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Net.Http;
using System.Text.Json;

namespace BrowserAsWallpaper;

public class BrowserAsWallpaperWindow : Form
{
	private WebView2 webView;
	private Screen currentScreen;
	private string targetUrl;
	private string userAgent;

	private IntPtr _mouseHookID = IntPtr.Zero;
	private Lib.LowLevelMouseProc _proc;
	private IntPtr _chromeRenderWidgetHostHWND = IntPtr.Zero;

	public BrowserAsWallpaperWindow(Screen screen)
	{
		this.currentScreen = screen;
		
		var config = ConfigLoader.Load();
		this.targetUrl = config.url;
		this.userAgent = config.user_agent;

		Console.WriteLine($"[Window] Initializing for screen: {screen.DeviceName}");
		this.FormBorderStyle = FormBorderStyle.None;
		this.WindowState = FormWindowState.Normal;
		this.StartPosition = FormStartPosition.Manual;
		this.ShowInTaskbar = false;
		this.Location = screen.Bounds.Location;
		this.Size = screen.Bounds.Size;

		webView = null!;
		_proc = HookCallback;
		_mouseHookID = SetHook(_proc);
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
		_chromeRenderWidgetHostHWND = IntPtr.Zero;
		if (_mouseHookID != IntPtr.Zero) Lib.UnhookWindowsHookEx(_mouseHookID);
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
			webView = new WebView2();
			webView.Dock = DockStyle.Fill;
			webView.DefaultBackgroundColor = Color.Black;
			this.Controls.Add(webView);

			await webView.EnsureCoreWebView2Async();
			if (webView == null || webView.CoreWebView2 == null) return;

			webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
			webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
			
			if (!string.IsNullOrEmpty(userAgent))
			{
				webView.CoreWebView2.Settings.UserAgent = userAgent;
			}

			webView.CoreWebView2.Navigate(targetUrl);
			FindChromeWindow();
			PinToDesktop();
		}
		catch { }
	}

	private void FindChromeWindow()
	{
		if (webView == null || !webView.IsHandleCreated) return;
		Lib.EnumChildWindows(this.webView.Handle, (hWnd, lParam) =>
		{
			StringBuilder sb = new StringBuilder(256);
			Lib.GetClassName(hWnd, sb, sb.Capacity);
			if (sb.ToString() == "Chrome_RenderWidgetHostHWND")
			{
				_chromeRenderWidgetHostHWND = hWnd;
				return false;
			}
			return true;
		}, IntPtr.Zero);
	}

	private IntPtr SetHook(Lib.LowLevelMouseProc proc)
	{
		using (Process curProcess = Process.GetCurrentProcess())
		using (ProcessModule curModule = curProcess.MainModule!)
		{
			return Lib.SetWindowsHookEx(Lib.WH_MOUSE_LL, proc, Lib.GetModuleHandle(curModule.ModuleName), 0);
		}
	}

	private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
	{
		if (nCode >= 0 && _chromeRenderWidgetHostHWND != IntPtr.Zero && !this.IsDisposed && webView != null)
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
					if (msg == Lib.WM_LBUTTONDOWN || msg == Lib.WM_LBUTTONUP || msg == Lib.WM_MOUSEMOVE)
					{
						IntPtr flags = (msg == Lib.WM_LBUTTONDOWN || (msg == Lib.WM_MOUSEMOVE && (Lib.GetAsyncKeyState(0x01) & 0x8000) != 0)) ? (IntPtr)0x0001 : IntPtr.Zero;
						Lib.PostMessage(_chromeRenderWidgetHostHWND, msg, flags, mousePosParam);
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
			do { child = Lib.FindWindowEx(progman, child, "WorkerW", null); if (child != IntPtr.Zero) { workerW = child; break; } } while (child != IntPtr.Zero);
		}
		else
		{
			Lib.EnumWindows((toplevelHandle, param) => { if (Lib.FindWindowEx(toplevelHandle, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero) workerW = Lib.FindWindowEx(IntPtr.Zero, toplevelHandle, "WorkerW", null); return true; }, IntPtr.Zero);
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
