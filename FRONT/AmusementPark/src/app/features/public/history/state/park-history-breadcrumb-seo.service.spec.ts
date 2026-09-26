import { CanonicalUrlService } from '@core/seo/canonical-url.service';
import { JsonLdService } from '@core/seo/json-ld.service';
import { ParkHistoryBreadcrumbSeoService } from './park-history-breadcrumb-seo.service';

describe('ParkHistoryBreadcrumbSeoService', () => {
  it('publishes a localized and contextual breadcrumb for an annual snapshot', () => {
    const canonicalUrlService = {
      buildAbsoluteUrl: vi.fn((path: string): string => `https://amusement-parks.fun${path}`)
    } as unknown as CanonicalUrlService;
    const jsonLdService = {
      replaceJsonLdByType: vi.fn()
    } as unknown as JsonLdService;
    const service = new ParkHistoryBreadcrumbSeoService(canonicalUrlService, jsonLdService);

    service.apply(
      'park-1',
      'Parc Exemple',
      'parc-exemple',
      'fr',
      '/fr/park/park-1/parc-exemple/history/1998',
      1998
    );

    expect(jsonLdService.replaceJsonLdByType).toHaveBeenCalledWith(
      'BreadcrumbList',
      expect.objectContaining({
        '@type': 'BreadcrumbList',
        itemListElement: [
          expect.objectContaining({ position: 1, name: 'Accueil' }),
          expect.objectContaining({ position: 2, name: 'Parcs' }),
          expect.objectContaining({ position: 3, name: 'Parc Exemple' }),
          expect.objectContaining({ position: 4, name: 'Histoire' }),
          expect.objectContaining({
            position: 5,
            name: 'En 1998',
            item: 'https://amusement-parks.fun/fr/park/park-1/parc-exemple/history/1998'
          })
        ]
      })
    );
  });

  it('falls back to English for an unsupported route language', () => {
    const canonicalUrlService = {
      buildAbsoluteUrl: vi.fn((path: string): string => `https://amusement-parks.fun${path}`)
    } as unknown as CanonicalUrlService;
    const jsonLdService = {
      replaceJsonLdByType: vi.fn()
    } as unknown as JsonLdService;
    const service = new ParkHistoryBreadcrumbSeoService(canonicalUrlService, jsonLdService);

    service.apply('park-1', 'Example Park', 'example-park', 'unsupported', '/en/park/park-1/example-park/history');

    expect(jsonLdService.replaceJsonLdByType).toHaveBeenCalledWith(
      'BreadcrumbList',
      expect.objectContaining({
        itemListElement: expect.arrayContaining([
          expect.objectContaining({ name: 'Home' }),
          expect.objectContaining({ name: 'Parks' }),
          expect.objectContaining({ name: 'History' })
        ])
      })
    );
  });
});
