// translation.service.ts
import { Injectable, Inject, PLATFORM_ID } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { isPlatformBrowser } from '@angular/common';

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

  useLang(lang: string) {
    this.translate.use(lang);
    if (isPlatformBrowser(this.platformId)) {
      document.documentElement.lang = lang; // Cette ligne ne s'exécutera que dans le navigateur
    }
  }
}
