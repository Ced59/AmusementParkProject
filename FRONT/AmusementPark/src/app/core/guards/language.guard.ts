import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { catchError, map, Observable, of } from 'rxjs';

import { TranslationService } from '@app/services/translation.service';

export const languageGuard: CanActivateFn = (route): Observable<boolean | UrlTree> | boolean | UrlTree => {
  const translationService = inject(TranslationService);
  const router = inject(Router);
  const lang: string = route.paramMap.get('lang') || 'en';

  if (!translationService.isValidLang(lang)) {
    return router.createUrlTree(isUnsupportedLanguageCode(lang) ? ['/en/home'] : ['/en/not-found']);
  }

  return translationService.useLang(lang).pipe(
    map((): boolean => true),
    catchError((): Observable<UrlTree> => of(router.createUrlTree(['/en/home'])))
  );
};

function isUnsupportedLanguageCode(value: string): boolean {
  return /^[a-z]{2}$/i.test(value);
}
