import { RenderMode, ServerRoute } from '@angular/ssr';

import { serverRoutes } from './app.routes.server';

describe('Server routes', () => {
  it('keeps every nested administration route client-rendered', () => {
    const adminRoute: ServerRoute | undefined = serverRoutes.find(
      (route: ServerRoute): boolean => route.path === ':lang/admin'
    );
    const nestedAdminRoute: ServerRoute | undefined = serverRoutes.find(
      (route: ServerRoute): boolean => route.path === ':lang/admin/**'
    );
    const fallbackIndex: number = serverRoutes.findIndex(
      (route: ServerRoute): boolean => route.path === '**'
    );
    const nestedAdminIndex: number = serverRoutes.indexOf(nestedAdminRoute as ServerRoute);

    expect(adminRoute?.renderMode).toBe(RenderMode.Client);
    expect(nestedAdminRoute?.renderMode).toBe(RenderMode.Client);
    expect(nestedAdminIndex).toBeGreaterThanOrEqual(0);
    expect(nestedAdminIndex).toBeLessThan(fallbackIndex);
  });

  it('server-renders the public park pricing page', () => {
    const pricingRoute: ServerRoute | undefined = serverRoutes.find(
      (route: ServerRoute): boolean => route.path === ':lang/park/:id/:slug/pricing'
    );

    expect(pricingRoute?.renderMode).toBe(RenderMode.Server);
  });

  it('server-renders standalone attraction history timelines', () => {
    const expectedPaths: string[] = [
      ':lang/attraction/:standaloneAttractionId/:slug/history',
      ':lang/attraction/:standaloneAttractionId/:slug/history/page/:page',
    ];

    for (const path of expectedPaths) {
      const route: ServerRoute | undefined = serverRoutes.find(
        (candidate: ServerRoute): boolean => candidate.path === path,
      );

      expect(route?.renderMode, path).toBe(RenderMode.Server);
    }
  });
});
