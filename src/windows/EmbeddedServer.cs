using System.Net;
using System.Text;
using System.Reflection;

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
            listener.Start();
            while (listener.IsListening)
            {
                var context = listener.GetContext();
                ProcessRequest(context);
            }
        }
        catch (HttpListenerException) { /* Ignored on stop */ }
    }

    public void Stop()
    {
        if (listener.IsListening)
        {
            listener.Stop();
            listener.Close();
        }
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
            // Generic Proxy logic (JSON, Image, etc.)
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
            catch { /* Ignore fetch errors */ }
        }
        else
        {
            // Serve Embedded Resource
            var assembly = Assembly.GetExecutingAssembly();
            string resourcePath = path.Replace('/', '.');
            
            var allResources = assembly.GetManifestResourceNames();
            var foundResource = allResources.FirstOrDefault(r => r.EndsWith("dist." + resourcePath, StringComparison.OrdinalIgnoreCase));

            if (foundResource != null)
            {
                using (var stream = assembly.GetManifestResourceStream(foundResource))
                {
                    if (stream != null)
                    {
                        using (var ms = new MemoryStream())
                        {
                            stream.CopyTo(ms);
                            responseData = ms.ToArray();
                        }
                    }
                }

                if (path.EndsWith(".js")) contentType = "application/javascript";
                else if (path.EndsWith(".css")) contentType = "text/css";
                else if (path.EndsWith(".svg")) contentType = "image/svg+xml";
            }
        }

        if (responseData != null)
        {
            context.Response.ContentType = contentType;
            context.Response.ContentLength64 = responseData.Length;
            await context.Response.OutputStream.WriteAsync(responseData, 0, responseData.Length);
            context.Response.OutputStream.Close();
        }
        else
        {
            context.Response.StatusCode = 404;
            context.Response.Close();
        }
    }
}
