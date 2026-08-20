import { CanonicalUrlService } from '@core/seo/canonical-url.service';
import { JsonLdService } from '@core/seo/json-ld.service';
import { HistoryTimelinePageViewModel } from '../models/history-view.model';
import { HistoryStandaloneBreadcrumbSeoService } from './history-standalone-breadcrumb-seo.service';

describe('HistoryStandaloneBreadcrumbSeoService', () => {
  it('replaces the breadcrumb JSON-LD with home, attraction and history for a standalone timeline', () => {
    const canonicalUrlService = {
      buildAbsoluteUrl: vi.fn((path: string): string => `https://amusement-parks.fun${path}`),
      buildCanonicalFromCurrentUrl: vi.fn((path: string): string => `https://amusement-parks.fun${path}`)
    } as unknown as CanonicalUrlService;
    const jsonLdService = {
      replaceJsonLdByType: vi.fn()
    } as unknown as JsonLdService;
    const service = new HistoryStandaloneBreadcrumbSeoService(canonicalUrlService, jsonLdService);
    const timeline: HistoryTimelinePageViewModel = {
      entityType: 'StandaloneAttraction',
      title: 'Histoire de Pendolino',
      subtitle: '',
      ownerName: 'Pendolino',
      park: null,
      parkItem: null,
      standaloneAttraction: {
        id: 'standalone-1',
        name: 'Pendolino',
        countryCode: 'AT',
        type: 'RollerCoaster',
        latitude: 46.561236,
        longitude: 13.253481,
        isVisible: true,
        adminReviewStatus: 'Validated'
      },
      includedParkItems: [],
      showParkItemControls: false,
      events: [],
      pagination: null,
      pageRanges: [],
      yearStart: 2007,
      yearEnd: 2007
    };

    service.apply(timeline, 'fr', '/fr/attraction/standalone-1/pendolino/history');

    expect(jsonLdService.replaceJsonLdByType).toHaveBeenCalledTimes(1);
    expect(jsonLdService.replaceJsonLdByType).toHaveBeenCalledWith(
      'BreadcrumbList',
      expect.objectContaining({
        '@type': 'BreadcrumbList',
        itemListElement: [
          expect.objectContaining({ position: 1, name: 'Accueil' }),
          expect.objectContaining({ position: 2, name: 'Pendolino', item: 'https://amusement-parks.fun/fr/attraction/standalone-1/pendolino' }),
          expect.objectContaining({ position: 3, name: 'Histoire de Pendolino', item: 'https://amusement-parks.fun/fr/attraction/standalone-1/pendolino/history' })
        ]
      })
    );
  });
});
