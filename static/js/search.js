(() => {
  const searchRoot = document.querySelector('[data-search]');
  if (!searchRoot) {
    return;
  }

  const searchInput = searchRoot.querySelector('.site-search-input');
  const searchPanel = searchRoot.querySelector('[data-search-panel]');
  const searchStatus = searchRoot.querySelector('[data-search-status]');
  const searchResults = searchRoot.querySelector('[data-search-results]');
  const searchIndexUrl = searchRoot.dataset.searchIndexUrl;

  if (!searchInput || !searchPanel || !searchStatus || !searchResults || !searchIndexUrl) {
    return;
  }

  let searchIndex = [];
  let searchIndexPromise = null;
  let latestRenderToken = 0;
  let latestResults = [];

  const normalize = (value = '') =>
    value
      .toString()
      .toLowerCase()
      .normalize('NFKC')
      .replace(/\s+/g, ' ')
      .trim();

  const setStatus = (message, isError = false) => {
    searchStatus.textContent = message;
    searchStatus.hidden = false;
    searchStatus.classList.toggle('is-error', isError);
  };

  const clearResults = () => {
    latestResults = [];
    searchResults.replaceChildren();
    searchResults.hidden = true;
  };

  const openPanel = () => {
    searchRoot.classList.add('is-open');
    searchPanel.hidden = false;
    searchInput.setAttribute('aria-expanded', 'true');
  };

  const closePanel = () => {
    searchRoot.classList.remove('is-open');
    searchPanel.hidden = true;
    searchInput.setAttribute('aria-expanded', 'false');
  };

  const buildSearchIndex = (entries) =>
    entries.map((entry) => {
      const tags = Array.isArray(entry.tags) ? entry.tags.filter(Boolean) : [];
      const title = entry.title || '';
      const section = entry.section || '';
      const summary = entry.summary || '';
      const content = entry.content || '';
      const tagsNorm = tags.map(normalize);

      return {
        ...entry,
        tags,
        title,
        section,
        summary,
        content,
        tagsNorm,
        titleNorm: normalize(title),
        sectionNorm: normalize(section),
        summaryNorm: normalize(summary),
        contentNorm: normalize(content),
      };
    });

  const loadSearchIndex = async () => {
    if (searchIndex.length > 0) {
      return searchIndex;
    }

    if (!searchIndexPromise) {
      searchIndexPromise = fetch(searchIndexUrl, {
        headers: { Accept: 'application/json' },
      })
        .then((response) => {
          if (!response.ok) {
            throw new Error('Search index request failed.');
          }

          return response.json();
        })
        .then((entries) => {
          searchIndex = buildSearchIndex(Array.isArray(entries) ? entries : []);
          return searchIndex;
        })
        .catch((error) => {
          searchIndexPromise = null;
          throw error;
        });
    }

    return searchIndexPromise;
  };

  const scoreEntry = (entry, query, tokens) => {
    let score = 0;
    const tagBlob = entry.tagsNorm.join(' ');

    for (const token of tokens) {
      let tokenScore = 0;

      if (entry.titleNorm === token) {
        tokenScore = 140;
      } else if (entry.titleNorm.startsWith(token)) {
        tokenScore = 110;
      } else if (entry.titleNorm.includes(token)) {
        tokenScore = 84;
      }

      if (tagBlob.includes(token)) {
        tokenScore = Math.max(tokenScore, 62);
      }

      if (entry.sectionNorm.includes(token)) {
        tokenScore = Math.max(tokenScore, 34);
      }

      if (entry.summaryNorm.includes(token)) {
        tokenScore = Math.max(tokenScore, 26);
      }

      if (entry.contentNorm.includes(token)) {
        tokenScore = Math.max(tokenScore, 14);
      }

      if (tokenScore === 0) {
        return -1;
      }

      score += tokenScore;
    }

    if (entry.titleNorm.includes(query)) {
      score += 18;
    }

    if (entry.summaryNorm.includes(query)) {
      score += 9;
    }

    return score;
  };

  const searchEntries = (query) => {
    const tokens = Array.from(new Set(query.split(' ').filter(Boolean)));

    return searchIndex
      .map((entry) => ({ entry, score: scoreEntry(entry, query, tokens) }))
      .filter(({ score }) => score > -1)
      .sort((left, right) => right.score - left.score || left.entry.title.localeCompare(right.entry.title))
      .slice(0, 8)
      .map(({ entry }) => entry);
  };

  const renderResultItem = (entry) => {
    const item = document.createElement('li');
    item.className = 'search-result-item';

    const link = document.createElement('a');
    link.className = 'search-result-link';
    link.href = entry.href;

    const meta = document.createElement('div');
    meta.className = 'search-result-meta';

    if (entry.section) {
      const section = document.createElement('span');
      section.className = 'search-result-section';
      section.textContent = entry.section;
      meta.append(section);
    }

    if (meta.childNodes.length > 0) {
      link.append(meta);
    }

    const title = document.createElement('strong');
    title.className = 'search-result-title';
    title.textContent = entry.title;
    link.append(title);

    if (entry.summary) {
      const summary = document.createElement('p');
      summary.className = 'search-result-summary';
      summary.textContent = entry.summary;
      link.append(summary);
    }

    if (entry.tags.length > 0) {
      const tagList = document.createElement('div');
      tagList.className = 'search-tag-list';

      entry.tags.slice(0, 4).forEach((tag) => {
        const tagItem = document.createElement('span');
        tagItem.className = 'tag';
        tagItem.textContent = tag;
        tagList.append(tagItem);
      });

      link.append(tagList);
    }

    item.append(link);
    return item;
  };

  const renderSearchResults = async () => {
    const query = normalize(searchInput.value);
    const renderToken = ++latestRenderToken;

    openPanel();

    if (!query) {
      clearResults();
      setStatus('\u8f93\u5165\u5173\u952e\u8bcd\u641c\u7d22\u6587\u7ae0\u3001\u6807\u7b7e\u548c\u680f\u76ee\u3002');
      return;
    }

    if (query.length < 2) {
      clearResults();
      setStatus('\u81f3\u5c11\u8f93\u5165 2 \u4e2a\u5b57\u7b26\u518d\u5f00\u59cb\u641c\u7d22\u3002');
      return;
    }

    if (!searchIndex.length) {
      clearResults();
      setStatus('\u6b63\u5728\u52a0\u8f7d\u641c\u7d22\u7d22\u5f15...');
    }

    try {
      await loadSearchIndex();
    } catch (error) {
      if (renderToken !== latestRenderToken) {
        return;
      }

      clearResults();
      setStatus('\u641c\u7d22\u7d22\u5f15\u52a0\u8f7d\u5931\u8d25\uff0c\u8bf7\u7a0d\u540e\u91cd\u8bd5\u3002', true);
      return;
    }

    if (renderToken !== latestRenderToken) {
      return;
    }

    latestResults = searchEntries(query);
    searchResults.replaceChildren(...latestResults.map(renderResultItem));
    searchResults.hidden = latestResults.length === 0;

    if (latestResults.length === 0) {
      setStatus('\u6ca1\u6709\u627e\u5230\u76f8\u5173\u5185\u5bb9\uff0c\u53ef\u4ee5\u6362\u4e00\u4e2a\u5173\u952e\u8bcd\u8bd5\u8bd5\u3002');
      return;
    }

    setStatus(`\u627e\u5230 ${latestResults.length} \u6761\u7ed3\u679c\uff0c\u6309\u76f8\u5173\u6027\u6392\u5e8f\u3002`);
  };

  searchInput.addEventListener('focus', () => {
    openPanel();

    if (!normalize(searchInput.value)) {
      setStatus('\u8f93\u5165\u5173\u952e\u8bcd\u641c\u7d22\u6587\u7ae0\u3001\u6807\u7b7e\u548c\u680f\u76ee\u3002');
    }

    if (!searchIndex.length && !searchIndexPromise) {
      loadSearchIndex().catch(() => {
        if (searchRoot.classList.contains('is-open')) {
          setStatus('\u641c\u7d22\u7d22\u5f15\u52a0\u8f7d\u5931\u8d25\uff0c\u8bf7\u7a0d\u540e\u91cd\u8bd5\u3002', true);
        }
      });
    }
  });

  searchInput.addEventListener('input', () => {
    renderSearchResults();
  });

  searchRoot.addEventListener('submit', (event) => {
    event.preventDefault();

    if (!normalize(searchInput.value)) {
      closePanel();
      return;
    }

    if (latestResults[0]) {
      window.location.assign(latestResults[0].href);
      return;
    }

    renderSearchResults();
  });

  searchInput.addEventListener('keydown', (event) => {
    if (event.key === 'Escape') {
      closePanel();
      searchInput.blur();
    }
  });

  document.addEventListener('pointerdown', (event) => {
    if (!searchRoot.contains(event.target)) {
      closePanel();
    }
  });
})();