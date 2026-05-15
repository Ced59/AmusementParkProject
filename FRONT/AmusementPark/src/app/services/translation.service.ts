import { EventEmitter, Inject, Injectable, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom, forkJoin, Observable, of, tap } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

import { LANGUAGES } from '@shared/models/localization';

@Injectable({
  providedIn: 'root'
})
export class TranslationService {
  private readonly validLangs: readonly string[] = LANGUAGES.map((lang) => lang.value);
  public readonly languageChanged: EventEmitter<string> = new EventEmitter<string>();

  constructor(
    private readonly translate: TranslateService,
    @Inject(PLATFORM_ID) private readonly platformId: object
  ) {
  }

  setDefaultLang(lang: string): void {
    this.translate.setDefaultLang(lang);
  }

  useLang(lang: string): Observable<unknown> {
    if (isPlatformBrowser(this.platformId)) {
      document.documentElement.lang = lang;
    }

    return this.translate.use(lang).pipe(
      catchError((error: unknown): Observable<unknown> => {
        console.error(`Error loading language ${lang}:`, error);
        if (lang !== 'en') {
          return this.translate.use('en');
        }

        return of(null);
      }),
      tap((): void => this.languageChanged.emit(lang))
    );
  }

  isValidLang(lang: string): boolean {
    return this.validLangs.includes(lang);
  }

  initializeLanguage(): Promise<unknown> {
    this.setDefaultLang('en');
    return firstValueFrom(this.useLang('en'));
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
