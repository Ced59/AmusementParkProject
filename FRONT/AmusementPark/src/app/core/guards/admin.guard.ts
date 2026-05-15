import { isPlatformBrowser } from '@angular/common';
import { inject, PLATFORM_ID } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { map, Observable } from 'rxjs';

import { AuthService } from '@app/services/auth/auth.service';
import { resolveSupportedLanguageFromUrl } from '@shared/utils/routing/localized-route.helpers';

export const adminGuard: CanActivateFn = (_route, state): Observable<boolean | UrlTree> | boolean | UrlTree => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const platformId = inject(PLATFORM_ID);

  return authService.ensureValidAccessToken(true).pipe(
    map((token: string | null) => {
      if (!token) {
        return redirectToHome(router, state);
      }

      if (authService.hasRole('ADMIN')) {
        return true;
      }

      if (isPlatformBrowser(platformId)) {
        console.warn('Access denied: user is not admin.');
      }

      return redirectToHome(router, state);
    })
  );
};

function redirectToHome(router: Router, state: { url?: string }): UrlTree {
  const lang: string = resolveSupportedLanguageFromUrl(state.url || router.url, 'en');
  return router.createUrlTree(['/', lang, 'home']);
}
