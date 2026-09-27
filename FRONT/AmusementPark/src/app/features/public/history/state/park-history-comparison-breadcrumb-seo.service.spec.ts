import { CanonicalUrlService } from '@core/seo/canonical-url.service';
import { JsonLdService } from '@core/seo/json-ld.service';
import { ParkHistoryComparisonBreadcrumbSeoService } from './park-history-comparison-breadcrumb-seo.service';

describe('ParkHistoryComparisonBreadcrumbSeoService', () => {
  it('publishes both compared years in the localized contextual breadcrumb', () => {
    const canonicalUrlService = {
      buildAbsoluteUrl: vi.fn((path: string): string => `https://amusement-parks.fun${path}`)
    } as unknown as CanonicalUrlService;
    const jsonLdService = {
      replaceJsonLdByType: vi.fn()
    } as unknown as JsonLdService;
    const service = new ParkHistoryComparisonBreadcrumbSeoService(
      canonicalUrlService,
      jsonLdService
    );

    service.apply(
      'park-1',
      'Parc Exemple',
      'parc-exemple',
      'fr',
      '/fr/park/park-1/parc-exemple/history/compare/1998/2026',
      1998,
      2026
    );

    expect(jsonLdService.replaceJsonLdByType).toHaveBeenCalledWith(
      'BreadcrumbList',
      expect.objectContaining({
        itemListElement: expect.arrayContaining([
          expect.objectContaining({ position: 3, name: 'Parc Exemple' }),
          expect.objectContaining({ position: 4, name: 'Histoire' }),
          expect.objectContaining({
            position: 5,
            name: '1998 face à 2026',
            item: 'https://amusement-parks.fun/fr/park/park-1/parc-exemple/history/compare/1998/2026'
          })
        ])
      })
    );
  });
});
