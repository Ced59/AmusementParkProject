import { TestBed } from '@angular/core/testing';

import { SeoRoutePolicyService } from './seo-route-policy.service';

describe('SeoRoutePolicyService', (): void => {
  let service: SeoRoutePolicyService;

  beforeEach((): void => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(SeoRoutePolicyService);
  });

  it('classifies private routes without matching similarly named public routes', (): void => {
    expect(service.isAdminRoute('/fr/admin/content')).toBe(true);
    expect(service.isAdminRoute('/fr/administration-guide')).toBe(false);
    expect(service.isAccountRoute('/en/reset-password/token')).toBe(true);
    expect(service.isAccountRoute('/en/park/reset-password-land')).toBe(false);
  });

  it('recognizes only the neutral root as the language entry route', (): void => {
    expect(service.isLanguageEntryRoute('/')).toBe(true);
    expect(service.isLanguageEntryRoute('/?source=bookmark')).toBe(true);
    expect(service.isLanguageEntryRoute('/fr/home')).toBe(false);
  });

  it('marks only query-filtered park collection and subpage routes', (): void => {
    expect(
      service.isFilteredPublicParkRoute('/fr/park/id/slug/items?page=2'),
    ).toBe(true);
    expect(
      service.isFilteredPublicParkRoute(
        '/fr/park/id/slug/item/item-id/item-slug/images?sort=date',
      ),
    ).toBe(true);
    expect(
      service.isFilteredPublicParkRoute('/fr/park/id/slug/map?closed=all'),
    ).toBe(true);
    expect(
      service.isFilteredPublicParkRoute('/fr/park/id/slug/pricing?campaign=spring'),
    ).toBe(true);
    expect(
      service.isFilteredPublicParkRoute('/fr/park/id/slug/comments?page=2'),
    ).toBe(true);
    expect(service.isFilteredPublicParkRoute('/fr/park/id/slug/items')).toBe(
      false,
    );
    expect(service.isFilteredPublicParkRoute('/fr/park/id/slug?ref=home')).toBe(
      false,
    );
  });

  it('resolves localized static routes and absolute URLs', (): void => {
    expect(
      service.resolveLanguage(
        'https://amusement-parks.fun/de/privacy?source=test',
      ),
    ).toBe('de');
    expect(service.resolveStaticRouteKey('/de/privacy')).toBe('privacy');
    expect(service.resolveStaticRouteKey('/fr/not-found')).toBe('notFound');
    expect(service.resolveStaticRouteKey('/fr/park/id/slug')).toBeNull();
    expect(service.resolveStaticRouteKey('/fr/constructor')).toBeNull();
  });

  it('normalizes duplicate separators for path segments', (): void => {
    expect(service.getPathSegments('/fr//park///id/slug')).toEqual([
      'fr',
      'park',
      'id',
      'slug',
    ]);
  });

  it('recognizes only shared user ranking detail routes', (): void => {
    expect(service.isSharedUserRankingRoute('/fr/rankings/shared/opaque-token')).toBe(true);
    expect(service.isSharedUserRankingRoute('/fr/rankings/shared/opaque-token?category=Attraction')).toBe(true);
    expect(service.isSharedUserRankingRoute('/fr/rankings/shared')).toBe(false);
    expect(service.isSharedUserRankingRoute('/fr/rankings')).toBe(false);
  });
});
