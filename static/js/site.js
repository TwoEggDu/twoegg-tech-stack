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
  const mermaidCodeBlocks = Array.from(document.querySelectorAll('pre code.language-mermaid'));
  if (!mermaidCodeBlocks.length) {
    return;
  }

  const renderMermaid = async () => {
    try {
      const { default: mermaid } = await import('https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.esm.min.mjs');

      mermaid.initialize({
        startOnLoad: false,
        securityLevel: 'loose',
        theme: 'base',
        flowchart: {
          useMaxWidth: true,
          htmlLabels: true
        },
        themeVariables: {
          fontFamily: '"IBM Plex Sans", "Segoe UI", sans-serif',
          primaryColor: '#fffaf2',
          primaryTextColor: '#14211f',
          primaryBorderColor: '#a14d34',
          lineColor: '#1e5d5f',
          secondaryColor: '#f4efe6',
          tertiaryColor: '#f8f4ed'
        }
      });

      const mermaidNodes = mermaidCodeBlocks
        .map((codeBlock, index) => {
          const pre = codeBlock.closest('pre');
          if (!pre) {
            return null;
          }

          const wrapper = document.createElement('div');
          wrapper.className = 'mermaid-block';

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
