import { isPublicStandaloneAttractionArticleRoute } from './public-standalone-article-route-policy';

describe('standalone history article SSR routes', () => {
  it.each(['fr', 'en', 'de', 'nl', 'it', 'es', 'pl', 'pt'])('recognizes an article in %s', language => {
    const path = `/${language}/attraction/22507909-9fab-4452-a87d-27fded8b2e3c/dreamfly-porto/history/a0e3e194-1987-4533-8f63-bcd80b547f01/dreamfly-porto-premieres-annees-maia`;
    expect(isPublicStandaloneAttractionArticleRoute(path)).toBe(true);
    expect(isPublicStandaloneAttractionArticleRoute(path + '/')).toBe(true);
  });

  it.each([
    '/fr/attraction/id/slug',
    '/fr/attraction/id/slug/history',
    '/fr/attraction/id/slug/history/page/2',
    '/fr/attraction/id/slug/history/event',
    '/fr/attraction/id/slug/history/event/article/extra',
    '/fr/admin/attraction/id/slug/history/event/article',
    '/fr/park/id/slug/history/event/article',
  ])('does not broaden the article route to %s', path => {
    expect(isPublicStandaloneAttractionArticleRoute(path)).toBe(false);
  });
});
