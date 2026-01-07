using System.Net;
using System.Text;
using System.Reflection;
using System.Windows.Forms;

namespace WaifuPaper;

public class EmbeddedServer
{
	private HttpListener listener;
	private string url;
	private HttpClient httpClient = new HttpClient();
	private string webRoot;

	public EmbeddedServer(string url)
	{
		this.url = url;
		listener = new HttpListener();
		listener.Prefixes.Add(url);
		
		string baseDir = AppContext.BaseDirectory;
		
		string releasePath = Path.Combine(baseDir, "frontend");
		
		string devPath = Path.Combine(baseDir, "..", "..", "..", "frontend", "dist");

		if (Directory.Exists(releasePath))
		{
			webRoot = Path.GetFullPath(releasePath);
		}
		else if (Directory.Exists(devPath))
		{
			webRoot = Path.GetFullPath(devPath);
		}
		else
		{
			// Fallback or error state, though we'll try to run anyway
			webRoot = Path.Combine(baseDir, "frontend");
		}
	}

	public void Start()
	{
		try
		{
			Console.WriteLine($"[Server] Starting at {url}");
			Console.WriteLine($"[Server] Serving files from {webRoot}");

			listener.Start();
			while (listener.IsListening)
			{
				var context = listener.GetContext();
				// Run request processing in a separate task to handle concurrent requests better
				Task.Run(() => ProcessRequest(context));
			}
		}
		catch (Exception ex)
		{
			// If it fails, we might be in a context where MessageBox isn't ideal, but keeping it for now
			// Logging to console is also helpful
			Console.WriteLine($"[Server] Error: {ex.Message}");
			MessageBox.Show($"Server failed to start: {ex.Message}", "WaifuPaper Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}

	public void Stop()
	{
		try
		{
			if (listener.IsListening)
			{
				listener.Stop();
				listener.Close();
			}
		}
		catch { }
	}

	private async Task ProcessRequest(HttpListenerContext context)
	{
		try
		{
			string rawPath = context.Request.Url?.AbsolutePath ?? "/";
			string path = rawPath.TrimStart('/');
			if (string.IsNullOrEmpty(path)) path = "index.html";

			byte[]? responseData = null;
			string contentType = "text/html";
			
			// CORS headers
			context.Response.AddHeader("Access-Control-Allow-Origin", "*");
			context.Response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
			context.Response.AddHeader("Access-Control-Allow-Headers", "Content-Type");

			if (context.Request.HttpMethod == "OPTIONS")
			{
				context.Response.StatusCode = 200;
				context.Response.Close();
				return;
			}

			if (rawPath.StartsWith("/api/proxy"))
			{
				try
				{
					var query = context.Request.QueryString;
					string? targetUrl = query["url"];
					if (!string.IsNullOrEmpty(targetUrl))
					{
						var response = await httpClient.GetAsync(targetUrl);
						if (response.IsSuccessStatusCode)
						{
							responseData = await response.Content.ReadAsByteArrayAsync();
							contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
						}
					}
				}
				catch (Exception ex) 
				{
					Console.WriteLine($"[Server] Proxy error: {ex.Message}");
				}
			}
			else
			{
				string filePath = Path.Combine(webRoot, path.Replace('/', Path.DirectorySeparatorChar));

				if (File.Exists(filePath))
				{
					try
					{
						responseData = await File.ReadAllBytesAsync(filePath);

						if (path.EndsWith(".js")) contentType = "application/javascript";
						else if (path.EndsWith(".css")) contentType = "text/css";
						else if (path.EndsWith(".svg")) contentType = "image/svg+xml";
						else if (path.EndsWith(".png")) contentType = "image/png";
						else if (path.EndsWith(".jpg") || path.EndsWith(".jpeg")) contentType = "image/jpeg";
						else if (path.EndsWith(".json")) contentType = "application/json";
					}
					catch { }
				}
				else
				{
					Console.WriteLine($"[Server] File not found: {filePath}");
				}
			}

			if (responseData != null)
			{
				context.Response.ContentType = contentType;
				context.Response.ContentLength64 = responseData.Length;
				await context.Response.OutputStream.WriteAsync(responseData, 0, responseData.Length);
			}
			else
			{
				context.Response.StatusCode = 404;
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[Server] Error processing request: {ex.Message}");
			context.Response.StatusCode = 500;
		}
		finally
		{
			context.Response.Close();
		}
	}
}