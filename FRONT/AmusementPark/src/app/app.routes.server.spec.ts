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
});
