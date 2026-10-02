import { HttpClient } from '@angular/common/http';
import { TranslateLoader } from '@ngx-translate/core';
import { PublicFirstTranslateLoader } from '@core/i18n/public-first-translate.loader';
import { TranslationService } from './services/translation.service';
import { AuthService } from './services/auth/auth.service';
import { firstValueFrom } from 'rxjs';
import { AuthenticatedUserLanguageService } from './services/users/authenticated-user-language.service';

export function HttpLoaderFactory(http: HttpClient): TranslateLoader {
  return new PublicFirstTranslateLoader(http);
}

export function initializeApp(
  translationService: TranslationService,
  authService: AuthService,
  authenticatedUserLanguageService: AuthenticatedUserLanguageService
): () => Promise<void> {
  return async () => {
    await translationService.initializeLanguage();
    await authService.initializeSession();
    await firstValueFrom(authenticatedUserLanguageService.hydratePreferencesFromCurrentUser());
  };
}
