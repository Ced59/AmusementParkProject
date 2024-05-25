import { Injectable, Inject, PLATFORM_ID } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { isPlatformBrowser } from '@angular/common';
import {Observable, of, firstValueFrom, forkJoin} from 'rxjs';
import {catchError, map} from 'rxjs/operators';
import {LANGUAGES} from "../commons/languages";

@Injectable({
  providedIn: 'root'
})
export class TranslationService {
  private validLangs = LANGUAGES.map(lang => lang.value);

  constructor(
    private translate: TranslateService,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {}

  setDefaultLang(lang: string) {
    this.translate.setDefaultLang(lang);
  }

  useLang(lang: string): Observable<any> {
    if (isPlatformBrowser(this.platformId)) {
      document.documentElement.lang = lang;
    }
    return this.translate.use(lang).pipe(
      catchError(error => {
        console.error(`Error loading language ${lang}:`, error);
        // Fallback to English if the requested language fails
        if (lang !== 'en') {
          console.log('Falling back to English');
          return this.translate.use('en');
        }
        return of(null);
      })
    );
  }

  isValidLang(lang: string): boolean {
    return this.validLangs.includes(lang);
  }

  initializeLanguage(): Promise<any> {
    this.setDefaultLang('en');
    return firstValueFrom(this.useLang('en'));
  }

  getCurrentLang(): string {
    let currentLang = this.translate.currentLang;
    console.log(currentLang);
    return currentLang;
  }

  getCurrentLangCode(): string {
    const currentLang = this.getCurrentLang();
    const language = LANGUAGES.find(lang => lang.value === currentLang);
    return language ? language.code : 'en-US';
  }

  getTranslations(keys: string[]): Observable<{ [key: string]: string }> {
    return forkJoin(
      keys.map(key =>
        this.translate.get(key).pipe(
          map(value => ({ key, value }))
        )
      )
    ).pipe(
      map(results => {
        let translations: { [key: string]: string } = {};
        results.forEach(result => {
          translations[result.key] = result.value;
        });
        return translations;
      })
    );
  }

}
