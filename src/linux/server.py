import http.server
import socketserver
import urllib.request
import os
import sys
from constants import PORT, USER_AGENT

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
FRONTEND_DIR = os.path.join(SCRIPT_DIR, "frontend", "dist")

if not os.path.exists(FRONTEND_DIR):
    FRONTEND_DIR = os.path.join(os.path.dirname(SCRIPT_DIR), "frontend", "dist")

class WaifuHandler(http.server.SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=FRONTEND_DIR, **kwargs)

    def do_GET(self):
        if self.path.startswith("/api/proxy"):
            self.handle_proxy()
        else:
            super().do_GET()

    def handle_proxy(self):
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
        sys.stderr.write("%s - - [%s] %s\n" % (self.client_address[0], self.log_date_time_string(), format % args))

def start_server():
    print(f"[Server] Serving files from: {FRONTEND_DIR}")
    try:
        socketserver.TCPServer.allow_reuse_address = True
        with socketserver.TCPServer(("", PORT), WaifuHandler) as httpd:
            httpd.serve_forever()
    except OSError as e:
        print(f"[Server] Failed to start on port {PORT}: {e}")
        sys.exit(1)

if __name__ == "__main__":
    start_server()
