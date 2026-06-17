import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';

import { SeoService } from './seo.service';
import { ParkDetailViewModel } from '@features/public/parks/models/park-detail-view.model';
import { ParkItemDetailViewModel } from '@features/public/park-items/models/park-item-detail-view.model';

describe('SeoService', () => {
  let service: SeoService;
  let documentRef: Document;

  beforeEach(() => {
    TestBed.configureTestingModule({});

    service = TestBed.inject(SeoService);
    documentRef = TestBed.inject(DOCUMENT);
    clearManagedSeoTags();
  });

  afterEach(() => {
    clearManagedSeoTags();
  });

  it('uses the park primary photo as the Open Graph image', () => {
    const park: ParkDetailViewModel = buildParkDetail({
      primaryPhoto: {
        imageId: 'park-photo 1',
      } as ParkDetailViewModel['primaryPhoto']
    });

    service.applyParkDetailSeo(park, 'fr', '/fr/park/park-1/demo-park');

    expect(readMetaContent('meta[property="og:image"]')).toBe('https://localhost:44391/images/park-photo%201');
    expect(readMetaContent('meta[name="twitter:image"]')).toBe('https://localhost:44391/images/park-photo%201');
  });

  it('uses the park item hero photo as the Open Graph image', () => {
    const detail: ParkItemDetailViewModel = buildParkItemDetail({
      heroPhoto: {
        imageId: 'item-photo-1',
      } as ParkItemDetailViewModel['heroPhoto']
    });

    service.applyParkItemDetailSeo(detail, 'fr', '/fr/park/park-1/demo-park/item/item-1/demo-item');

    expect(readMetaContent('meta[property="og:image"]')).toBe('https://localhost:44391/images/item-photo-1');
    expect(readMetaContent('meta[name="twitter:image"]')).toBe('https://localhost:44391/images/item-photo-1');
  });

  it('falls back to the official site logo when the page has no main photo', () => {
    const park: ParkDetailViewModel = buildParkDetail({
      primaryPhoto: null
    });

    service.applyParkDetailSeo(park, 'fr', '/fr/park/park-1/demo-park');

    expect(readMetaContent('meta[property="og:image"]')).toBe('http://localhost:4200/assets/general-icon/logo-amusementpark.png');
    expect(readMetaContent('meta[name="twitter:image"]')).toBe('http://localhost:4200/assets/general-icon/logo-amusementpark.png');
  });

  function readMetaContent(selector: string): string | null {
    return documentRef.head.querySelector<HTMLMetaElement>(selector)?.content ?? null;
  }

  function clearManagedSeoTags(): void {
    documentRef.head.querySelectorAll(
      [
        'meta[name="description"]',
        'meta[name="robots"]',
        'meta[name="googlebot"]',
        'meta[name^="twitter:"]',
        'meta[property^="og:"]',
        'link[rel="canonical"]',
        'link[rel="alternate"]',
        'script[data-managed-by="amusementpark-seo"]'
      ].join(',')
    ).forEach((element: Element): void => {
      element.remove();
    });
  }
});

function buildParkDetail(overrides: Partial<ParkDetailViewModel> = {}): ParkDetailViewModel {
  return {
    name: 'Demo Park',
    description: 'Demo park description',
    countryCode: null,
    city: null,
    street: null,
    postalCode: null,
    websiteUrl: null,
    latitude: null,
    longitude: null,
    primaryPhoto: null,
    ...overrides
  } as ParkDetailViewModel;
}

function buildParkItemDetail(overrides: Partial<ParkItemDetailViewModel> = {}): ParkItemDetailViewModel {
  return {
    name: 'Demo Item',
    description: 'Demo item description',
    parkName: null,
    parkLink: null,
    manufacturerName: null,
    heroPhoto: null,
    ...overrides
  } as ParkItemDetailViewModel;
}
