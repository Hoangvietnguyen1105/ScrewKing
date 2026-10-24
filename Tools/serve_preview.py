from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from functools import partial
root=Path(__file__).resolve().parents[2]/"Builds/TikTok/BrowserRelease"
if not (root/"index.html").exists():raise SystemExit("Build the optimized browser preview first")
class PreviewHandler(SimpleHTTPRequestHandler):
    def end_headers(self):
        self.send_header("Cache-Control", "no-store")
        super().end_headers()

print("Preview: http://127.0.0.1:8769",flush=True)
ThreadingHTTPServer(("127.0.0.1",8769),partial(PreviewHandler,directory=str(root))).serve_forever()
