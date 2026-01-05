using System.Runtime.InteropServices;

namespace BrowserAsWallpaper;

public static class SharedLib
{
	[DllImport("kernel32.dll")]
	public static extern bool AttachConsole(int dwProcessId);
}