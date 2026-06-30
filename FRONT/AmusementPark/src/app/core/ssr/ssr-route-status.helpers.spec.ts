import { isSsrNotFoundRoute, resolveSsrRouteStatusCode, shouldApplyNoindexFollowHeader } from './ssr-route-status.helpers';

describe('SSR route status helpers', () => {
  it('marks unknown localized routes as 404', () => {
    expect(resolveSsrRouteStatusCode('/fr/page-qui-nexiste-pas-123456')).toBe(404);
    expect(resolveSsrRouteStatusCode('/fr/page-qui-nexiste-pas-123456?from=test')).toBe(404);
    expect(resolveSsrRouteStatusCode('/zz/home')).toBe(404);
    expect(resolveSsrRouteStatusCode('/route-inconnue')).toBe(404);
  });

  it('marks explicit not found pages as 404', () => {
    expect(isSsrNotFoundRoute('/fr/not-found')).toBeTrue();
    expect(resolveSsrRouteStatusCode('/en/not-found/')).toBe(404);
  });

  it('keeps known public routes successful', () => {
    const knownPublicRoutes: string[] = [
      '/',
      '/fr',
      '/fr/home',
      '/fr/parks',
      '/fr/rankings',
      '/fr/manufacturers',
      '/fr/technical',
      '/fr/technical/chain-lift',
      '/fr/about',
      '/fr/contact',
      '/fr/versions',
      '/fr/privacy',
      '/fr/park/123/parc-test',
      '/fr/park/123/parc-test/opening-hours',
      '/fr/park/123/parc-test/images',
      '/fr/park/123/parc-test/history',
      '/fr/park/123/parc-test/history/event-1/ouverture-1987',
      '/fr/park/123/parc-test/videos/456/video-test',
      '/fr/park/123/parc-test/video/s/456/video-test',
      '/fr/park/123/parc-test/zone/789/zone-test',
      '/fr/park/123/parc-test/item/abc/attraction-test',
      '/fr/park/123/parc-test/item/abc/attraction-test/history',
      '/fr/park/123/parc-test/item/abc/attraction-test/history/event-2/retrack',
      '/fr/park/123/parc-test/item/abc/attraction-test/videos',
      '/fr/park/123/parc-test/item/abc/attraction-test/videos/456/video-test',
      '/fr/park/123/parc-test/item/abc/attraction-test/video/s/456/video-test',
      '/fr/park-manufacturer/42/mack-rides'
    ];

    for (const route of knownPublicRoutes) {
      expect(resolveSsrRouteStatusCode(route)).withContext(route).toBe(200);
    }
  });

  it('keeps known private client routes successful for the CSR shell', () => {
    expect(resolveSsrRouteStatusCode('/fr/profile')).toBe(200);
    expect(resolveSsrRouteStatusCode('/fr/reset-password')).toBe(200);
    expect(resolveSsrRouteStatusCode('/fr/admin/parks/edit/123/items/new')).toBe(200);
  });

  it('applies noindex follow to public 404 and filtered exploration routes', () => {
    expect(shouldApplyNoindexFollowHeader('/fr/page-qui-nexiste-pas-123456')).toBeTrue();
    expect(shouldApplyNoindexFollowHeader('/fr/not-found')).toBeTrue();
    expect(shouldApplyNoindexFollowHeader('/fr/park/123/parc-test/map')).toBeTrue();
    expect(shouldApplyNoindexFollowHeader('/fr/park/123/parc-test/items?zone=abc')).toBeTrue();
    expect(shouldApplyNoindexFollowHeader('/fr/park/123/parc-test/weather?unit=celsius')).toBeTrue();
    expect(shouldApplyNoindexFollowHeader('/fr/park/123/parc-test/opening-hours?from=2026-07-01')).toBeTrue();
    expect(shouldApplyNoindexFollowHeader('/fr/profile')).toBeTrue();
    expect(shouldApplyNoindexFollowHeader('/fr/admin/parks')).toBeTrue();
    expect(shouldApplyNoindexFollowHeader('/fr/parks')).toBeFalse();
    expect(shouldApplyNoindexFollowHeader('/fr/park/123/parc-test/opening-hours')).toBeFalse();
    expect(shouldApplyNoindexFollowHeader('/fr/technical/chain-lift')).toBeFalse();
  });
});
