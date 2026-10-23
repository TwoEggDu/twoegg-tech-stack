"""Check all rendered preview pages and their local links. No dependencies.

Usage: python previews/reading-experience/check_links.py BUILD_DIR [BASE_PATH] [ORIGIN]
BASE_PATH is /twoegg-tech-stack for the production baseURL, or empty for localhost.
ORIGIN optionally checks live HTTP responses, including Hugo's virtual livereload.js.
"""
from html.parser import HTMLParser
from pathlib import Path
from urllib.parse import unquote, urlsplit, urljoin, urldefrag
from urllib.request import build_opener, ProxyHandler
import json
import sys


class Page(HTMLParser):
    def __init__(self, path):
        super().__init__()
        self.links, self.assets, self.ids, self.h1 = [], [], set(), 0
        self.feed(path.read_text(encoding="utf-8"))

    def handle_starttag(self, tag, attrs):
        a = dict(attrs)
        if "id" in a:
            self.ids.add(a["id"])
        if tag == "h1":
            self.h1 += 1
        if tag == "a":
            self.links.append(a.get("href", ""))
        if tag in ("script", "link"):
            self.assets.append(a.get("src", a.get("href", "")))


root = Path(sys.argv[1]).resolve()
base = sys.argv[2].rstrip("/") if len(sys.argv) > 2 else "/twoegg-tech-stack"
origin = sys.argv[3].rstrip("/") if len(sys.argv) > 3 else ""
if origin and urlsplit(origin).hostname not in ("localhost", "127.0.0.1", "::1"):
    sys.exit("ORIGIN must be a local preview server")
# Local preview traffic should not enter a workstation's external HTTP proxy.
opener = build_opener(ProxyHandler({}))
routes = [str(p.parent.relative_to(root)).replace("\\", "/") + "/"
          for p in sorted((root / "preview").rglob("index.html"))]
if not routes:
    sys.exit("No preview pages found")
errors, targets, fragments, link_count = [], set(), 0, 0
http_urls = set()
for route in routes:
    document = root / route / "index.html"
    page = Page(document)
    if page.h1 != 1:
        errors.append(f"{route}: expected one H1, got {page.h1}")
    for href in page.links + page.assets:
        link_count += 1
        url = urlsplit(href)
        if url.scheme or url.netloc:
            continue
        if not href:
            errors.append(f"{route}: empty link")
            continue
        path = unquote(url.path)
        if origin:
            http_urls.add(urldefrag(urljoin(origin + "/" + route, href))[0])
            # The Hugo dev server injects this endpoint; it is not an output file.
            # Validate it over HTTP instead of pretending it is a static asset.
            if path == "/livereload.js":
                continue
        if not path:
            target = document
        else:
            if base and path.startswith(base + "/"):
                path = path[len(base):]
            target = root / path.lstrip("/") if path.startswith("/") else document.parent / path
            if target.is_dir() or path.endswith("/"):
                target = target / "index.html"
        if not target.is_file():
            errors.append(f"{route}: missing {href}")
            continue
        targets.add(str(target.relative_to(root)))
        if url.fragment:
            fragments += 1
            if unquote(url.fragment) not in Page(target).ids:
                errors.append(f"{route}: missing anchor {href}")
search = json.loads((root / "index.json").read_text(encoding="utf-8"))
if any("/preview/" in entry["href"] for entry in search):
    errors.append("Preview pages must not enter the article search index")
http_checked = 0
for url in sorted(http_urls):
    http_checked += 1
    try:
        with opener.open(url, timeout=5) as response:
            if response.status != 200:
                errors.append(f"HTTP {response.status}: {url}")
            response.read()
    except Exception as exc:
        errors.append(f"HTTP {url}: {exc}")
        break
print(json.dumps({"pages": len(routes), "links_and_assets": link_count,
                  "unique_targets": len(targets), "anchor_links": fragments,
                  "search_entries": len(search), "http_checked": http_checked,
                  "errors": errors}, ensure_ascii=False, indent=2))
sys.exit(bool(errors))
