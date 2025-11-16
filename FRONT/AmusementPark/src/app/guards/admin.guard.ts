import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { AuthService } from '../services/auth/auth.service';

export const adminGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const platformId = inject(PLATFORM_ID);

  // Si pas loggué, on laisse authGuard gérer ça normalement,
  // mais au cas où il n'est pas enchaîné, on protège quand même :
  if (!authService.isLoggedIn()) {
    return redirectToHome(router, state);
  }

  // Check du rôle ADMIN
  if (authService.hasRole('ADMIN')) {
    return true;
  }

  if (isPlatformBrowser(platformId)) {
    console.warn('Access denied: user is not admin.');
    // ici tu peux plugger un toast "Access denied"
  }

  return redirectToHome(router, state);
};

function redirectToHome(router: Router, state: any) {
  const url = state.url || router.url || '/en/home';
  const segments = url.split('/').filter(Boolean);
  const lang = segments[0] || 'en';
  return router.createUrlTree([`/${lang}/home`]);
}
