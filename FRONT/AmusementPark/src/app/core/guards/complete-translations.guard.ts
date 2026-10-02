import { inject } from '@angular/core';
import { CanActivateChildFn } from '@angular/router';
import { map } from 'rxjs';
import { TranslationService } from '@app/services/translation.service';
import { resolveLanguageFromUrl } from '@shared/utils/routing/route-language.utils';

export const completeTranslationsGuard: CanActivateChildFn = (_route, state) => {
  const translations = inject(TranslationService);
  const language: string = resolveLanguageFromUrl(state.url, translations.getCurrentLang() || 'en');
  return translations.loadCompleteTranslations(language).pipe(map(() => true));
};
