import { optimizeHtmlForRobotNoJs } from './robot-html-optimizer';

describe('robot HTML optimizer', () => {
  it('removes executable scripts and Angular transfer state while preserving JSON-LD', () => {
    const html: string = [
      '<html><head>',
      '<script type="application/ld+json">{"@context":"https://schema.org"}</script>',
      '<script src="main-ABCDEFGH.js" type="module"></script>',
      '<script id="ng-state" type="application/json">{"state":true}</script>',
      '<script>window.__bootstrap = true;</script>',
      '</head><body><main>Public SSR content</main></body></html>'
    ].join('');

    const result = optimizeHtmlForRobotNoJs(html);

    expect(result.removedScriptCount).toBe(3);
    expect(result.html).toContain('application/ld+json');
    expect(result.html).toContain('Public SSR content');
    expect(result.html).not.toContain('main-ABCDEFGH.js');
    expect(result.html).not.toContain('ng-state');
    expect(result.html).not.toContain('window.__bootstrap');
  });

  it('removes script preloads without touching stylesheets or images', () => {
    const html: string = [
      '<html><head>',
      '<link rel="modulepreload" href="chunk-ABCDEFGH.js">',
      '<link rel="preload" as="script" href="main-ABCDEFGH.js">',
      '<link rel="prefetch" href="lazy-ABCDEFGH.mjs">',
      '<link rel="preload" as="style" href="styles-ABCDEFGH.css">',
      '<link rel="stylesheet" href="styles-ABCDEFGH.css">',
      '<link rel="preload" as="image" href="/hero.webp">',
      '</head><body></body></html>'
    ].join('');

    const result = optimizeHtmlForRobotNoJs(html);

    expect(result.removedScriptLikeLinkCount).toBe(3);
    expect(result.html).not.toContain('modulepreload');
    expect(result.html).not.toContain('as="script"');
    expect(result.html).not.toContain('lazy-ABCDEFGH.mjs');
    expect(result.html).toContain('as="style"');
    expect(result.html).toContain('rel="stylesheet"');
    expect(result.html).toContain('as="image"');
  });

  it('keeps ordinary SSR HTML unchanged when no script-like tags are present', () => {
    const html: string = '<html><head><title>Page</title></head><body><main>Content</main></body></html>';

    const result = optimizeHtmlForRobotNoJs(html);

    expect(result.html).toBe(html);
    expect(result.removedScriptCount).toBe(0);
    expect(result.removedScriptLikeLinkCount).toBe(0);
  });
});
