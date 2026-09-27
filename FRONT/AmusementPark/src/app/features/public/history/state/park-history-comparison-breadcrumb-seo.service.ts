import { Injectable } from '@angular/core';

import { CanonicalUrlService } from '@core/seo/canonical-url.service';
import { JsonLdService } from '@core/seo/json-ld.service';

interface ParkHistoryComparisonBreadcrumbCopy {
  home: string;
  parks: string;
  history: string;
  comparison: (fromYear: number, toYear: number) => string;
}

const COPY: Record<string, ParkHistoryComparisonBreadcrumbCopy> = {
  fr: { home: 'Accueil', parks: 'Parcs', history: 'Histoire', comparison: (fromYear: number, toYear: number): string => `${fromYear} face à ${toYear}` },
  en: { home: 'Home', parks: 'Parks', history: 'History', comparison: (fromYear: number, toYear: number): string => `${fromYear} vs ${toYear}` },
  de: { home: 'Startseite', parks: 'Parks', history: 'Geschichte', comparison: (fromYear: number, toYear: number): string => `${fromYear} im Vergleich zu ${toYear}` },
  nl: { home: 'Home', parks: 'Parken', history: 'Geschiedenis', comparison: (fromYear: number, toYear: number): string => `${fromYear} tegenover ${toYear}` },
  it: { home: 'Home', parks: 'Parchi', history: 'Storia', comparison: (fromYear: number, toYear: number): string => `${fromYear} rispetto al ${toYear}` },
  es: { home: 'Inicio', parks: 'Parques', history: 'Historia', comparison: (fromYear: number, toYear: number): string => `${fromYear} frente a ${toYear}` },
  pl: { home: 'Strona główna', parks: 'Parki', history: 'Historia', comparison: (fromYear: number, toYear: number): string => `${fromYear} a ${toYear}` },
  pt: { home: 'Início', parks: 'Parques', history: 'História', comparison: (fromYear: number, toYear: number): string => `${fromYear} face a ${toYear}` }
};

@Injectable({ providedIn: 'root' })
export class ParkHistoryComparisonBreadcrumbSeoService {
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
    fromYear: number,
    toYear: number
  ): void {
    const normalizedLanguage: string = COPY[language] ? language : 'en';
    const copy: ParkHistoryComparisonBreadcrumbCopy = COPY[normalizedLanguage];
    const parkPath: string = `/${normalizedLanguage}/park/${encodeURIComponent(parkId)}/${encodeURIComponent(parkSlug)}`;
    const historyPath: string = `${parkPath}/history`;
    this.jsonLdService.replaceJsonLdByType('BreadcrumbList', {
      '@context': 'https://schema.org',
      '@type': 'BreadcrumbList',
      itemListElement: [
        this.item(1, copy.home, `/${normalizedLanguage}/home`),
        this.item(2, copy.parks, `/${normalizedLanguage}/parks`),
        this.item(3, parkName, parkPath),
        this.item(4, copy.history, historyPath),
        this.item(5, copy.comparison(fromYear, toYear), canonicalPath)
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
