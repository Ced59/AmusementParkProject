import { HttpClient } from '@angular/common/http';
import { TranslateLoader } from '@ngx-translate/core';
import { catchError, forkJoin, map, Observable, of } from 'rxjs';
import { TranslationService } from './services/translation.service';
import { AuthService } from './services/auth/auth.service';

type TranslationDictionary = Record<string, unknown>;
type TranslationOverrides = Record<string, TranslationDictionary>;

function mergeTranslations(base: TranslationDictionary, override: TranslationDictionary): TranslationDictionary {
  const result: TranslationDictionary = { ...base };

  for (const [key, value] of Object.entries(override)) {
    const baseValue: unknown = result[key];

    if (
      value &&
      typeof value === 'object' &&
      !Array.isArray(value) &&
      baseValue &&
      typeof baseValue === 'object' &&
      !Array.isArray(baseValue)
    ) {
      result[key] = mergeTranslations(baseValue as TranslationDictionary, value as TranslationDictionary);
      continue;
    }

    result[key] = value;
  }

  return result;
}

class MergedTranslateHttpLoader implements TranslateLoader {
  public constructor(private readonly http: HttpClient) {
  }

  public getTranslation(language: string): Observable<TranslationDictionary> {
    return forkJoin({
      base: this.http.get<TranslationDictionary>(`./assets/i18n/${language}.json`),
      overrides: this.http.get<TranslationOverrides>('./assets/i18n/all-overrides.json').pipe(catchError(() => of({})))
    }).pipe(map(({ base, overrides }) => mergeTranslations(base, overrides[language] ?? {})));
  }
}

export function HttpLoaderFactory(http: HttpClient): TranslateLoader {
  return new MergedTranslateHttpLoader(http);
}

export function initializeApp(translationService: TranslationService, authService: AuthService): () => Promise<void> {
  return async () => {
    await translationService.initializeLanguage();
    await authService.initializeSession();
  };
}
