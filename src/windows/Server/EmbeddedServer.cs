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
	public bool IsRunning { get; private set; }

	public EmbeddedServer(string url)
	{
		this.url = url;
		listener = new HttpListener();
		listener.Prefixes.Add(url);
	}

	public void Start()
	{
		try
		{
			string baseDir = AppContext.BaseDirectory;
			string frontendPath = Path.Combine(baseDir, "frontend", "dist");

			if (!Directory.Exists(frontendPath))
			{
				string altPath = Path.Combine(baseDir, "..", "..", "..", "frontend", "dist");
			}

			listener.Start();
			IsRunning = true;
			while (listener.IsListening)
			{
				var context = listener.GetContext();
				ProcessRequest(context);
			}
		}
		catch (Exception ex)
		{
			IsRunning = false;
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
			IsRunning = false;
		}
		catch { IsRunning = false; }
	}

	private async void ProcessRequest(HttpListenerContext context)
	{
		string rawPath = context.Request.Url?.AbsolutePath ?? "/";
		string path = rawPath.TrimStart('/');
		if (string.IsNullOrEmpty(path)) path = "index.html";

		byte[]? responseData = null;
		string contentType = "text/html";

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
			catch { }
		}
		else
		{
			string baseDir = AppContext.BaseDirectory;
			string filePath = Path.Combine(baseDir, "frontend", "dist", path.Replace('/', Path.DirectorySeparatorChar));

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
				}
				catch { }
			}
		}

		if (responseData != null)
		{
			try
			{
				context.Response.ContentType = contentType;
				context.Response.ContentLength64 = responseData.Length;
				await context.Response.OutputStream.WriteAsync(responseData, 0, responseData.Length);
				context.Response.OutputStream.Close();
			}
			catch { }
		}
		else
		{
			context.Response.StatusCode = 404;
			context.Response.Close();
		}
	}
}