import { Route } from '@angular/router';

import { authGuard } from '@core/guards/auth.guard';
import { PROFILE_ROUTES } from './profile-routing.module';

describe('profile routes', () => {
  it('keeps the visit editor lazy, authenticated and ahead of the profile root', () => {
    const editorRoute: Route | undefined = PROFILE_ROUTES.find(
      (route: Route): boolean => route.path === 'visits/:visitId'
    );
    const rootRoute: Route | undefined = PROFILE_ROUTES.find((route: Route): boolean => route.path === '');

    expect(editorRoute?.loadComponent).toBeDefined();
    expect(editorRoute?.canActivate).toContain(authGuard);
    expect(PROFILE_ROUTES.indexOf(editorRoute as Route)).toBeLessThan(PROFILE_ROUTES.indexOf(rootRoute as Route));
  });
});
