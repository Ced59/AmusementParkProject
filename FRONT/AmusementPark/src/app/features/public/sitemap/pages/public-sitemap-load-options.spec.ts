import { resolvePublicSitemapLoadOptions } from './public-sitemap-load-options';

describe('public sitemap load options', () => {
  it('keeps SSR sitemap rendering limited to root nodes', () => {
    expect(resolvePublicSitemapLoadOptions(true)).toEqual({
      includeDescendants: false,
      loadDescendantsInInitialRequest: false
    });
  });

  it('allows browser rendering to load descendants after the root nodes', () => {
    expect(resolvePublicSitemapLoadOptions(false)).toEqual({
      includeDescendants: true,
      loadDescendantsInInitialRequest: false
    });
  });
});
