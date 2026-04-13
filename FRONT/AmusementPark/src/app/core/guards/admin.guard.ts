import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { map, Observable } from 'rxjs';

import { AuthService } from '@app/services/auth/auth.service';

export const adminGuard: CanActivateFn = (_route, state): Observable<boolean | UrlTree> | boolean | UrlTree => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.ensureValidAccessToken().pipe(
    map((token: string | null) => {
      if (!token) {
        return redirectToHome(router, state);
      }

      if (authService.hasRole('ADMIN')) {
        return true;
      }

      if (typeof window !== 'undefined') {
        console.warn('Access denied: user is not admin.');
      }

      return redirectToHome(router, state);
    })
  );
};

function redirectToHome(router: Router, state: { url?: string }): UrlTree {
  const url: string = state.url || router.url || '/en/home';
  const segments: string[] = url.split('/').filter(Boolean);
  const lang: string = segments[0] || 'en';
  return router.createUrlTree([`/${lang}/home`]);
}
