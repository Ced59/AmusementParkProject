// Browser diagnostics only. This module is never imported by the application.
export function initialThemeObserver(savedTheme) {
  if (![null, 'light', 'dark'].includes(savedTheme)) throw new Error('Invalid saved theme for the isolated browser.');
  return `(() => {
    const evidence = window.__apfInitialTheme = { frames: [], storageError: false };
    try {
      ${savedTheme === null ? "localStorage.removeItem('amusement-park-theme');" : `localStorage.setItem('amusement-park-theme', ${JSON.stringify(savedTheme)});`}
    } catch { evidence.storageError = true; }
    const start = performance.now();
    const sample = () => {
      const timeMs = performance.now();
      const paint = performance.getEntriesByType('paint').find(entry => entry.name === 'first-contentful-paint');
      if (document.body && document.querySelector('app-root') && evidence.frames.length < 180) {
        const style = getComputedStyle(document.body);
        evidence.frames.push({ timeMs, theme: document.body.classList.contains('light-mode') ? 'light' : 'dark',
          background: style.backgroundColor, color: style.color });
      }
      if ((!paint && timeMs - start < 2500) || (paint && timeMs < paint.startTime + 500)) requestAnimationFrame(sample);
    };
    requestAnimationFrame(sample);
  })();`;
}

export function validateInitialTheme(evidence, expectedTheme) {
  if (!['light', 'dark'].includes(expectedTheme) || evidence.storageError
    || !Number.isFinite(evidence.firstContentfulPaintMs) || !Array.isArray(evidence.frames) || evidence.frames.length > 180) {
    throw new Error('Initial theme evidence is incomplete; no no-flash verdict is possible.');
  }
  const expected = expectedTheme === 'light'
    ? { background: 'rgb(247, 246, 240)', color: 'rgb(25, 32, 29)' }
    : { background: 'rgb(21, 26, 23)', color: 'rgb(245, 245, 236)' };
  const visibleFrames = evidence.frames.filter(frame => frame.timeMs >= evidence.firstContentfulPaintMs);
  if (!visibleFrames.length) throw new Error('No post-paint theme frame was observed.');
  const wrongFrames = visibleFrames.filter(frame => frame.theme !== expectedTheme || frame.background !== expected.background || frame.color !== expected.color);
  if (wrongFrames.length || evidence.finalTheme !== expectedTheme || evidence.finalBackground !== expected.background) {
    throw new Error(`Initial ${expectedTheme} theme flashed or diverged: ${JSON.stringify(wrongFrames.slice(0, 4))}`);
  }
  return { expectedTheme, firstContentfulPaintMs: evidence.firstContentfulPaintMs, observedPostPaintFrames: visibleFrames.length, wrongFrames: 0 };
}
