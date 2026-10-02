import { DOCUMENT } from '@angular/common';
import { EventEmitter, Inject, Injectable, Optional } from '@angular/core';
import { TranslateLoader, TranslateService } from '@ngx-translate/core';
import { firstValueFrom, forkJoin, Observable, of, switchMap, tap } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

import { LANGUAGES } from '@shared/models/localization';
import { resolveLanguageFromUrl } from '@shared/utils/routing/route-language.utils';
import { PublicFirstTranslateLoader } from '@core/i18n/public-first-translate.loader';

@Injectable({
  providedIn: 'root'
})
export class TranslationService {
  private readonly validLangs: readonly string[] = LANGUAGES.map((lang) => lang.value);
  public readonly languageChanged: EventEmitter<string> = new EventEmitter<string>();

  constructor(
    private readonly translate: TranslateService,
    @Inject(DOCUMENT) private readonly document: Document,
    @Optional() private readonly loader: TranslateLoader | null = null
  ) {
  }

  setDefaultLang(lang: string): void {
    this.translate.setDefaultLang(lang);
  }

  useLang(lang: string): Observable<unknown> {
    this.document.documentElement.lang = lang;

    return this.activateLanguage(lang).pipe(
      catchError((error: unknown): Observable<unknown> => {
        console.error(`Error loading language ${lang}:`, error);
        if (lang !== 'en') {
          this.setDefaultLang('en');
          return this.activateLanguage('en');
        }

        return of(null);
      }),
      tap((): void => this.languageChanged.emit(lang))
    );
  }

  loadCompleteTranslations(language: string = this.getCurrentLang() || 'en'): Observable<unknown> {
    if (!(this.loader instanceof PublicFirstTranslateLoader)) {
      return of(null);
    }
    return this.loader.getCompleteTranslation(language).pipe(
      tap((dictionary: Record<string, unknown>): void => this.translate.setTranslation(language, dictionary, true))
    );
  }

  private activateLanguage(language: string): Observable<unknown> {
    // ngx-translate can already hold a public-only dictionary for this language.
    const preparation: Observable<unknown> = this.loader instanceof PublicFirstTranslateLoader && this.loader.requiresCompleteTranslations
      ? this.loadCompleteTranslations(language)
      : of(null);
    return preparation.pipe(switchMap(() => this.translate.use(language)));
  }

  isValidLang(lang: string): boolean {
    return this.validLangs.includes(lang);
  }

  initializeLanguage(): Promise<unknown> {
    const initialLanguage: string = this.resolveInitialLanguage();
    this.setDefaultLang(initialLanguage);
    return firstValueFrom(this.useLang(initialLanguage));
  }

  private resolveInitialLanguage(): string {
    const pathname: string = this.document.location?.pathname ?? '';
    const htmlLanguage: string | null = this.document.documentElement.getAttribute('lang');

    return resolveLanguageFromUrl(pathname, htmlLanguage ?? 'en');
  }

  getCurrentLang(): string {
    return this.translate.currentLang;
  }

  getCurrentLangCode(): string {
    const currentLang: string = this.getCurrentLang();
    const language = LANGUAGES.find((lang) => lang.value === currentLang);
    return language ? language.code : 'en-US';
  }

  getTranslations(keys: string[]): Observable<Record<string, string>> {
    return forkJoin(
      keys.map((key: string) =>
        this.translate.get(key).pipe(
          map((value: string) => ({ key, value }))
        )
      )
    ).pipe(
      map((results: { key: string; value: string }[]): Record<string, string> => {
        const translations: Record<string, string> = {};
        results.forEach((result: { key: string; value: string }): void => {
          translations[result.key] = result.value;
        });
        return translations;
      })
    );
  }
}
