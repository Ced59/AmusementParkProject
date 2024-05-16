import { Injectable, Inject, PLATFORM_ID } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { isPlatformBrowser } from '@angular/common';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class TranslationService {
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
    return this.translate.use(lang);
  }
}
