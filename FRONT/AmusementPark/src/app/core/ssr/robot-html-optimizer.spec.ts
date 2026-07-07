import {
  inspectSeoReadyHtml,
  isBareAngularShell,
  isSeoReadyHtml,
  optimizeHtmlForRobotNoJs,
  prepareRobotHtmlForResponse,
  shouldRetrySeoReadyHtmlRender,
  shouldReturnBotSsrUnavailable
} from './robot-html-optimizer';

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

  it('compacts presentational Angular SSR markup while preserving SEO content and links', () => {
    const repeatedContent: string = 'Helpful ranking content for visitors. '.repeat(20);
    const html: string = [
      '<html ng-server-context="ssr"><head>',
      '<title>Rankings</title>',
      '<meta name="description" content="Useful ranking page.">',
      '<link rel="canonical" href="https://amusement-parks.fun/fr/rankings">',
      '<link rel="stylesheet" href="styles.css">',
      '<style ng-app-id="ng">.ranking-card{color:red}</style>',
      '<script type="application/ld+json">{"@context":"https://schema.org"}</script>',
      '</head><body><app-root _nghost-ng-c123 ngh="1">',
      '<main _ngcontent-ng-c123 class="ranking-page" style="display:grid">',
      '<a _ngcontent-ng-c123 class="ranking-card__link" href="/fr/parks">Parcs</a>',
      '<i class="pi pi-star" aria-hidden="true"></i>',
      '<section _ngcontent-ng-c123 class="ranking-card"><span class="ranking-card__text">',
      repeatedContent,
      '</span></section>',
      '</main><app-page-jump-button _nghost-ng-c456></app-page-jump-button>',
      '</app-root></body></html>'
    ].join('');

    const result = optimizeHtmlForRobotNoJs(html);

    expect(result.html.length).toBeLessThan(html.length);
    expect(result.html).toContain('<title>Rankings</title>');
    expect(result.html).toContain('name="description"');
    expect(result.html).toContain('rel="canonical"');
    expect(result.html).toContain('application/ld+json');
    expect(result.html).toContain('href="/fr/parks"');
    expect(result.html).toContain('Parcs');
    expect(result.html).toContain(repeatedContent.trim());
    expect(result.html).not.toContain('<style');
    expect(result.html).not.toContain('_ngcontent');
    expect(result.html).not.toContain('_nghost');
    expect(result.html).not.toContain('ngh=');
    expect(result.html).not.toContain('class=');
    expect(result.html).not.toContain('style=');
    expect(result.html).not.toContain('pi pi-star');
    expect(result.html).not.toContain('app-page-jump-button');
  });

  it('keeps ordinary SSR HTML unchanged when no script-like tags are present', () => {
    const html: string = '<html><head><title>Page</title></head><body><main>Content</main></body></html>';

    const result = optimizeHtmlForRobotNoJs(html);

    expect(result.html).toBe(html);
    expect(result.removedScriptCount).toBe(0);
    expect(result.removedScriptLikeLinkCount).toBe(0);
  });

  it('marks complete SSR HTML as SEO-ready', () => {
    const html: string = buildSeoReadyHtml();

    const result = inspectSeoReadyHtml(html);

    expect(result.isReady).toBeTrue();
    expect(result.reason).toBe('ready');
    expect(isSeoReadyHtml(html)).toBeTrue();
  });

  it('detects bare Angular shells before other SEO checks', () => {
    const html: string = [
      '<html><head>',
      '<title>Public page</title>',
      '<meta name="description" content="A useful public description for the page.">',
      '<link rel="canonical" href="https://amusement-parks.fun/en/home">',
      '</head><body><app-root></app-root></body></html>'
    ].join('');

    const result = inspectSeoReadyHtml(html);

    expect(isBareAngularShell(html)).toBeTrue();
    expect(result.isReady).toBeFalse();
    expect(result.reason).toBe('bare-angular-shell');
  });

  it('asks SSR to retry transiently incomplete SEO metadata', () => {
    const missingCanonical = inspectSeoReadyHtml([
      '<html><head>',
      '<title>Public page</title>',
      '<meta name="description" content="A useful public description for the page.">',
      '</head><body><app-root><main>',
      'Helpful amusement park content with practical details for visitors. '.repeat(12),
      '</main></app-root></body></html>'
    ].join(''));
    const thinContent = inspectSeoReadyHtml([
      '<html><head>',
      '<title>Thin public page</title>',
      '<meta name="description" content="A valid description.">',
      '<link rel="canonical" href="https://amusement-parks.fun/en/thin">',
      '</head><body><app-root><main>Short</main></app-root></body></html>'
    ].join(''));
    const ready = inspectSeoReadyHtml(buildSeoReadyHtml());

    expect(missingCanonical.reason).toBe('missing-canonical');
    expect(shouldRetrySeoReadyHtmlRender(missingCanonical)).toBeTrue();
    expect(thinContent.reason).toBe('insufficient-body-content');
    expect(shouldRetrySeoReadyHtmlRender(thinContent)).toBeFalse();
    expect(shouldRetrySeoReadyHtmlRender(ready)).toBeFalse();
  });

  it('removes scripts for robot responses only when SSR HTML is SEO-ready', () => {
    const html: string = buildSeoReadyHtml([
      '<script type="module" src="main.js"></script>',
      '<script type="application/ld+json">{"@context":"https://schema.org"}</script>'
    ].join(''));

    const result = prepareRobotHtmlForResponse(html, {
      allowRobotNoJsOptimization: true,
      robotNoJsHtmlEnabled: true,
      isRobotRequest: true
    });

    expect(result.robotHtmlStatus).toBe('no-js');
    expect(result.seoReady.isReady).toBeTrue();
    expect(result.removedScriptCount).toBe(1);
    expect(result.html).not.toContain('main.js');
    expect(result.html).toContain('application/ld+json');
  });

  it('keeps scripts when robot no-JS optimization is not allowed for a CSR fallback', () => {
    const html: string = '<html><head><script src="main.js"></script></head><body><app-root></app-root></body></html>';

    const result = prepareRobotHtmlForResponse(html, {
      allowRobotNoJsOptimization: false,
      robotNoJsHtmlEnabled: true,
      isRobotRequest: true
    });

    expect(result.robotHtmlStatus).toBe('not-allowed');
    expect(result.seoReady.reason).toBe('bare-angular-shell');
    expect(result.html).toContain('main.js');
  });

  it('blocks robot no-JS optimization when SSR HTML is not SEO-ready', () => {
    const html: string = '<html><head><title>Short page</title><script src="main.js"></script></head><body><main>Short</main></body></html>';

    const result = prepareRobotHtmlForResponse(html, {
      allowRobotNoJsOptimization: true,
      robotNoJsHtmlEnabled: true,
      isRobotRequest: true
    });

    expect(result.robotHtmlStatus).toBe('blocked-not-seo-ready');
    expect(result.html).toContain('main.js');
  });

  it('keeps executable scripts when SSR HTML has metadata but not enough body content', () => {
    const html: string = [
      '<html><head>',
      '<title>Thin public page</title>',
      '<meta name="description" content="A valid description is not enough when the SSR body is thin.">',
      '<link rel="canonical" href="https://amusement-parks.fun/en/thin">',
      '<script type="module" src="main.js"></script>',
      '</head><body><app-root><main>Short content</main></app-root></body></html>'
    ].join('');

    const result = prepareRobotHtmlForResponse(html, {
      allowRobotNoJsOptimization: true,
      robotNoJsHtmlEnabled: true,
      isRobotRequest: true
    });

    expect(result.robotHtmlStatus).toBe('blocked-not-seo-ready');
    expect(result.seoReady.reason).toBe('insufficient-body-content');
    expect(result.html).toContain('main.js');
  });

  it('returns bot SSR unavailable only for robot requests that would otherwise be 200', () => {
    expect(shouldReturnBotSsrUnavailable(true, 200)).toBeTrue();
    expect(shouldReturnBotSsrUnavailable(true, 404)).toBeFalse();
    expect(shouldReturnBotSsrUnavailable(false, 200)).toBeFalse();
  });
});

function buildSeoReadyHtml(extraHead: string = ''): string {
  const bodyText: string = 'Helpful amusement park content with practical details for visitors. '.repeat(12);

  return [
    '<html><head>',
    '<title>Amusement park guide</title>',
    '<meta name="description" content="A useful public description for amusement park visitors.">',
    '<link rel="canonical" href="https://amusement-parks.fun/en/home">',
    extraHead,
    '</head><body><app-root><main>',
    bodyText,
    '</main></app-root></body></html>'
  ].join('');
}
