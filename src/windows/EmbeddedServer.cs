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
            Console.WriteLine($"Server starting on {url}");
            
            string baseDir = AppContext.BaseDirectory;
            string frontendPath = Path.Combine(baseDir, "frontend", "dist");
            Console.WriteLine($"Frontend path: {frontendPath}");

            if (!Directory.Exists(frontendPath))
            {
                Console.WriteLine($"WARNING: Frontend directory NOT found at: {frontendPath}");
                // Also check one level up for development context
                string altPath = Path.Combine(baseDir, "..", "..", "..", "frontend", "dist");
                if (Directory.Exists(altPath))
                {
                    Console.WriteLine($"Found development frontend path: {altPath}");
                }
            }

            listener.Start();
            Console.WriteLine("Listener started successfully.");
            while (listener.IsListening)
            {
                var context = listener.GetContext();
                ProcessRequest(context);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FATAL Server Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
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

    private async void ProcessRequest(HttpListenerContext context)
    {
        string rawPath = context.Request.Url?.AbsolutePath ?? "/";
        string path = rawPath.TrimStart('/');
        if (string.IsNullOrEmpty(path)) path = "index.html";

        Console.WriteLine($"Request: {rawPath} -> {path}");

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
                    Console.WriteLine($"Proxying: {targetUrl}");
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
                Console.WriteLine($"Proxy Error: {ex.Message}");
            }
        }
        else
        {
            // Serve Physical File
            string baseDir = AppContext.BaseDirectory;
            string filePath = Path.Combine(baseDir, "frontend", "dist", path.Replace('/', Path.DirectorySeparatorChar));
            
            // For development fallback
            if (!File.Exists(filePath))
            {
                filePath = Path.Combine(baseDir, "..", "..", "..", "frontend", "dist", path.Replace('/', Path.DirectorySeparatorChar));
            }

            if (File.Exists(filePath))
            {
                try
                {
                    responseData = await File.ReadAllBytesAsync(filePath);
                    Console.WriteLine($"Serving file: {filePath}");

                    if (path.EndsWith(".js")) contentType = "application/javascript";
                    else if (path.EndsWith(".css")) contentType = "text/css";
                    else if (path.EndsWith(".svg")) contentType = "image/svg+xml";
                    else if (path.EndsWith(".png")) contentType = "image/png";
                    else if (path.EndsWith(".jpg") || path.EndsWith(".jpeg")) contentType = "image/jpeg";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading file {filePath}: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"File NOT found: {filePath}");
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
            catch (Exception ex)
            {
                Console.WriteLine($"Response Write Error: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine($"404 Not Found: {rawPath}");
            context.Response.StatusCode = 404;
            context.Response.Close();
        }
    }
}
