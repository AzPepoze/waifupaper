using System.Windows.Forms;
using System.Drawing;

namespace BrowserAsWallpaper;

public static class Tray
{
    public static NotifyIcon CreateTray(string text, EventHandler onExit)
    {
        ContextMenuStrip trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("Exit " + text, null, onExit);

        NotifyIcon trayIcon = new NotifyIcon();
        trayIcon.Text = text;
        trayIcon.Icon = SystemIcons.Application;
        trayIcon.ContextMenuStrip = trayMenu;
        trayIcon.Visible = true;
        
        return trayIcon;
    }
}