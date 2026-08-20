import { isPlatformBrowser } from '@angular/common';
import { inject, PLATFORM_ID } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';

import { LanguagePreferenceService } from '@app/services/localization/language-preference.service';

export const languageEntryGuard: CanActivateFn = (): boolean | UrlTree => {
  if (!isPlatformBrowser(inject(PLATFORM_ID))) {
    return true;
  }

  const preferredLanguage: string | null = inject(LanguagePreferenceService).getPreferredLanguage();
  if (preferredLanguage === null) {
    return true;
  }

  return inject(Router).createUrlTree(['/', preferredLanguage, 'home']);
};
