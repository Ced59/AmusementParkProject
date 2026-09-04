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

  it('server-renders shared user rankings for social previews', () => {
    const sharedRankingRoute: ServerRoute | undefined = serverRoutes.find(
      (route: ServerRoute): boolean => route.path === ':lang/rankings/shared/:shareId',
    );

    expect(sharedRankingRoute?.renderMode).toBe(RenderMode.Server);
  });

  it('keeps every nested private profile route client-rendered before the fallback', () => {
    const profileRoute: ServerRoute | undefined = serverRoutes.find(
      (route: ServerRoute): boolean => route.path === ':lang/profile'
    );
    const nestedProfileRoute: ServerRoute | undefined = serverRoutes.find(
      (route: ServerRoute): boolean => route.path === ':lang/profile/**'
    );
    const fallbackIndex: number = serverRoutes.findIndex(
      (route: ServerRoute): boolean => route.path === '**'
    );

    expect(profileRoute?.renderMode).toBe(RenderMode.Client);
    expect(nestedProfileRoute?.renderMode).toBe(RenderMode.Client);
    expect(serverRoutes.indexOf(nestedProfileRoute as ServerRoute)).toBeLessThan(fallbackIndex);
  });

  it('keeps device-local passport routes client-rendered before the fallback', () => {
    const expectedPaths: string[] = [
      ':lang/passport/local',
      ':lang/passport/local/:draftId'
    ];
    const fallbackIndex: number = serverRoutes.findIndex(
      (route: ServerRoute): boolean => route.path === '**'
    );

    for (const path of expectedPaths) {
      const route: ServerRoute | undefined = serverRoutes.find(
        (candidate: ServerRoute): boolean => candidate.path === path
      );
      expect(route?.renderMode, path).toBe(RenderMode.Client);
      expect(serverRoutes.indexOf(route as ServerRoute), path).toBeLessThan(fallbackIndex);
    }
  });

  it('server-renders current and historical rating methodology pages', () => {
    const expectedPaths: string[] = [
      ':lang/rankings/methodology',
      ':lang/rankings/methodology/:version'
    ];

    for (const path of expectedPaths) {
      const route: ServerRoute | undefined = serverRoutes.find(
        (candidate: ServerRoute): boolean => candidate.path === path
      );
      expect(route?.renderMode, path).toBe(RenderMode.Server);
    }
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
