using System.Runtime.InteropServices;

namespace WaifuPaper;

public static class SharedLib
{
	[DllImport("kernel32.dll")]
	public static extern bool AttachConsole(int dwProcessId);
}