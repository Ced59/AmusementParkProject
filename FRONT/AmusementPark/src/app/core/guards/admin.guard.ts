import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from '../../services/auth/auth.service';

export const adminGuard: CanActivateFn = (_route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isLoggedIn()) {
    return redirectToHome(router, state);
  }

  if (authService.hasRole('ADMIN')) {
    return true;
  }

  if (typeof window !== 'undefined') {
    console.warn('Access denied: user is not admin.');
  }

  return redirectToHome(router, state);
};

function redirectToHome(router: Router, state: { url?: string }): ReturnType<Router['createUrlTree']> {
  const url: string = state.url || router.url || '/en/home';
  const segments: string[] = url.split('/').filter(Boolean);
  const lang: string = segments[0] || 'en';
  return router.createUrlTree([`/${lang}/home`]);
}
