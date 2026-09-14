import { createMatomoPageViewData, sanitizeMatomoPageViewUrl } from './matomo-page-view-url';

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

  it.each([
    ['rankings/shared/ranking-secret', 'personal-ranking'],
    ['passport/shared/visits/visit-secret', 'visit-recap'],
    ['passport/shared/years/year-secret', 'year-recap'],
    ['passport/shared/profiles/profile-secret', 'passport-profile'],
    ['passport/shared/comparisons/comparison-secret', 'profile-comparison']
  ])('replaces a public share token with its categorical product path', (
    path: string,
    recapType: string
  ) => {
    expect(sanitizeMatomoPageViewUrl(
      `https://amusement-parks.fun/fr/${path}?source=private#details`
    )).toBe(`https://amusement-parks.fun/fr/product/share/${recapType}`);
  });

  it('preserves a public canonical path', () => {
    expect(sanitizeMatomoPageViewUrl(
      'https://amusement-parks.fun/fr/park/public-id/park-name'
    )).toBe('https://amusement-parks.fun/fr/park/public-id/park-name');
  });

  it('replaces a private passport title in the complete page-view payload', () => {
    expect(createMatomoPageViewData(
      'https://amusement-parks.fun/fr/profile/visits/visit-technical-id',
      'Visite privée à Europa-Park'
    )).toEqual({
      url: 'https://amusement-parks.fun/fr/product/passport',
      title: 'Passport'
    });
  });

  it.each([
    ['rankings/shared/ranking-secret', 'Classement de Camille'],
    ['passport/shared/visits/visit-secret', 'Journée de Camille à Europa-Park'],
    ['passport/shared/years/year-secret', 'L’année 2026 de Camille'],
    ['passport/shared/profiles/profile-secret', 'Passeport de Camille'],
    ['passport/shared/comparisons/comparison-secret', 'Camille face à Alex']
  ])('removes personal content from the complete public share page-view payload', (
    path: string,
    privateTitle: string
  ) => {
    const pageView = createMatomoPageViewData(
      `https://amusement-parks.fun/fr/${path}?source=private#details`,
      privateTitle
    );

    expect(pageView.url).toMatch(/^https:\/\/amusement-parks\.fun\/fr\/product\/share\//);
    expect(pageView.url).not.toContain('secret');
    expect(pageView.title).toBe('Shared experience');
    expect(JSON.stringify(pageView)).not.toContain(privateTitle);
  });
});
