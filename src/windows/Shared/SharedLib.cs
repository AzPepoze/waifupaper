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
	public string app_id { get; set; } = "com.azpepoze.browser-as-wallpaper";
	public string binary_name { get; set; } = "browser-as-wallpaper";
	public string user_agent { get; set; } = "";
}

public static class ConfigLoader
{
	public static AppConfig Load()
	{
		string configPath = Path.Combine(AppContext.BaseDirectory, "config.json");
		if (!File.Exists(configPath))
		{
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