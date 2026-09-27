import { Injectable } from '@angular/core';

import { CanonicalUrlService } from '@core/seo/canonical-url.service';
import { JsonLdService } from '@core/seo/json-ld.service';

interface HistoricalLineageBreadcrumbCopy {
  home: string;
  parks: string;
  history: string;
}

export interface HistoricalLineageBreadcrumbContext {
  parkName: string;
  parkPath: string;
  historyPath: string;
}

const BREADCRUMB_COPY: Record<string, HistoricalLineageBreadcrumbCopy> = {
  fr: { home: 'Accueil', parks: 'Parcs', history: 'Histoire' },
  en: { home: 'Home', parks: 'Parks', history: 'History' },
  de: { home: 'Startseite', parks: 'Parks', history: 'Geschichte' },
  nl: { home: 'Home', parks: 'Parken', history: 'Geschiedenis' },
  it: { home: 'Home', parks: 'Parchi', history: 'Storia' },
  es: { home: 'Inicio', parks: 'Parques', history: 'Historia' },
  pl: { home: 'Strona główna', parks: 'Parki', history: 'Historia' },
  pt: { home: 'Início', parks: 'Parques', history: 'História' }
};

@Injectable({ providedIn: 'root' })
export class HistoricalLineageBreadcrumbSeoService {
  constructor(
    private readonly canonicalUrlService: CanonicalUrlService,
    private readonly jsonLdService: JsonLdService
  ) {
  }

  apply(
    language: string,
    label: string,
    canonicalPath: string,
    context: HistoricalLineageBreadcrumbContext | null
  ): void {
    const normalizedLanguage: string = BREADCRUMB_COPY[language] ? language : 'en';
    const copy: HistoricalLineageBreadcrumbCopy = BREADCRUMB_COPY[normalizedLanguage];
    const items: Array<Record<string, unknown>> = [
      this.item(1, copy.home, `/${normalizedLanguage}/home`)
    ];
    if (context) {
      items.push(
        this.item(2, copy.parks, `/${normalizedLanguage}/parks`),
        this.item(3, context.parkName, context.parkPath),
        this.item(4, copy.history, context.historyPath),
        this.item(5, label, canonicalPath)
      );
    } else {
      items.push(this.item(2, label, canonicalPath));
    }

    this.jsonLdService.replaceJsonLdByType('BreadcrumbList', {
      '@context': 'https://schema.org',
      '@type': 'BreadcrumbList',
      itemListElement: items
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
