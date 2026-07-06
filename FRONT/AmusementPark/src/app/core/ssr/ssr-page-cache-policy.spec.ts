import { shouldCacheSsrRenderedHtml } from './ssr-page-cache-policy';

describe('SSR page cache policy', () => {
  it('allows complete public SSR HTML to be cached', () => {
    const html: string = [
      '<html><head>',
      '<title>Amusement park guide</title>',
      '<meta name="description" content="A useful public description for amusement park visitors.">',
      '<link rel="canonical" href="https://amusement-parks.fun/en/home">',
      '</head><body><app-root><main>',
      'Helpful amusement park content with practical details for visitors. '.repeat(12),
      '</main></app-root></body></html>'
    ].join('');

    const result = shouldCacheSsrRenderedHtml(html);

    expect(result.canCache).toBeTrue();
    expect(result.reason).toBe('ready');
  });

  it('blocks transient fallback HTML without a canonical from being cached', () => {
    const html: string = [
      '<html><head>',
      '<title>Page not found - Amusement Parks</title>',
      '<meta name="description" content="The requested page is not available.">',
      '<meta name="robots" content="noindex,follow">',
      '</head><body><app-root><main>',
      'The requested page could not be rendered because public data was temporarily unavailable. '.repeat(12),
      '</main></app-root></body></html>'
    ].join('');

    const result = shouldCacheSsrRenderedHtml(html);

    expect(result.canCache).toBeFalse();
    expect(result.reason).toBe('missing-canonical');
  });
});
