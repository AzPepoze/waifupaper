using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace WaifuPaper;

public class WaifuPaperWindow : Form
{
	private static EmbeddedServer? sharedServer;
	private static readonly object serverLock = new object();
	private WebView2 webView;
	private Screen currentScreen;

	//-------------------------------------------------------
	// Input Forwarding Fields
	//-------------------------------------------------------
	private IntPtr _mouseHookID = IntPtr.Zero;
	private NativeMethods.LowLevelMouseProc _proc;
	private IntPtr _chromeRenderWidgetHostHWND = IntPtr.Zero;

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

		//-------------------------------------------------------
		// Initialize Input Hook
		//-------------------------------------------------------
		_proc = HookCallback;
		_mouseHookID = SetHook(_proc);
	}

	protected override bool ShowWithoutActivation => true;

	protected override void OnFormClosing(FormClosingEventArgs e)
	{
		NativeMethods.UnhookWindowsHookEx(_mouseHookID);
		base.OnFormClosing(e);
	}

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

			FindChromeWindow();
		}
		catch { }
	}

	//-------------------------------------------------------
	// Input Forwarding Logic
	//-------------------------------------------------------
	private void FindChromeWindow()
	{
		NativeMethods.EnumChildWindows(this.webView.Handle, (hWnd, lParam) =>
		{
			StringBuilder sb = new StringBuilder(256);
			NativeMethods.GetClassName(hWnd, sb, sb.Capacity);
			if (sb.ToString() == "Chrome_RenderWidgetHostHWND")
			{
				_chromeRenderWidgetHostHWND = hWnd;
				return false;
			}
			return true;
		}, IntPtr.Zero);
	}

	private IntPtr SetHook(NativeMethods.LowLevelMouseProc proc)
	{
		using (Process curProcess = Process.GetCurrentProcess())
		using (ProcessModule curModule = curProcess.MainModule!)
		{
			return NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, proc,
				NativeMethods.GetModuleHandle(curModule.ModuleName), 0);
		}
	}

	private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
	{
		if (nCode >= 0 && _chromeRenderWidgetHostHWND != IntPtr.Zero)
		{
			NativeMethods.MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);

			IntPtr windowUnderMouse = NativeMethods.WindowFromPoint(hookStruct.pt);
			StringBuilder className = new StringBuilder(256);
			NativeMethods.GetClassName(windowUnderMouse, className, className.Capacity);

			string cls = className.ToString();
			if (cls == "SysListView32" || cls == "SHELLDLL_DefView" || cls == "WorkerW" || cls == "Progman")
			{
				Point clientPoint = webView.PointToClient(hookStruct.pt);

				IntPtr mousePosParam = NativeMethods.MakeLParam(clientPoint.X, clientPoint.Y);

				uint msg = (uint)wParam;
				IntPtr interactionFlags = IntPtr.Zero;

				if (msg == NativeMethods.WM_LBUTTONDOWN)
				{
					interactionFlags = (IntPtr)0x0001;
				}
				else if (msg == NativeMethods.WM_LBUTTONUP)
				{
					interactionFlags = IntPtr.Zero;
				}
				else if (msg == NativeMethods.WM_MOUSEMOVE)
				{
					bool isLeftDown = (NativeMethods.GetAsyncKeyState(0x01) & 0x8000) != 0;
					interactionFlags = isLeftDown ? (IntPtr)0x0001 : IntPtr.Zero;
				}

				if (msg == NativeMethods.WM_LBUTTONDOWN ||
					msg == NativeMethods.WM_LBUTTONUP ||
					msg == NativeMethods.WM_MOUSEMOVE)
				{
					NativeMethods.PostMessage(_chromeRenderWidgetHostHWND, msg, interactionFlags, mousePosParam);
				}
			}
		}
		return NativeMethods.CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
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