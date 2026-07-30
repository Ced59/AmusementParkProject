import { registerLocaleData } from '@angular/common';
import localeDe from '@angular/common/locales/de';
import localeEn from '@angular/common/locales/en';
import localeEs from '@angular/common/locales/es';
import localeFr from '@angular/common/locales/fr';
import localeIt from '@angular/common/locales/it';
import localeNl from '@angular/common/locales/nl';
import localePl from '@angular/common/locales/pl';
import localePt from '@angular/common/locales/pt';

import { LANGUAGES } from '@shared/models/localization';

const ANGULAR_LOCALE_DATA: Readonly<Record<string, unknown>> = {
  de: localeDe,
  en: localeEn,
  es: localeEs,
  fr: localeFr,
  it: localeIt,
  nl: localeNl,
  pl: localePl,
  pt: localePt
};

export function registerSupportedAngularLocales(): void {
  for (const language of LANGUAGES) {
    const localeData: unknown = ANGULAR_LOCALE_DATA[language.value];
    if (!localeData) {
      continue;
    }

    registerLocaleData(localeData, language.value);
    registerLocaleData(localeData, language.code);
  }
}
