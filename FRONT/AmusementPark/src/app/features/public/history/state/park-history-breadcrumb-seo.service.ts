import { Injectable } from '@angular/core';

import { CanonicalUrlService } from '@core/seo/canonical-url.service';
import { JsonLdService } from '@core/seo/json-ld.service';

interface ParkHistoryBreadcrumbCopy {
  home: string;
  parks: string;
  history: string;
  year: (year: number) => string;
}

const PARK_HISTORY_BREADCRUMB_COPY: Record<string, ParkHistoryBreadcrumbCopy> = {
  fr: { home: 'Accueil', parks: 'Parcs', history: 'Histoire', year: (year: number): string => `En ${year}` },
  en: { home: 'Home', parks: 'Parks', history: 'History', year: (year: number): string => `In ${year}` },
  de: { home: 'Startseite', parks: 'Parks', history: 'Geschichte', year: (year: number): string => `Im Jahr ${year}` },
  nl: { home: 'Home', parks: 'Parken', history: 'Geschiedenis', year: (year: number): string => `In ${year}` },
  it: { home: 'Home', parks: 'Parchi', history: 'Storia', year: (year: number): string => `Nel ${year}` },
  es: { home: 'Inicio', parks: 'Parques', history: 'Historia', year: (year: number): string => `En ${year}` },
  pl: { home: 'Strona główna', parks: 'Parki', history: 'Historia', year: (year: number): string => `W ${year} roku` },
  pt: { home: 'Início', parks: 'Parques', history: 'História', year: (year: number): string => `Em ${year}` }
};

@Injectable({ providedIn: 'root' })
export class ParkHistoryBreadcrumbSeoService {
  constructor(
    private readonly canonicalUrlService: CanonicalUrlService,
    private readonly jsonLdService: JsonLdService
  ) {
  }

  apply(
    parkId: string,
    parkName: string,
    parkSlug: string,
    language: string,
    canonicalPath: string,
    year: number | null = null
  ): void {
    const normalizedLanguage: string = PARK_HISTORY_BREADCRUMB_COPY[language] ? language : 'en';
    const copy: ParkHistoryBreadcrumbCopy = PARK_HISTORY_BREADCRUMB_COPY[normalizedLanguage];
    const parkPath: string = `/${normalizedLanguage}/park/${encodeURIComponent(parkId)}/${encodeURIComponent(parkSlug)}`;
    const historyPath: string = `${parkPath}/history`;
    const items: Array<Record<string, unknown>> = [
      this.item(1, copy.home, `/${normalizedLanguage}/home`),
      this.item(2, copy.parks, `/${normalizedLanguage}/parks`),
      this.item(3, parkName, parkPath),
      this.item(4, copy.history, historyPath)
    ];

    if (year !== null) {
      items.push(this.item(5, copy.year(year), canonicalPath));
    } else {
      items[items.length - 1] = this.item(4, copy.history, canonicalPath);
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
