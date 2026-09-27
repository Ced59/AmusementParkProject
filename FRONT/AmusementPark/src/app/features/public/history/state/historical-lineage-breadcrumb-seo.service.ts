import { Injectable } from '@angular/core';

import { CanonicalUrlService } from '@core/seo/canonical-url.service';
import { JsonLdService } from '@core/seo/json-ld.service';

const HOME_LABELS: Record<string, string> = {
  fr: 'Accueil',
  en: 'Home',
  de: 'Startseite',
  nl: 'Home',
  it: 'Home',
  es: 'Inicio',
  pl: 'Strona główna',
  pt: 'Início'
};

@Injectable({ providedIn: 'root' })
export class HistoricalLineageBreadcrumbSeoService {
  constructor(
    private readonly canonicalUrlService: CanonicalUrlService,
    private readonly jsonLdService: JsonLdService
  ) {
  }

  apply(language: string, label: string, canonicalPath: string): void {
    const normalizedLanguage: string = HOME_LABELS[language] ? language : 'en';
    this.jsonLdService.replaceJsonLdByType('BreadcrumbList', {
      '@context': 'https://schema.org',
      '@type': 'BreadcrumbList',
      itemListElement: [
        this.item(1, HOME_LABELS[normalizedLanguage], `/${normalizedLanguage}/home`),
        this.item(2, label, canonicalPath)
      ]
    });
  }

  private item(position: number, name: string, path: string): Record<string, unknown> {
    return {
      '@type': 'ListItem',
      position,
      name,
      item: this.canonicalUrlService.buildAbsoluteUrl(path)
    };
  }
}
