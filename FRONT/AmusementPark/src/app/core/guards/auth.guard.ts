import { isPlatformBrowser } from '@angular/common';
import { inject, PLATFORM_ID } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { map, Observable } from 'rxjs';

import { AuthService } from '@app/services/auth/auth.service';
import { ModalService } from '@app/services/modal/modal.service';
import { resolveSupportedLanguageFromUrl } from '@shared/utils/routing/localized-route.helpers';

export const authGuard: CanActivateFn = (_route, state): Observable<boolean | UrlTree> | boolean | UrlTree => {
  const authService = inject(AuthService);
  const modalService = inject(ModalService);
  const router = inject(Router);
  const platformId = inject(PLATFORM_ID);

  return authService.ensureValidAccessToken(true).pipe(
    map((token: string | null) => {
      if (token) {
        return true;
      }

      const lang: string = resolveSupportedLanguageFromUrl(state.url || router.url, 'en');

      if (isPlatformBrowser(platformId)) {
        modalService.openModal('loginModal');
      }

      return router.createUrlTree(['/', lang, 'home']);
    })
  );
};
