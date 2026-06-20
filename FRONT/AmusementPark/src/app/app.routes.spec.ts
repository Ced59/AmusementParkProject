import { Route } from '@angular/router';

import { routes } from './app.routes';

describe('App routes', () => {
  it('redirects legacy video share routes to canonical video routes', () => {
    const publicRoutes: Route[] = getPublicRoutes();
    const expectedRedirects: Record<string, string> = {
      'park/:id/:slug/video/s/:videoId/:videoSlug': 'park/:id/:slug/videos/:videoId/:videoSlug',
      'park/:id/:slug/video/:videoId/:videoSlug': 'park/:id/:slug/videos/:videoId/:videoSlug',
      'park/:id/:slug/item/:itemId/:itemSlug/video/s/:videoId/:videoSlug': 'park/:id/:slug/item/:itemId/:itemSlug/videos/:videoId/:videoSlug',
      'park/:id/:slug/item/:itemId/:itemSlug/video/:videoId/:videoSlug': 'park/:id/:slug/item/:itemId/:itemSlug/videos/:videoId/:videoSlug'
    };

    for (const [legacyPath, canonicalPath] of Object.entries(expectedRedirects)) {
      const route: Route | undefined = publicRoutes.find((candidate: Route): boolean => candidate.path === legacyPath);

      expect(route).withContext(legacyPath).toBeDefined();
      expect(route?.redirectTo).withContext(legacyPath).toBe(canonicalPath);
      expect(route?.pathMatch).withContext(legacyPath).toBe('full');
      expect(route?.loadComponent).withContext(legacyPath).toBeUndefined();
    }
  });
});

function getPublicRoutes(): Route[] {
  const localizedRoute: Route | undefined = routes.find((route: Route): boolean => route.path === ':lang');
  const publicLayoutRoute: Route | undefined = localizedRoute?.children?.find((route: Route): boolean =>
    (route.children ?? []).some((child: Route): boolean => child.path === 'home'));

  return publicLayoutRoute?.children ?? [];
}
