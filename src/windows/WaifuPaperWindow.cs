using System.Runtime.InteropServices;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace WaifuPaper;

public class WaifuPaperWindow : Form
{
    private WebView2 webView;
    private NotifyIcon trayIcon;
    private ContextMenuStrip trayMenu;
    private EmbeddedServer server;

    // Win32 APIs
    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    static extern IntPtr FindWindowEx(IntPtr hWndParent, IntPtr hWndChildAfter, string? lpszClass, string? lpszWindow);

    [DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
    const uint SWP_NOSIZE = 0x0001;
    const uint SWP_NOMOVE = 0x0002;
    const uint SWP_NOACTIVATE = 0x0010;

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    public WaifuPaperWindow()
    {
        this.FormBorderStyle = FormBorderStyle.None;
        this.WindowState = FormWindowState.Maximized;
        this.ShowInTaskbar = false;
        
        // Start Local Server
        server = new EmbeddedServer("http://127.0.0.1:43210/");
        Task.Run(() => server.Start());

        // Initialize UI Components
        webView = new WebView2();
        webView.Dock = DockStyle.Fill;
        this.Controls.Add(webView);

        trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("Exit", null, OnExit);

        trayIcon = new NotifyIcon();
        trayIcon.Text = "WaifuPaper";
        trayIcon.Icon = SystemIcons.Application; 
        trayIcon.ContextMenuStrip = trayMenu;
        trayIcon.Visible = true;
        
        InitializeAsync();
    }

    private void OnExit(object? sender, EventArgs e)
    {
        server.Stop();
        trayIcon.Visible = false;
        Application.Exit();
    }

    async void InitializeAsync()
    {
        try
        {
            await webView.EnsureCoreWebView2Async();
            webView.CoreWebView2.Navigate("http://127.0.0.1:43210/");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"WebView2 Initialization Error: {ex.Message}\n\nMake sure WebView2 Runtime is installed.", "WaifuPaper Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        
        // Pin to Desktop
        PinToDesktop();
    }

    private void PinToDesktop()
    {
        IntPtr progman = FindWindow("Progman", null);
        IntPtr result = IntPtr.Zero;
        SendMessageTimeout(progman, 0x052C, new IntPtr(0), IntPtr.Zero, 0x0000, 1000, out result);

        IntPtr workerW = IntPtr.Zero;
        EnumWindows(new EnumWindowsProc((tophandle, topparamhandle) =>
        {
            IntPtr p = FindWindowEx(tophandle, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (p != IntPtr.Zero)
            {
                workerW = FindWindowEx(IntPtr.Zero, tophandle, "WorkerW", null);
            }
            return true;
        }), IntPtr.Zero);

        if (workerW != IntPtr.Zero)
        {
            SetParent(this.Handle, workerW);
            this.Bounds = Screen.PrimaryScreen!.Bounds;
            this.Location = new Point(0, 0);
            
            // On Windows, reparenting to WorkerW often makes it non-interactive.
            // But for many users, this is the desired "True Wallpaper" behavior.
        }
    }
}
