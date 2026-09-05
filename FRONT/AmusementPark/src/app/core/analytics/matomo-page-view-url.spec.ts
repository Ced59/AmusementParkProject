import { sanitizeMatomoPageViewUrl } from './matomo-page-view-url';

describe('sanitizeMatomoPageViewUrl', () => {
  it.each([
    'https://amusement-parks.fun/fr/profile/passport/parks/park-technical-id',
    'https://amusement-parks.fun/fr/profile/visits/visit-technical-id',
    'https://amusement-parks.fun/fr/passport/local/draft-technical-id'
  ])('replaces private passport paths with a synthetic URL', (pageUrl: string) => {
    expect(sanitizeMatomoPageViewUrl(pageUrl)).toBe(
      'https://amusement-parks.fun/fr/product/passport'
    );
  });

  it('removes query parameters and fragments from every tracked page', () => {
    expect(sanitizeMatomoPageViewUrl(
      'https://amusement-parks.fun/fr/reset-password?token=private#form'
    )).toBe('https://amusement-parks.fun/fr/reset-password');
  });

  it('preserves a public canonical path', () => {
    expect(sanitizeMatomoPageViewUrl(
      'https://amusement-parks.fun/fr/park/public-id/park-name'
    )).toBe('https://amusement-parks.fun/fr/park/public-id/park-name');
  });
});
