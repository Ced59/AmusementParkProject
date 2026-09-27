import { CanonicalUrlService } from '@core/seo/canonical-url.service';
import { JsonLdService } from '@core/seo/json-ld.service';
import { HistoricalLineageBreadcrumbSeoService } from './historical-lineage-breadcrumb-seo.service';

describe('HistoricalLineageBreadcrumbSeoService', () => {
  it('publishes the complete localized park and history context', () => {
    const canonicalUrlService = {
      buildAbsoluteUrl: vi.fn((path: string): string => `https://amusement-parks.fun${path}`)
    } as unknown as CanonicalUrlService;
    const jsonLdService = {
      replaceJsonLdByType: vi.fn()
    } as unknown as JsonLdService;
    const service = new HistoricalLineageBreadcrumbSeoService(canonicalUrlService, jsonLdService);

    service.apply(
      'fr',
      'Lignée historique de Le Grand Huit',
      '/fr/history/lineages/park-1/parkitem/item-1/le-grand-huit',
      {
        parkName: 'Parc exemple',
        parkPath: '/fr/park/park-1/parc-exemple',
        historyPath: '/fr/park/park-1/parc-exemple/history'
      }
    );

    expect(jsonLdService.replaceJsonLdByType).toHaveBeenCalledWith(
      'BreadcrumbList',
      expect.objectContaining({
        itemListElement: [
          expect.objectContaining({ position: 1, name: 'Accueil' }),
          expect.objectContaining({ position: 2, name: 'Parcs' }),
          expect.objectContaining({ position: 3, name: 'Parc exemple' }),
          expect.objectContaining({ position: 4, name: 'Histoire' }),
          expect.objectContaining({
            position: 5,
            name: 'Lignée historique de Le Grand Huit',
            item: 'https://amusement-parks.fun/fr/history/lineages/park-1/parkitem/item-1/le-grand-huit'
          })
        ]
      })
    );
  });
});
