import { prepareRobotHtmlForResponse, shouldReturnBotSsrUnavailable } from './robot-html-optimizer';
import { prepareSsrRobotsResponse } from './ssr-robots-response';
import { shouldApplyNoindexFollowHeader } from './ssr-route-status.helpers';

const branchUrl: string = '/fr/sitemap?node=parks&page=2';
const html: string = [
  '<html><head><title>Parcs — Plan du site · Page 2</title>',
  '<meta name="description" content="Explore les liens des parcs dans le plan du site.">',
  '<meta name="robots" content="index,follow"><meta name="googlebot" content="index,follow">',
  `<link rel="canonical" href="https://amusement-parks.fun${branchUrl}">`,
  '<script type="application/ld+json">{"@type":"BreadcrumbList","itemListElement":[]}</script>',
  '<script id="ng-state" type="application/json">{}</script><script src="main.js"></script>',
  '<style>.branch {color:blue}</style></head><body><main>',
  'Les parcs et leurs attractions sont accessibles depuis les rubriques du plan du site. '.repeat(12),
  '<a href="/fr/sitemap?node=parks%2Fpark:park-1">Parc Démo</a>',
  '</main></body></html>'
].join('');

describe('SSR robots response delivery', () => {
  it.each([false, true])('preserves indexable branch HTML after robot preparation (no-JS optimization: %s)', (noJs: boolean) => {
    const prepared = prepareRobotHtmlForResponse(html, {
      allowRobotNoJsOptimization: true, robotNoJsHtmlEnabled: true, isRobotRequest: noJs
    });
    const result = prepareSsrRobotsResponse(prepared.html, branchUrl, 200, false);
    expect(result.directive).toBeNull();
    expect(result.html).toContain('<meta name="robots" content="index,follow">');
    expect(result.html).toContain('<meta name="googlebot" content="index,follow">');
    expect(result.html).toContain('BreadcrumbList');
    expect(result.html).toContain('href="/fr/sitemap?node=parks%2Fpark:park-1"');
    expect(result.html).toContain(`href="https://amusement-parks.fun${branchUrl}"`);
    expect(result.html.includes('src="main.js"')).toBe(!noJs);
  });

  it('preserves Angular noindex for an unresolved branch instead of granting indexability from syntax', () => {
    const unavailable: string = '<html><head><meta name="robots" content="noindex,follow"><meta name="googlebot" content="noindex,follow"></head><body>Indisponible</body></html>';
    const result = prepareSsrRobotsResponse(unavailable, '/fr/sitemap?node=unknown', 200, false);
    expect(result.directive).toBeNull();
    expect(result.html).toBe(unavailable);
  });

  it.each([
    ['/fr/sitemap', 404, false], [branchUrl, 404, false],
    ['/fr/sitemap', 503, false], [branchUrl, 503, false],
    ['/fr/sitemap', 200, true], [branchUrl, 200, true]
  ] as const)('excludes sitemap errors and CSR fallbacks: %s, %s, %s', (url, status, isCsrFallback) => {
    const result = prepareSsrRobotsResponse(html, url, status, isCsrFallback);
    expect(result.directive).toBe('noindex, follow');
    expect(result.html).toContain('<meta name="robots" content="noindex,follow">');
    expect(result.html).toContain('<meta name="googlebot" content="noindex,follow">');
    expect(result.html).not.toContain('BreadcrumbList');
  });

  it('enforces noindex on malformed sitemap queries and unrelated filters', () => {
    for (const url of ['/fr/sitemap?node=parks&sort=random', '/fr/parks?node=parks&page=2']) {
      const result = prepareSsrRobotsResponse(html, url, 200, false);
      expect(result.directive).toBe('noindex, follow');
      expect(result.html).toContain('content="noindex,follow"');
      expect(result.html).not.toContain('BreadcrumbList');
    }
  });

  it('keeps eligible sitemap requests on the bot SSR-unavailable path instead of a bare successful shell', () => {
    expect(shouldReturnBotSsrUnavailable(true, 200, shouldApplyNoindexFollowHeader(branchUrl))).toBe(true);
  });
});
