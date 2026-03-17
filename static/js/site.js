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
