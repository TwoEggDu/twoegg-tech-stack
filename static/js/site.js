(() => {
  const body = document.body;
  const header = document.querySelector('.site-header');
  if (!body || !header) {
    return;
  }

  let ticking = false;

  const syncHeaderState = () => {
    body.classList.toggle('has-scrolled', window.scrollY > 18);
    ticking = false;
  };

  window.addEventListener(
    'scroll',
    () => {
      if (!ticking) {
        window.requestAnimationFrame(syncHeaderState);
        ticking = true;
      }
    },
    { passive: true }
  );

  syncHeaderState();
})();

(() => {
  document.querySelectorAll('.article-reading .article-body table').forEach((table) => {
    if (table.parentElement?.classList.contains('article-table')) {
      return;
    }

    const headers = Array.from(table.querySelectorAll('thead th'));
    const wrapper = document.createElement('div');
    wrapper.className = 'article-table';
    wrapper.tabIndex = 0;
    wrapper.setAttribute('role', 'region');
    wrapper.setAttribute('aria-label', `表格：${headers[0]?.textContent.trim() || '文章数据'}`);
    table.dataset.columns = String(headers.length);
    headers.forEach((header) => header.setAttribute('scope', 'col'));
    table.before(wrapper);
    wrapper.appendChild(table);
  });
})();

(() => {
  const mermaidCodeBlocks = Array.from(document.querySelectorAll('pre code.language-mermaid'));
  if (!mermaidCodeBlocks.length) {
    return;
  }

  const renderMermaid = async () => {
    try {
      const { default: mermaid } = await import('https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.esm.min.mjs');

      const readingStyles = document.body.classList.contains('reading-site')
        ? getComputedStyle(document.body)
        : null;
      const themeVariables = readingStyles
        ? {
            fontFamily: readingStyles.fontFamily,
            fontSize: '16px',
            primaryColor: readingStyles.getPropertyValue('--paper').trim(),
            primaryTextColor: readingStyles.getPropertyValue('--ink').trim(),
            primaryBorderColor: readingStyles.getPropertyValue('--line').trim(),
            lineColor: readingStyles.getPropertyValue('--accent').trim(),
            secondaryColor: readingStyles.getPropertyValue('--wash').trim(),
            secondaryTextColor: readingStyles.getPropertyValue('--ink').trim(),
            tertiaryColor: readingStyles.getPropertyValue('--paper').trim(),
            edgeLabelBackground: readingStyles.getPropertyValue('--paper').trim(),
            clusterBkg: readingStyles.getPropertyValue('--wash').trim(),
            clusterBorder: readingStyles.getPropertyValue('--line').trim()
          }
        : {
            fontFamily: '"IBM Plex Sans", "Segoe UI", sans-serif',
            fontSize: '18px',
            primaryColor: '#fffaf2',
            primaryTextColor: '#14211f',
            primaryBorderColor: '#a14d34',
            lineColor: '#1e5d5f',
            secondaryColor: '#f4efe6',
            tertiaryColor: '#f8f4ed'
          };

      mermaid.initialize({
        startOnLoad: false,
        securityLevel: 'loose',
        theme: 'base',
        fontFamily: themeVariables.fontFamily,
        flowchart: {
          useMaxWidth: false,
          htmlLabels: true,
          nodeSpacing: 36,
          rankSpacing: 54,
          padding: 12
        },
        themeVariables
      });

      const mermaidNodes = mermaidCodeBlocks
        .map((codeBlock, index) => {
          const pre = codeBlock.closest('pre');
          if (!pre) {
            return null;
          }

          const wrapper = document.createElement('div');
          wrapper.className = 'mermaid-block';
          wrapper.tabIndex = 0;
          wrapper.setAttribute('role', 'region');
          wrapper.setAttribute('aria-label', `图示 ${index + 1}，可横向滚动`);

          const graph = document.createElement('div');
          graph.className = 'mermaid';
          graph.id = `mermaid-diagram-${index + 1}`;
          graph.textContent = (codeBlock.textContent || '').trim();

          wrapper.appendChild(graph);
          pre.replaceWith(wrapper);
          return graph;
        })
        .filter(Boolean);

      if (mermaidNodes.length) {
        await mermaid.run({ nodes: mermaidNodes });
      }
    } catch (error) {
      console.error('Failed to render Mermaid diagrams.', error);
    }
  };

  renderMermaid();
})();
