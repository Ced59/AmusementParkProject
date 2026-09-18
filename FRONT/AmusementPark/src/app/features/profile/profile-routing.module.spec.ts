import { Route } from '@angular/router';

import { authGuard } from '@core/guards/auth.guard';
import { PROFILE_ROUTES } from './profile-routing.module';

describe('profile routes', () => {
  it('keeps every statistics scope lazy and authenticated', () => {
    const paths: string[] = [
      'passport/statistics',
      'passport/items/:parkItemId',
      'passport/parks/:parkId',
      'passport/years/:year'
    ];

    paths.forEach((path: string): void => {
      const route: Route | undefined = PROFILE_ROUTES.find((candidate: Route): boolean => candidate.path === path);
      expect(route?.loadComponent).toBeDefined();
      expect(route?.canActivate).toContain(authGuard);
    });
  });

  it('keeps the visit editor lazy, authenticated and ahead of the profile root', () => {
    const editorRoute: Route | undefined = PROFILE_ROUTES.find(
      (route: Route): boolean => route.path === 'visits/:visitId'
    );
    const rootRoute: Route | undefined = PROFILE_ROUTES.find((route: Route): boolean => route.path === '');

    expect(editorRoute?.loadComponent).toBeDefined();
    expect(editorRoute?.canActivate).toContain(authGuard);
    expect(PROFILE_ROUTES.indexOf(editorRoute as Route)).toBeLessThan(PROFILE_ROUTES.indexOf(rootRoute as Route));
  });

  it('keeps the passport overview lazy and authenticated', () => {
    const overviewRoute: Route | undefined = PROFILE_ROUTES.find(
      (route: Route): boolean => route.path === 'passport'
    );

    expect(overviewRoute?.loadComponent).toBeDefined();
    expect(overviewRoute?.canActivate).toContain(authGuard);
  });

  it('keeps the opaque invitation behind the account authentication guard', () => {
    const route: Route | undefined = PROFILE_ROUTES.find(
      (candidate: Route): boolean =>
        candidate.path === 'passport/comparisons/invitations/:token'
    );

    expect(route).toBeDefined();
    expect(route?.canActivate).toContain(authGuard);
    expect(PROFILE_ROUTES.indexOf(route!)).toBeLessThan(
      PROFILE_ROUTES.findIndex((candidate: Route): boolean => candidate.path === 'passport')
    );
  });

  it('keeps private Park Fit profiles lazy, authenticated and ahead of the profile root', () => {
    const route: Route | undefined = PROFILE_ROUTES.find(
      (candidate: Route): boolean => candidate.path === 'park-fit/profiles'
    );
    const rootIndex: number = PROFILE_ROUTES.findIndex(
      (candidate: Route): boolean => candidate.path === ''
    );

    expect(route?.loadComponent).toBeDefined();
    expect(route?.canActivate).toContain(authGuard);
    expect(PROFILE_ROUTES.indexOf(route!)).toBeLessThan(rootIndex);
  });

  it('keeps personal collections lazy and authenticated', () => {
    const route: Route | undefined = PROFILE_ROUTES.find(
      (candidate: Route): boolean => candidate.path === 'collections'
    );

    expect(route?.loadComponent).toBeDefined();
    expect(route?.canActivate).toContain(authGuard);
  });

  it('keeps the private trip list and detail lazy, authenticated and ahead of the profile root', () => {
    const paths: string[] = [
      'trips',
      'trips/:tripId',
      'trips/:tripId/preferences',
      'trips/:tripId/preference-summary'
    ];
    const rootIndex: number = PROFILE_ROUTES.findIndex((candidate: Route): boolean => candidate.path === '');

    paths.forEach((path: string): void => {
      const route: Route | undefined = PROFILE_ROUTES.find((candidate: Route): boolean => candidate.path === path);
      expect(route?.loadComponent).toBeDefined();
      expect(route?.canActivate).toContain(authGuard);
      expect(PROFILE_ROUTES.indexOf(route!)).toBeLessThan(rootIndex);
    });
  });

  it('keeps the private notification centre lazy and authenticated', () => {
    const route: Route | undefined = PROFILE_ROUTES.find(
      (candidate: Route): boolean => candidate.path === 'notifications'
    );

    expect(route?.loadComponent).toBeDefined();
    expect(route?.canActivate).toContain(authGuard);
  });
});
