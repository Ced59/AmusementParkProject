import { DefaultUrlSerializer } from '@angular/router';

import { buildPublicSitemapCanonicalUrl } from './public-sitemap-seo.helpers';

describe('HTML sitemap canonical URL', () => {
  it('matches RouterLink encoding for the validated node grammar and page', () => {
    const serializer = new DefaultUrlSerializer();
    const tree = serializer.parse('/fr/sitemap');
    tree.queryParams = { node: 'snapshot-sections/sitemap-section:park-items-fr-1', page: 2 };
    expect(buildPublicSitemapCanonicalUrl('https://amusement-parks.fun/fr/sitemap', ['snapshot-sections', 'sitemap-section:park-items-fr-1'], 2))
      .toBe(`https://amusement-parks.fun${serializer.serialize(tree)}`);
  });

  it('omits redundant root and first-page parameters', () => {
    expect(buildPublicSitemapCanonicalUrl('https://amusement-parks.fun/fr/sitemap', [], 1)).toBe('https://amusement-parks.fun/fr/sitemap');
    expect(buildPublicSitemapCanonicalUrl('https://amusement-parks.fun/fr/sitemap', ['parks'], 1)).toBe('https://amusement-parks.fun/fr/sitemap?node=parks');
  });
});
