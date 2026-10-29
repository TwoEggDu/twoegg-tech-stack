"""Build real Hugo pages in an isolated fixture; no third-party Python packages."""
import importlib.util
import json
import re
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def checker():
    spec = importlib.util.spec_from_file_location('reading_check', ROOT / 'scripts/check-reading-pages.py')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def content(root, path, **meta):
    target = root / 'content' / path
    target.parent.mkdir(parents=True, exist_ok=True)
    body = meta.pop('body', '## A heading\n\nA paragraph.\n')
    target.write_text('---\n' + '\n'.join(
        f'{key}: {json.dumps(value, ensure_ascii=False)}' for key, value in meta.items()
    ) + '\n---\n\n' + body, encoding='utf-8')
    return target


def hugo(root, *args):
    return subprocess.run(['hugo', '--source', str(root), *args],
                          capture_output=True, text=True, encoding='utf-8')


def fixture(root):
    shutil.copytree(ROOT / 'layouts', root / 'layouts')
    shutil.copytree(ROOT / 'static', root / 'static')
    shutil.copytree(ROOT / 'archetypes', root / 'archetypes')
    # The homepage is out of scope; its real template needs the entire content site.
    (root / 'layouts/index.html').write_text('<!doctype html><title>Home</title>', encoding='utf-8')
    (root / 'layouts/index.json').write_text('[]', encoding='utf-8')
    (root / 'hugo.toml').write_text(
        'baseURL = "https://example.test/demo/"\ntitle = "Fixture"\n'
        'disableKinds = ["taxonomy", "term", "RSS", "sitemap"]\n'
        '[markup.goldmark.renderer]\nunsafe = true\n', encoding='utf-8')
    (root / 'data').mkdir()
    topics = [{'key': key, 'title': title, 'page': f'reading/{key}.md'}
              for key, title in [('delivery', '工程与交付'), ('engine', '引擎研发')]]
    (root / 'data/reading_paths.json').write_text(json.dumps({'topics': topics}), encoding='utf-8')
    for topic in topics:
        content(root, topic['page'], title=topic['title'], type='reading-guide',
                reading_kind='topic', topic_key=topic['key'])
    for slug in ('technical-topics', 'game-development', 'about'):
        content(root, f'{slug}.md', title=slug)
    content(root, 'engineering-journey/_index.md', title='Special section landing')
    content(root, 'delivery-engineering/plain.md', title='Plain article')
    content(root, 'delivery-engineering/short.md', title='Short article', body='Just a sentence.')
    content(root, 'essays/standalone.md', title='Standalone')
    content(root, 'delivery-engineering/book.md', title='Book', series='Book',
            series_id='book', series_role='index', series_title='Book', topic_key='delivery',
            body='## Chapters {#chapters}\n\n{{< reading-series-chapters >}}',
            reading_groups=[{'title': 'Read', 'intro': 'Start here', 'first': 0, 'last': 1}])
    content(root, 'delivery-engineering/zero.md', title='Chapter zero', series='Book',
            primary_series='book', series_order=0)
    content(root, 'delivery-engineering/one.md', title='Chapter one', series='Book',
            primary_series='book', series_order=1, type='reading-series')
    content(root, 'delivery-engineering/old-index.md', title='Legacy', series='Legacy',
            series_id='legacy', series_role='index', series_title='Legacy')
    content(root, 'delivery-engineering/old.md', title='Old chapter', series='Legacy', weight=10)
    content(root, 'delivery-engineering/name-only-index.md', title='Name only',
            series='Name only', series_role='index')
    content(root, 'delivery-engineering/name-only.md', title='Name only chapter',
            series='Name only', weight=10)
    content(root, 'delivery-engineering/orphan.md', title='No index yet', primary_series='future')
    content(root, 'engine-toolchain/il2cpp-series-index.md', title='IL2CPP', series_id='il2cpp-index',
            series_title='IL2CPP', topic_key='engine')
    content(root, 'engine-toolchain/il2cpp-example.md', title='Runtime example')
    content(root, 'engine-toolchain/legacy-module.md', title='Legacy module',
            series_id='module', series='Book')


class ReadingLayouts(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.temp = tempfile.TemporaryDirectory(prefix='reading-layouts-')
        cls.root = Path(cls.temp.name)
        fixture(cls.root)
        result = hugo(cls.root)
        if result.returncode:
            raise AssertionError(result.stdout + result.stderr)

    @classmethod
    def tearDownClass(cls):
        cls.temp.cleanup()

    def page(self, route):
        return (self.root / 'public' / route / 'index.html').read_text(encoding='utf-8')

    def test_default_article_uses_reading_style_without_type(self):
        page = self.page('delivery-engineering/plain')
        self.assertIn('reading-site', page)
        self.assertIn('series-reading.css', page)
        self.assertIn('reading-article-header', page)
        self.assertIn('aria-label="面包屑"', page)
        self.assertIn('href="/demo/reading/delivery/"', page)
        self.assertNotIn('class="article-back"', page)
        self.assertNotIn('00 / 00', page)

    def test_default_series_article_preserves_zero_and_neighbors(self):
        page = self.page('delivery-engineering/zero')
        self.assertIn('第 00 篇', page)
        self.assertIn('00 / 02', page)
        self.assertIn('href="/demo/delivery-engineering/one/"', page)
        self.assertIn('href="/demo/delivery-engineering/book/#chapters"', page)
        self.assertIn('reading-toc-desktop', page)

    def test_explicit_series_keeps_same_markup(self):
        page = self.page('delivery-engineering/one')
        self.assertIn('reading-site', page)
        self.assertIn('01 / 02', page)
        self.assertIn('href="/demo/delivery-engineering/zero/"', page)

    def test_default_series_index_uses_cover(self):
        page = self.page('delivery-engineering/book')
        self.assertIn('series-cover', page)
        self.assertIn('id="chapters"', page)

    def test_legacy_name_resolves_index_without_fabricating_anchor(self):
        page = self.page('delivery-engineering/old')
        self.assertIn('href="/demo/delivery-engineering/old-index/"', page)
        self.assertNotIn('old-index/#chapters', page)

    def test_standalone_and_short_article_have_valid_navigation(self):
        standalone = self.page('essays/standalone')
        self.assertIn('reading-site', standalone)
        self.assertIn('href="/demo/technical-topics/"', standalone)
        self.assertNotIn('第 00 篇', standalone)
        self.assertNotIn('reading-toc-mobile', self.page('delivery-engineering/short'))

    def test_legacy_index_without_id_only_contains_its_own_series(self):
        page = self.page('delivery-engineering/name-only')
        self.assertIn('01 / 01', page)
        self.assertIn('href="/demo/delivery-engineering/name-only-index/"', page)
        self.assertNotIn('href="/demo/delivery-engineering/plain/"', page)
        self.assertNotIn('href="/demo/delivery-engineering/old/"', page)

    def test_unknown_legacy_series_still_uses_reading_style(self):
        page = self.page('delivery-engineering/orphan')
        self.assertIn('reading-site', page)
        self.assertNotIn('第 00 篇', page)

    def test_runtime_articles_keep_engine_and_index(self):
        page = self.page('engine-toolchain/il2cpp-example')
        self.assertIn('href="/demo/reading/engine/"', page)
        self.assertIn('href="/demo/engine-toolchain/il2cpp-series-index/"', page)

    def test_legacy_series_id_is_not_treated_as_primary_series(self):
        page = self.page('engine-toolchain/legacy-module')
        self.assertIn('reading-site', page)
        self.assertNotIn('第 00 篇', page)
        self.assertNotIn('href="/demo/delivery-engineering/book/"', page)

    def test_new_default_archetype_is_draft_and_builds(self):
        result = hugo(self.root, 'new', 'content', 'delivery-engineering/new-default.md')
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        path = self.root / 'content/delivery-engineering/new-default.md'
        self.assertIn('draft: true', path.read_text(encoding='utf-8'))
        result = hugo(self.root, '--buildDrafts')
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertIn('reading-site', self.page('delivery-engineering/new-default'))

    def test_publication_check_accepts_generated_pages(self):
        count, errors = checker().check_site(self.root / 'public', 'https://example.test/demo/')
        self.assertGreater(count, 5)
        self.assertEqual(errors, [])

    def test_publication_check_rejects_legacy_style_and_broken_anchor(self):
        path = self.root / 'public/delivery-engineering/zero/index.html'
        original = path.read_text(encoding='utf-8')
        try:
            path.write_text(original.replace('series-reading.css', 'missing.css')
                            .replace('book/#chapters', 'book/#absent'), encoding='utf-8')
            _, errors = checker().check_site(self.root / 'public', 'https://example.test/demo/')
            self.assertTrue(any('missing stylesheet: series-reading.css' in x for x in errors))
            self.assertTrue(any('missing anchor:' in x for x in errors))
            path.write_text('<article class="article article-reading">Old template</article>', encoding='utf-8')
            _, errors = checker().check_site(self.root / 'public', 'https://example.test/demo/')
            self.assertTrue(any('legacy article layout' in x for x in errors))
        finally:
            path.write_text(original, encoding='utf-8')


class InvalidSeries(unittest.TestCase):
    def test_position_numbering_preserves_fractional_insertion_order(self):
        with tempfile.TemporaryDirectory(prefix='reading-position-') as folder:
            root = Path(folder)
            fixture(root)
            content(root, 'delivery-engineering/book.md', title='Book', series='Book',
                    series_id='book', series_role='index', type='reading-series',
                    reading_number_by_position=True)
            content(root, 'delivery-engineering/inserted.md', title='Inserted',
                    type='reading-series', primary_series='book', series_order=1.5)
            result = hugo(root)
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            page = (root / 'public/delivery-engineering/inserted/index.html').read_text(encoding='utf-8')
            self.assertIn('03 / 03', page)
            self.assertIn('href="/demo/delivery-engineering/one/"', page)

    def test_strict_chapter_rejects_duplicate_order_with_default_index(self):
        with tempfile.TemporaryDirectory(prefix='reading-duplicate-') as folder:
            root = Path(folder)
            fixture(root)
            content(root, 'delivery-engineering/duplicate.md', title='Duplicate',
                    type='reading-series', primary_series='book', series_order=1)
            result = hugo(root)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn('Duplicate reading series_order', result.stdout + result.stderr)

    def test_strict_chapter_rejects_non_integer_or_negative_order(self):
        for order in (1.5, -1, 'one'):
            with self.subTest(order=order), tempfile.TemporaryDirectory(prefix='reading-order-') as folder:
                root = Path(folder)
                fixture(root)
                content(root, 'delivery-engineering/bad-order.md', title='Bad order',
                        type='reading-series', primary_series='book', series_order=order)
                result = hugo(root)
                self.assertNotEqual(result.returncode, 0)
                self.assertIn('Reading series_order must be a non-negative integer',
                              result.stdout + result.stderr)

    def test_series_archetypes_create_working_chapters(self):
        with tempfile.TemporaryDirectory(prefix='reading-archetypes-') as folder:
            root = Path(folder)
            fixture(root)
            for kind, path in [('series-index', 'delivery-engineering/new-book.md'),
                               ('series-article', 'delivery-engineering/new-chapter.md')]:
                result = hugo(root, 'new', 'content', '--kind', kind, path)
                self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
                created = root / 'content' / path
                raw = created.read_text(encoding='utf-8')
                self.assertIn('draft: true', raw)
                created.write_text(raw.replace('replace-with-series-id', 'new-book'), encoding='utf-8')
            result = hugo(root, '--buildDrafts')
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            index = (root / 'public/delivery-engineering/new-book/index.html').read_text(encoding='utf-8')
            self.assertIn('reading-chapters', index)
            self.assertIn('/demo/delivery-engineering/new-chapter/', index)

    def test_explicit_series_missing_index_fails(self):
        with tempfile.TemporaryDirectory(prefix='reading-invalid-') as folder:
            root = Path(folder)
            fixture(root)
            content(root, 'delivery-engineering/bad.md', title='Bad',
                    type='reading-series', primary_series='absent', series_order=1)
            result = hugo(root)
            self.assertNotEqual(result.returncode, 0)
            self.assertRegex(result.stdout + result.stderr, '(?i)reading.*index')


if __name__ == '__main__':
    unittest.main()
