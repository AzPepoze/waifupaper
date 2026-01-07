using System.Runtime.InteropServices;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using System;

namespace BrowserAsWallpaper;

public class AppConfig
{
	public string url { get; set; } = "https://google.com";
	public string app_name { get; set; } = "BrowserAsWallpaper";
	public string user_agent { get; set; } = "";
}

public static class ConfigLoader
{
	public static AppConfig Load()
	{
		string configPath = Path.Combine(AppContext.BaseDirectory, "config.json");
		if (!File.Exists(configPath))
		{
			// Try src folder (relative to bin/Debug/net8.0-windows/win-x64)
			string devPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "config.json");
			if (File.Exists(devPath)) configPath = devPath;
		}

		if (File.Exists(configPath))
		{
			try
			{
				string jsonString = File.ReadAllText(configPath);
				var config = JsonSerializer.Deserialize<AppConfig>(jsonString);
				return config ?? new AppConfig();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[Config] Error loading config: {ex.Message}");
			}
		}
		return new AppConfig();
	}
}

public static class SharedLib
{
	[DllImport("kernel32.dll")]
	public static extern bool AttachConsole(int dwProcessId);
}