import {
  isSsrNotFoundRoute,
  resolveSsrRouteStatusCode,
  shouldApplyNoindexFollowHeader,
} from './ssr-route-status.helpers';

describe('SSR route status helpers', () => {
  it('marks unknown localized routes as 404', () => {
    expect(resolveSsrRouteStatusCode('/fr/page-qui-nexiste-pas-123456')).toBe(
      404,
    );
    expect(
      resolveSsrRouteStatusCode('/fr/page-qui-nexiste-pas-123456?from=test'),
    ).toBe(404);
    expect(resolveSsrRouteStatusCode('/route-inconnue')).toBe(404);
  });

  it('keeps unsupported language code redirects successful', () => {
    expect(resolveSsrRouteStatusCode('/zz/home')).toBe(200);
    expect(resolveSsrRouteStatusCode('/zz/page-qui-nexiste-pas-123456')).toBe(
      200,
    );
    expect(shouldApplyNoindexFollowHeader('/zz/home')).toBe(false);
  });

  it('marks explicit not found pages as 404', () => {
    expect(isSsrNotFoundRoute('/fr/not-found')).toBe(true);
    expect(resolveSsrRouteStatusCode('/en/not-found/')).toBe(404);
  });

  it('keeps known public routes successful', () => {
    const knownPublicRoutes: string[] = [
      '/',
      '/fr',
      '/fr/home',
      '/fr/parks',
      '/fr/sitemap',
      '/fr/rankings',
      '/fr/manufacturers',
      '/fr/technical',
      '/fr/technical/chain-lift',
      '/fr/about',
      '/fr/contact',
      '/fr/versions',
      '/fr/privacy',
      '/fr/attraction/standalone-123/attraction-test',
      '/fr/park/123/parc-test',
      '/fr/park/123/parc-test/map',
      '/fr/park/123/parc-test/opening-hours',
      '/fr/park/123/parc-test/pricing',
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
      '/fr/park-manufacturer/42/mack-rides',
    ];

    for (const route of knownPublicRoutes) {
      expect(resolveSsrRouteStatusCode(route), route).toBe(200);
    }
  });

  it('keeps known private client routes successful for the CSR shell', () => {
    expect(resolveSsrRouteStatusCode('/fr/profile')).toBe(200);
    expect(resolveSsrRouteStatusCode('/fr/reset-password')).toBe(200);
    expect(
      resolveSsrRouteStatusCode('/fr/admin/parks/edit/123/items/new'),
    ).toBe(200);
  });

  it('keeps park and park item comment routes available for SSR', () => {
    const commentRoutes: string[] = [
      '/fr/park/123/parc-test/comments',
      '/fr/park/123/parc-test/item/abc/attraction-test/comments',
    ];

    for (const route of commentRoutes) {
      expect(resolveSsrRouteStatusCode(route), route).toBe(200);
      expect(isSsrNotFoundRoute(route), route).toBe(false);
      expect(shouldApplyNoindexFollowHeader(route), route).toBe(false);
    }
  });

  it('applies noindex follow to public 404 and filtered exploration routes', () => {
    expect(
      shouldApplyNoindexFollowHeader('/fr/page-qui-nexiste-pas-123456'),
    ).toBe(true);
    expect(shouldApplyNoindexFollowHeader('/fr/not-found')).toBe(true);
    expect(
      shouldApplyNoindexFollowHeader('/fr/park/123/parc-test/items?zone=abc'),
    ).toBe(true);
    expect(
      shouldApplyNoindexFollowHeader(
        '/fr/park/123/parc-test/weather?unit=celsius',
      ),
    ).toBe(true);
    expect(
      shouldApplyNoindexFollowHeader(
        '/fr/park/123/parc-test/opening-hours?from=2026-07-01',
      ),
    ).toBe(true);
    expect(
      shouldApplyNoindexFollowHeader(
        '/fr/park/123/parc-test/pricing?campaign=spring',
      ),
    ).toBe(true);
    expect(shouldApplyNoindexFollowHeader('/fr/parks?search=test')).toBe(true);
    expect(shouldApplyNoindexFollowHeader('/fr/profile')).toBe(true);
    expect(shouldApplyNoindexFollowHeader('/fr/admin/parks')).toBe(true);
    expect(shouldApplyNoindexFollowHeader('/fr/parks')).toBe(false);
    expect(shouldApplyNoindexFollowHeader('/fr/sitemap')).toBe(false);
    expect(shouldApplyNoindexFollowHeader('/fr/park/123/parc-test/map')).toBe(
      false,
    );
    expect(
      shouldApplyNoindexFollowHeader('/fr/park/123/parc-test/opening-hours'),
    ).toBe(false);
    expect(shouldApplyNoindexFollowHeader('/fr/technical/chain-lift')).toBe(
      false,
    );
  });
});
