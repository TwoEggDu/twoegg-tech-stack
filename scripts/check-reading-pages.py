"""Validate generated reading pages before publishing (Python standard library only)."""
import argparse
from html.parser import HTMLParser
from pathlib import Path
import sys
import tomllib
from urllib.parse import unquote, urljoin, urlsplit

VOID = {'area', 'base', 'br', 'col', 'embed', 'hr', 'img', 'input', 'link', 'meta',
        'param', 'source', 'track', 'wbr'}
NAV_CLASSES = {'breadcrumb', 'reading-pagination', 'reading-series-back',
               'reading-article-aside', 'reading-toc-mobile', 'reading-chapters'}


class Page(HTMLParser):
    def __init__(self, text):
        super().__init__(convert_charrefs=True)
        self.stack = []
        self.classes = set()
        self.ids = set()
        self.styles = []
        self.navigation = []
        self.canonical = ''
        self.reading = False
        self.legacy = False
        self.breadcrumb = False
        self.header_h1 = 0
        self.body_classes = set()
        self.kind = ''
        self.feed(text)
        self.close()

    def handle_starttag(self, tag, attrs):
        attrs = dict(attrs)
        classes = set(attrs.get('class', '').split())
        self.classes.update(classes)
        if attrs.get('id'):
            self.ids.add(attrs['id'])
        if tag == 'body':
            self.body_classes = classes
            self.reading = attrs.get('data-reading-page') in ('article', 'series')
        if tag == 'article':
            self.reading |= 'reading-article' in classes
            self.legacy |= 'article' in classes
        parent_nav = self.stack[-1][1] if self.stack else False
        parent_header = self.stack[-1][2] if self.stack else False
        nav = parent_nav or bool(classes & NAV_CLASSES)
        header = parent_header or bool(classes & {'reading-article-header', 'series-cover'})
        if tag == 'nav' and 'breadcrumb' in classes:
            self.breadcrumb = attrs.get('aria-label') == '面包屑'
        if tag == 'h1' and header:
            self.header_h1 += 1
        if tag == 'a' and nav:
            self.navigation.append(attrs.get('href', ''))
        if tag == 'link' and attrs.get('rel') == 'stylesheet':
            self.styles.append(attrs.get('href', ''))
        if tag == 'link' and attrs.get('rel') == 'canonical':
            self.canonical = attrs.get('href', '')
        if tag == 'meta' and attrs.get('name') == 'hugo-kind':
            self.kind = attrs.get('content', '')
        if tag not in VOID:
            self.stack.append((tag, nav, header))

    def handle_endtag(self, tag):
        for i in range(len(self.stack) - 1, -1, -1):
            if self.stack[i][0] == tag:
                del self.stack[i:]
                break


def check_site(public, base_url):
    public = Path(public).resolve()
    base = urlsplit(base_url)
    prefix = unquote(base.path).rstrip('/') + '/'
    errors = []
    checked = 0
    cache = {}

    def parse(path):
        if path not in cache:
            cache[path] = Page(path.read_text(encoding='utf-8'))
        return cache[path]

    def target(href, current):
        url = urlsplit(urljoin(current, href))
        if url.scheme not in ('http', 'https') or url.netloc != base.netloc:
            raise ValueError(f'navigation must stay on this site: {href}')
        path = unquote(url.path)
        if path == prefix.rstrip('/'):
            path += '/'
        if not path.startswith(prefix):
            raise ValueError(f'link leaves deployment prefix {prefix}: {href}')
        local = (public / path[len(prefix):]).resolve()
        if not local.is_relative_to(public):
            raise ValueError(f'link leaves output directory: {href}')
        if local.is_dir():
            local /= 'index.html'
        if not local.is_file():
            raise ValueError(f'missing target: {href}')
        return local, unquote(url.fragment)

    for path in sorted(public.rglob('*.html')):
        try:
            page = parse(path)
        except (UnicodeError, OSError) as error:
            errors.append(f'{path.relative_to(public)}: cannot read generated HTML: {error}')
            continue
        if page.kind in ('home', 'section', 'taxonomy', 'term') or not (page.reading or page.legacy):
            continue
        checked += 1
        label = str(path.relative_to(public)).replace('\\', '/')
        problems = []
        if page.legacy or 'article-back' in page.classes:
            problems.append('legacy article layout / return link')
        if 'reading-site' not in page.body_classes:
            problems.append('missing reading-site body')
        if not page.breadcrumb:
            problems.append('missing breadcrumb')
        if page.header_h1 != 1:
            problems.append('reading header must contain exactly one h1')
        if 'main' not in page.ids:
            problems.append('missing main landmark target')
        if not page.canonical:
            problems.append('missing canonical URL')
        style_names = {Path(urlsplit(x).path).name for x in page.styles}
        for required in ('reading.css', 'site-masthead.css', 'series-reading.css'):
            if required not in style_names:
                problems.append(f'missing stylesheet: {required}')
        for href in page.styles + page.navigation:
            try:
                if not href:
                    raise ValueError('empty navigation target')
                dest, anchor = target(href, page.canonical or base_url)
                if anchor and anchor not in parse(dest).ids:
                    raise ValueError(f'missing anchor: {href}')
            except (ValueError, OSError, UnicodeError) as error:
                problems.append(str(error))
        errors.extend(f'{label}: {problem}' for problem in dict.fromkeys(problems))
    if not checked:
        errors.append('No reading/article pages found; check the build output directory.')
    return checked, errors


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--public', type=Path, default=Path('public'))
    parser.add_argument('--base-url', help='Must match the Hugo build baseURL, including subpath.')
    args = parser.parse_args()
    base_url = args.base_url
    if not base_url:
        with Path('hugo.toml').open('rb') as source:
            base_url = tomllib.load(source)['baseURL']
    count, errors = check_site(args.public, base_url)
    for error in errors:
        print(f'ERROR {error}', file=sys.stderr)
    print(f'Reading pages: {count}; errors: {len(errors)}')
    return bool(errors)


if __name__ == '__main__':
    raise SystemExit(main())
