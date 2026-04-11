import { isPlatformBrowser } from '@angular/common';
import { inject, PLATFORM_ID } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from '../../services/auth/auth.service';
import { ModalService } from '../../services/modal/modal.service';

export const authGuard: CanActivateFn = (_route, state) => {
  const authService = inject(AuthService);
  const modalService = inject(ModalService);
  const router = inject(Router);
  const platformId = inject(PLATFORM_ID);

  if (authService.isLoggedIn()) {
    return true;
  }

  if (isPlatformBrowser(platformId)) {
    modalService.openModal('loginModal');
    return false;
  }

  const url: string = state.url || router.url || '/en/home';
  const segments: string[] = url.split('/').filter(Boolean);
  const lang: string = segments[0] || 'en';

  return router.createUrlTree([`/${lang}/home`]);
};
