import { Injectable, Inject, PLATFORM_ID } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { isPlatformBrowser } from '@angular/common';
import { Observable, of, firstValueFrom } from 'rxjs';
import { catchError } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class TranslationService {
  private validLangs = ['en', 'fr', 'es', 'de'];

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
}
