import { Injectable } from '@angular/core';

import { CanonicalUrlService } from '@core/seo/canonical-url.service';
import { JsonLdService } from '@core/seo/json-ld.service';
import { buildPublicRoutePath, buildPublicStandaloneAttractionRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
import { HistoryTimelinePageViewModel } from '../models/history-view.model';

interface StandaloneHistoryBreadcrumbCopy {
  home: string;
  history: (ownerName: string) => string;
}

const STANDALONE_HISTORY_BREADCRUMB_COPY: Record<string, StandaloneHistoryBreadcrumbCopy> = {
  fr: { home: 'Accueil', history: (ownerName: string): string => `Histoire de ${ownerName}` },
  en: { home: 'Home', history: (ownerName: string): string => `${ownerName} history` },
  de: { home: 'Startseite', history: (ownerName: string): string => `Geschichte von ${ownerName}` },
  nl: { home: 'Home', history: (ownerName: string): string => `Geschiedenis van ${ownerName}` },
  it: { home: 'Home', history: (ownerName: string): string => `Storia di ${ownerName}` },
  es: { home: 'Inicio', history: (ownerName: string): string => `Historia de ${ownerName}` },
  pl: { home: 'Strona główna', history: (ownerName: string): string => `Historia ${ownerName}` },
  pt: { home: 'Início', history: (ownerName: string): string => `História de ${ownerName}` }
};

@Injectable({ providedIn: 'root' })
export class HistoryStandaloneBreadcrumbSeoService {
  constructor(
    private readonly canonicalUrlService: CanonicalUrlService,
    private readonly jsonLdService: JsonLdService
  ) {
  }

  apply(timeline: HistoryTimelinePageViewModel, language: string, canonicalPath: string | null): void {
    const attraction = timeline.standaloneAttraction;
    if (!attraction?.id || !attraction.name || !canonicalPath) {
      return;
    }

    const normalizedLanguage: string = STANDALONE_HISTORY_BREADCRUMB_COPY[language]
      ? language
      : 'en';
    const copy: StandaloneHistoryBreadcrumbCopy = STANDALONE_HISTORY_BREADCRUMB_COPY[normalizedLanguage];
    const attractionPath: string | null = buildPublicRoutePath(buildPublicStandaloneAttractionRouteCommands({
      language: normalizedLanguage,
      attractionId: attraction.id,
      attractionName: attraction.name
    }));

    if (!attractionPath) {
      return;
    }

    const currentUrl: string = this.canonicalUrlService.buildCanonicalFromCurrentUrl(canonicalPath);
    const breadcrumb = {
      '@context': 'https://schema.org',
      '@type': 'BreadcrumbList',
      itemListElement: [
        {
          '@type': 'ListItem',
          position: 1,
          name: copy.home,
          item: this.canonicalUrlService.buildAbsoluteUrl(`/${normalizedLanguage}/home`)
        },
        {
          '@type': 'ListItem',
          position: 2,
          name: attraction.name,
          item: this.canonicalUrlService.buildAbsoluteUrl(attractionPath)
        },
        {
          '@type': 'ListItem',
          position: 3,
          name: copy.history(attraction.name),
          item: currentUrl
        }
      ]
    };

    this.jsonLdService.replaceJsonLdByType('BreadcrumbList', breadcrumb);
  }
}
