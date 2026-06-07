import { HttpClient } from '@angular/common/http';
import { TranslateLoader } from '@ngx-translate/core';
import { TranslateHttpLoader } from '@ngx-translate/http-loader';
import { TranslationService } from './services/translation.service';
import { AuthService } from './services/auth/auth.service';

export function HttpLoaderFactory(http: HttpClient): TranslateLoader {
  return new TranslateHttpLoader(http, './assets/i18n/', '.json');
}

export function initializeApp(translationService: TranslationService, authService: AuthService): () => Promise<void> {
  return async () => {
    await translationService.initializeLanguage();
    await authService.initializeSession();
  };
}
