import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';

import { SeoService } from './seo.service';
import { ParkDetailViewModel } from '@features/public/parks/models/park-detail-view.model';
import { ParkItemDetailViewModel } from '@features/public/park-items/models/park-item-detail-view.model';
import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { VideoDto } from '@app/models/videos/video-dto';
import { VideoHostingProvider } from '@app/models/videos/video-hosting-provider';
import { VideoOwnerType } from '@app/models/videos/video-owner-type';
import { VideoType } from '@app/models/videos/video-type';

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

    expect(readMetaContent('meta[property="og:image"]')).toBe('https://localhost:44391/images/park-photo%201?width=1200&v=2');
    expect(readMetaContent('meta[property="og:image:secure_url"]')).toBe('https://localhost:44391/images/park-photo%201?width=1200&v=2');
    expect(readMetaContent('meta[property="og:image:width"]')).toBe('1200');
    expect(readMetaContent('meta[property="og:image:alt"]')).toBe('Demo Park');
    expect(readMetaContent('meta[name="twitter:image"]')).toBe('https://localhost:44391/images/park-photo%201?width=1200&v=2');
  });

  it('uses the park item hero photo as the Open Graph image', () => {
    const detail: ParkItemDetailViewModel = buildParkItemDetail({
      heroPhoto: {
        imageId: 'item-photo-1',
      } as ParkItemDetailViewModel['heroPhoto']
    });

    service.applyParkItemDetailSeo(detail, 'fr', '/fr/park/park-1/demo-park/item/item-1/demo-item');

    expect(readMetaContent('meta[property="og:image"]')).toBe('https://localhost:44391/images/item-photo-1?width=1200&v=2');
    expect(readMetaContent('meta[name="twitter:image"]')).toBe('https://localhost:44391/images/item-photo-1?width=1200&v=2');
  });

  it('falls back to the site social image when the page has no main photo', () => {
    const park: ParkDetailViewModel = buildParkDetail({
      primaryPhoto: null
    });

    service.applyParkDetailSeo(park, 'fr', '/fr/park/park-1/demo-park');

    expect(readMetaContent('meta[property="og:image"]')).toBe('http://localhost:4200/assets/general-icon/logo-amusementpark.png');
    expect(readMetaContent('meta[name="twitter:image"]')).toBe('http://localhost:4200/assets/general-icon/logo-amusementpark.png');
  });

  it('uses the park gallery photo as the Open Graph image without falling back to the park logo', () => {
    const park: Park = buildPark({
      currentLogoImageId: 'park-logo-1'
    });

    service.applyParkImagesSeo(park, 'fr', '/fr/park/park-1/demo-park/photos', 4, 'gallery image 1');

    expect(readMetaContent('meta[property="og:image"]')).toBe('https://localhost:44391/images/gallery%20image%201?width=1200&v=2');
    expect(readMetaContent('meta[name="twitter:image"]')).toBe('https://localhost:44391/images/gallery%20image%201?width=1200&v=2');
  });

  it('uses the park main photo when a park video has no thumbnail', () => {
    const video: VideoDto = buildVideo({ thumbnailImageId: null, thumbnailUrl: null });

    service.applyParkVideoSeo(video, buildPark(), 'fr', '/fr/park/park-1/demo-park/videos/video-1/demo-video', 'park-main-1');

    expect(readMetaContent('meta[property="og:image"]')).toBe('https://localhost:44391/images/park-main-1?width=1200&v=2');
    expect(readMetaContent('meta[name="twitter:image"]')).toBe('https://localhost:44391/images/park-main-1?width=1200&v=2');
  });

  it('uses the park item main photo when a park item video has no thumbnail', () => {
    const video: VideoDto = buildVideo({
      ownerType: VideoOwnerType.PARK_ITEM,
      ownerId: 'item-1',
      thumbnailImageId: null,
      thumbnailUrl: null
    });

    service.applyParkItemVideoSeo(
      video,
      buildParkItem(),
      buildPark(),
      'fr',
      '/fr/park/park-1/demo-park/item/item-1/demo-item/videos/video-1/demo-video',
      'item-main-1'
    );

    expect(readMetaContent('meta[property="og:image"]')).toBe('https://localhost:44391/images/item-main-1?width=1200&v=2');
    expect(readMetaContent('meta[name="twitter:image"]')).toBe('https://localhost:44391/images/item-main-1?width=1200&v=2');
  });

  it('uses the first video thumbnail before the park main photo on video lists', () => {
    service.applyParkVideosSeo(
      buildPark(),
      'fr',
      '/fr/park/park-1/demo-park/videos',
      2,
      'video-thumb-1',
      'park-main-1'
    );

    expect(readMetaContent('meta[property="og:image"]')).toBe('https://localhost:44391/images/video-thumb-1?width=1200&v=2');
    expect(readMetaContent('meta[name="twitter:image"]')).toBe('https://localhost:44391/images/video-thumb-1?width=1200&v=2');
  });

  it('uses the park item main photo instead of an external thumbnail URL on park item video lists', () => {
    service.applyParkItemVideosSeo(
      buildParkItem(),
      buildPark(),
      'fr',
      '/fr/park/park-1/demo-park/item/item-1/demo-item/videos',
      2,
      'https://cdn.example.com/thumb.jpg',
      'item-main-1'
    );

    expect(readMetaContent('meta[property="og:image"]')).toBe('https://localhost:44391/images/item-main-1?width=1200&v=2');
    expect(readMetaContent('meta[name="twitter:image"]')).toBe('https://localhost:44391/images/item-main-1?width=1200&v=2');
  });

  it('uses the park main photo instead of an external thumbnail URL on park video detail pages', () => {
    const video: VideoDto = buildVideo({
      thumbnailImageId: null,
      thumbnailUrl: 'https://cdn.example.com/thumb.jpg'
    });

    service.applyParkVideoSeo(video, buildPark(), 'fr', '/fr/park/park-1/demo-park/videos/video-1/demo-video', 'park-main-1');

    expect(readMetaContent('meta[property="og:image"]')).toBe('https://localhost:44391/images/park-main-1?width=1200&v=2');
    expect(readMetaContent('meta[name="twitter:image"]')).toBe('https://localhost:44391/images/park-main-1?width=1200&v=2');
  });

  it('uses localized French fallbacks for park item video social metadata', () => {
    const video: VideoDto = buildVideo({
      ownerType: VideoOwnerType.PARK_ITEM,
      ownerId: 'item-1',
      description: 'Watch this English fallback.',
      descriptions: [],
      thumbnailImageId: 'video-thumb-1'
    });

    service.applyParkItemVideoSeo(
      video,
      buildParkItem({ name: 'River Quest' }),
      buildPark({ name: 'Phantasialand' }),
      'fr',
      '/fr/park/park-1/phantasialand/item/item-1/river-quest/video/s/video-1/river-quest',
      'item-main-1'
    );

    expect(readMetaContent('meta[name="description"]')).toBe('Regarde Demo video pour River Quest à Phantasialand.');
    expect(readMetaContent('meta[property="og:description"]')).toBe('Regarde Demo video pour River Quest à Phantasialand.');
    expect(readMetaContent('meta[property="og:locale"]')).toBe('fr_FR');
    expect(readMetaContent('meta[property="og:url"]'))
      .toBe('http://localhost:4200/fr/park/park-1/phantasialand/item/item-1/river-quest/videos/video-1/river-quest');
    expect(readMetaContent('meta[property="og:image"]')).toBe('https://localhost:44391/images/video-thumb-1?width=1200&v=2');
    expect(readMetaContent('meta[property="og:image:alt"]')).toBe('Demo video');
    expect(readMetaContent('meta[name="twitter:image:alt"]')).toBe('Demo video');
  });

  it('uses explicit localized discovery terms for the French park content SEO', () => {
    service.applyParkItemsSeo('Parc Démo', 'fr', '/fr/park/park-1/parc-demo/items');

    expect(documentRef.title).toBe('Attractions, spectacles, restaurants et boutiques à Parc Démo — Amusement Parks');
    expect(readMetaContent('meta[name="description"]'))
      .toBe('Découvre les attractions, spectacles, restaurants, boutiques et lieux pratiques de Parc Démo.');
  });

  it('uses explicit discovery terms for the English park content SEO', () => {
    service.applyParkItemsSeo('Demo Park', 'en', '/en/park/park-1/demo-park/items');

    expect(documentRef.title).toBe('Demo Park attractions, shows, restaurants and shops — Amusement Parks');
    expect(readMetaContent('meta[name="description"]'))
      .toBe('Browse attractions, shows, restaurants, shops and practical places at Demo Park.');
  });

  it('applies indexable localized metadata to the public rankings page', () => {
    service.applyRouteDefaults('/fr/rankings');

    expect(documentRef.title).toBe('Classements — Amusement Parks');
    expect(readMetaContent('meta[name="description"]'))
      .toBe('Découvre les parcs, attractions, restaurants, hôtels et services les plus régulièrement appréciés des visiteurs.');
    expect(readMetaContent('meta[name="robots"]')).toBe('index,follow');
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

function buildPark(overrides: Partial<Park> = {}): Park {
  return {
    id: 'park-1',
    name: 'Demo Park',
    latitude: 50.8,
    longitude: 6.8,
    isVisible: true,
    descriptions: [],
    ...overrides
  };
}

function buildParkItem(overrides: Partial<ParkItem> = {}): ParkItem {
  return {
    id: 'item-1',
    parkId: 'park-1',
    name: 'Demo Item',
    category: 'Attraction',
    type: 'RollerCoaster',
    latitude: 50.8,
    longitude: 6.8,
    isVisible: true,
    ...overrides
  } as ParkItem;
}

function buildVideo(overrides: Partial<VideoDto> = {}): VideoDto {
  return {
    id: 'video-1',
    hostingProvider: VideoHostingProvider.YOUTUBE,
    ownerType: VideoOwnerType.PARK,
    ownerId: 'park-1',
    type: VideoType.OTHER,
    originalUrl: 'https://www.youtube.com/watch?v=demo',
    canonicalUrl: 'https://www.youtube.com/watch?v=demo',
    title: 'Demo video',
    description: 'Demo video description',
    thumbnailUrl: 'https://cdn.example.com/default-thumb.jpg',
    thumbnailImageId: 'video-thumb-1',
    languageCodes: ['fr'],
    titles: [],
    descriptions: [],
    tagIds: [],
    externalMetadata: {},
    isPublished: true,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
    ...overrides
  };
}
