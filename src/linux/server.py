import http.server
import socketserver
import urllib.request
import os
import sys

# Configuration
PORT = 49555
USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36"
# Switch to Yande.re (Same Moebooru API format)
API_URL = "https://yande.re/post.json?limit=1&tags=order:random+rating:safe"

# Locate Frontend Dist
# Checks local directory (Release mode) then parent directory (Dev mode)
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
FRONTEND_DIR = os.path.join(SCRIPT_DIR, "frontend", "dist")

if not os.path.exists(FRONTEND_DIR):
    FRONTEND_DIR = os.path.join(os.path.dirname(SCRIPT_DIR), "frontend", "dist")


class WaifuHandler(http.server.SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        # Serve files from the frontend build directory
        super().__init__(*args, directory=FRONTEND_DIR, **kwargs)

    def do_GET(self):
        if self.path.startswith("/api/proxy"):
            self.handle_proxy()
        else:
            super().do_GET()

    def handle_proxy(self):
        """Proxies a request (JSON or Image)."""
        from urllib.parse import urlparse, parse_qs

        query = parse_qs(urlparse(self.path).query)
        target_url = query.get("url", [None])[0]

        if not target_url:
            self.send_error(400, "Missing 'url' parameter")
            return

        try:
            req = urllib.request.Request(target_url, headers={"User-Agent": USER_AGENT})
            with urllib.request.urlopen(req) as response:
                content_type = response.headers.get("Content-Type", "application/octet-stream")
                self.send_response(200)
                self.send_header("Content-type", content_type)
                self.end_headers()
                self.wfile.write(response.read())
        except Exception as e:
            print(f"[Server] Proxy Error for {target_url}: {e}")
            self.send_error(500, "Failed to fetch resource")

    def log_message(self, format, *args):
        # Enable logging for debugging
        sys.stderr.write("%s - - [%s] %s\n" % (self.client_address[0], self.log_date_time_string(), format % args))


def start_server():
    """Starts the TCP server on the defined port."""
    print(f"[Server] Serving files from: {FRONTEND_DIR}")
    try:
        # allow_reuse_address allows restarting quickly without waiting for timeout
        socketserver.TCPServer.allow_reuse_address = True
        with socketserver.TCPServer(("", PORT), WaifuHandler) as httpd:
            # print(f"[Server] Serving at port {PORT}")
            httpd.serve_forever()
    except OSError as e:
        print(f"[Server] Failed to start on port {PORT}: {e}")
        sys.exit(1)


if __name__ == "__main__":
    start_server()
