import { TestBed } from '@angular/core/testing';

import { SeoRoutePolicyService } from './seo-route-policy.service';

describe('SeoRoutePolicyService', (): void => {
  let service: SeoRoutePolicyService;

  beforeEach((): void => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(SeoRoutePolicyService);
  });

  it('classifies private routes without matching similarly named public routes', (): void => {
    expect(service.isAdminRoute('/fr/admin/content')).toBeTrue();
    expect(service.isAdminRoute('/fr/administration-guide')).toBeFalse();
    expect(service.isAccountRoute('/en/reset-password/token')).toBeTrue();
    expect(service.isAccountRoute('/en/park/reset-password-land')).toBeFalse();
  });

  it('marks only query-filtered park collection and subpage routes', (): void => {
    expect(service.isFilteredPublicParkRoute('/fr/park/id/slug/items?page=2')).toBeTrue();
    expect(service.isFilteredPublicParkRoute('/fr/park/id/slug/item/item-id/item-slug/images?sort=date')).toBeTrue();
    expect(service.isFilteredPublicParkRoute('/fr/park/id/slug/items')).toBeFalse();
    expect(service.isFilteredPublicParkRoute('/fr/park/id/slug?ref=home')).toBeFalse();
  });

  it('resolves localized static routes and absolute URLs', (): void => {
    expect(service.resolveLanguage('https://amusement-parks.fun/de/privacy?source=test')).toBe('de');
    expect(service.resolveStaticRouteKey('/de/privacy')).toBe('privacy');
    expect(service.resolveStaticRouteKey('/fr/not-found')).toBe('notFound');
    expect(service.resolveStaticRouteKey('/fr/park/id/slug')).toBeNull();
    expect(service.resolveStaticRouteKey('/fr/constructor')).toBeNull();
  });

  it('normalizes duplicate separators for path segments', (): void => {
    expect(service.getPathSegments('/fr//park///id/slug')).toEqual(['fr', 'park', 'id', 'slug']);
  });
});
