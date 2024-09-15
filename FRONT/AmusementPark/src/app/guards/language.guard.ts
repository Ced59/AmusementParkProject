import { CanActivateFn } from '@angular/router';
import { inject } from '@angular/core';
import { TranslationService } from '../services/translation.service';
import { Router } from '@angular/router';
import { catchError, map } from 'rxjs/operators';

export const languageGuard: CanActivateFn = (route) => {
  const translationService = inject(TranslationService);
  const router = inject(Router);
  const lang = route.paramMap.get('lang') || 'en';

  if (!translationService.isValidLang(lang)) {
    return router.navigate(['en/home']).then(() => false);
  }

  return translationService.useLang(lang).pipe(
    map(() => true),
    catchError(async () => {
      await router.navigate(['en/home']);
      return false;
    })
  );
};
