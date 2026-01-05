using System.Runtime.InteropServices;

namespace WaifuPaper;

static class Program
{
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool AllocConsole();

    [STAThread]
    static void Main()
    {
        AllocConsole();
        ApplicationConfiguration.Initialize();
        Application.Run(new WaifuPaperWindow());
    }
}
