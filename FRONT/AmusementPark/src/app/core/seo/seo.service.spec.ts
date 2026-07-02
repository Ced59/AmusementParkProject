import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';

import { SeoService } from './seo.service';
import { ParkDetailViewModel } from '@features/public/parks/models/park-detail-view.model';
import { ParkReferenceDetailViewModel } from '@features/public/parks/models/park-reference-detail-view.model';
import { ParkItemDetailViewModel } from '@features/public/park-items/models/park-item-detail-view.model';
import { HistoryTimelinePageViewModel } from '@features/public/history/models/history-view.model';
import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { TechnicalPage } from '@app/models/technical-pages/technical-page';
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

    expect(readMetaContent('meta[property="og:image"]')).toBe('https://localhost:44391/images/binary/park-photo%201?width=1200&v=2');
    expect(readMetaContent('meta[property="og:image:secure_url"]')).toBe('https://localhost:44391/images/binary/park-photo%201?width=1200&v=2');
    expect(readMetaContent('meta[property="og:image:width"]')).toBe('1200');
    expect(readMetaContent('meta[property="og:image:height"]')).toBe('630');
    expect(readMetaContent('meta[property="og:image:alt"]')).toBe('Demo Park');
    expect(readMetaContent('meta[name="twitter:image"]')).toBe('https://localhost:44391/images/binary/park-photo%201?width=1200&v=2');
  });

  it('uses the park item hero photo as the Open Graph image', () => {
    const detail: ParkItemDetailViewModel = buildParkItemDetail({
      heroPhoto: {
        imageId: 'item-photo-1',
      } as ParkItemDetailViewModel['heroPhoto']
    });

    service.applyParkItemDetailSeo(detail, 'fr', '/fr/park/park-1/demo-park/item/item-1/demo-item');

    expect(readMetaContent('meta[property="og:image"]')).toBe('https://localhost:44391/images/binary/item-photo-1?width=1200&v=2');
    expect(readMetaContent('meta[name="twitter:image"]')).toBe('https://localhost:44391/images/binary/item-photo-1?width=1200&v=2');
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

    expect(readMetaContent('meta[property="og:image"]')).toBe('https://localhost:44391/images/binary/gallery%20image%201?width=1200&v=2');
    expect(readMetaContent('meta[name="twitter:image"]')).toBe('https://localhost:44391/images/binary/gallery%20image%201?width=1200&v=2');
  });

  it('uses the park main photo when a park video has no thumbnail', () => {
    const video: VideoDto = buildVideo({ thumbnailImageId: null, thumbnailUrl: null });

    service.applyParkVideoSeo(video, buildPark(), 'fr', '/fr/park/park-1/demo-park/videos/video-1/demo-video', 'park-main-1');

    expect(readMetaContent('meta[property="og:image"]')).toBe('https://localhost:44391/images/binary/park-main-1?width=1200&v=2');
    expect(readMetaContent('meta[name="twitter:image"]')).toBe('https://localhost:44391/images/binary/park-main-1?width=1200&v=2');
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

    expect(readMetaContent('meta[property="og:image"]')).toBe('https://localhost:44391/images/binary/item-main-1?width=1200&v=2');
    expect(readMetaContent('meta[name="twitter:image"]')).toBe('https://localhost:44391/images/binary/item-main-1?width=1200&v=2');
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

    expect(readMetaContent('meta[property="og:image"]')).toBe('https://localhost:44391/images/binary/video-thumb-1?width=1200&v=2');
    expect(readMetaContent('meta[name="twitter:image"]')).toBe('https://localhost:44391/images/binary/video-thumb-1?width=1200&v=2');
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

    expect(readMetaContent('meta[property="og:image"]')).toBe('https://localhost:44391/images/binary/item-main-1?width=1200&v=2');
    expect(readMetaContent('meta[name="twitter:image"]')).toBe('https://localhost:44391/images/binary/item-main-1?width=1200&v=2');
  });

  it('uses the park main photo instead of an external thumbnail URL on park video detail pages', () => {
    const video: VideoDto = buildVideo({
      thumbnailImageId: null,
      thumbnailUrl: 'https://cdn.example.com/thumb.jpg'
    });

    service.applyParkVideoSeo(video, buildPark(), 'fr', '/fr/park/park-1/demo-park/videos/video-1/demo-video', 'park-main-1');

    expect(readMetaContent('meta[property="og:image"]')).toBe('https://localhost:44391/images/binary/park-main-1?width=1200&v=2');
    expect(readMetaContent('meta[name="twitter:image"]')).toBe('https://localhost:44391/images/binary/park-main-1?width=1200&v=2');
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
    expect(readMetaContent('meta[property="og:image"]')).toBe('https://localhost:44391/images/binary/video-thumb-1?width=1200&v=2');
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

  it('applies indexable localized metadata to park opening hours pages without duplicating language copy', () => {
    service.applyParkOpeningHoursSeo('Parc Demo', 'fr', '/fr/park/park-1/parc-demo/opening-hours', 12);

    const frenchTitle: string = documentRef.title;
    const frenchDescription: string | null = readMetaContent('meta[name="description"]');

    expect(frenchTitle).toBe('Dates et horaires de Parc Demo - Amusement Parks');
    expect(frenchDescription).toContain('Parc Demo');
    expect(readMetaContent('meta[name="robots"]')).toBe('index,follow');
    expect(readCanonicalHref()).toBe('http://localhost:4200/fr/park/park-1/parc-demo/opening-hours');

    service.applyParkOpeningHoursSeo('Demo Park', 'en', '/en/park/park-1/demo-park/opening-hours?from=2026-07-01', 12, null, '/en/park/park-1/demo-park/opening-hours');

    expect(documentRef.title).toBe('Dates and opening hours for Demo Park - Amusement Parks');
    expect(readMetaContent('meta[name="description"]')).not.toBe(frenchDescription);
    expect(documentRef.title).not.toBe(frenchTitle);
    expect(readMetaContent('meta[name="robots"]')).toBe('noindex,follow');
    expect(readCanonicalHref()).toBe('http://localhost:4200/en/park/park-1/demo-park/opening-hours');
  });

  it('keeps history timeline SEO titles and descriptions unique between public languages', () => {
    const cases: Array<{ language: string; title: string; url: string }> = [
      { language: 'fr', title: 'Histoire de Mirapolis', url: '/fr/park/park-1/mirapolis/history' },
      { language: 'en', title: 'Mirapolis history', url: '/en/park/park-1/mirapolis/history' },
      { language: 'de', title: 'Geschichte von Mirapolis', url: '/de/park/park-1/mirapolis/history' },
      { language: 'nl', title: 'Geschiedenis van Mirapolis', url: '/nl/park/park-1/mirapolis/history' },
      { language: 'it', title: 'Storia di Mirapolis', url: '/it/park/park-1/mirapolis/history' },
      { language: 'es', title: 'Historia de Mirapolis', url: '/es/park/park-1/mirapolis/history' },
      { language: 'pl', title: 'Historia Mirapolis', url: '/pl/park/park-1/mirapolis/history' },
      { language: 'pt', title: 'História de Mirapolis', url: '/pt/park/park-1/mirapolis/history' }
    ];
    const titles = new Set<string>();
    const descriptions = new Set<string>();

    for (const entry of cases) {
      service.applyHistoryTimelineSeo(buildHistoryTimeline({ title: entry.title }), entry.language, entry.url);

      titles.add(documentRef.title);
      descriptions.add(readMetaContent('meta[name="description"]') ?? '');
      expect(readMetaContent('meta[name="robots"]')).toBe('index,follow');
      expect(readCanonicalHref()).toBe(`http://localhost:4200${entry.url}`);
    }

    expect(titles.size).toBe(cases.length);
    expect(descriptions.size).toBe(cases.length);
  });

  it('localizes opening hours breadcrumb JSON-LD with contextual German labels', () => {
    service.applyParkOpeningHoursSeo('Phantasialand', 'de', '/de/park/park-1/phantasialand/opening-hours', 12);

    expect(readBreadcrumbNames()).toEqual([
      'Startseite',
      'Parkliste',
      'Phantasialand',
      'Öffnungszeiten von Phantasialand'
    ]);
  });

  it('uses contextual French video breadcrumb JSON-LD labels', () => {
    service.applyParkVideoSeo(
      buildVideo({ title: 'La Catapulte offride' }),
      buildPark({ name: 'Le Fleury' }),
      'fr',
      '/fr/park/park-1/le-fleury/videos/video-1/la-catapulte-offride'
    );

    expect(readBreadcrumbNames()).toEqual([
      'Accueil',
      'Liste des parcs',
      'Le Fleury',
      'Vidéos de Le Fleury',
      'La Catapulte offride'
    ]);
  });

  it('uses a clean canonical route for stale park zone slugs', () => {
    service.applyParkZoneSeo(
      'Europa-Park',
      'Monaco themed area',
      'en',
      '/en/park/park-1/europa-park/zone/zone-1/monaco',
      null,
      12,
      '/en/park/park-1/europa-park/zone/zone-1/monaco-themed-area'
    );

    expect(documentRef.title).toBe('Monaco themed area at Europa-Park — Amusement Parks');
    expect(readCanonicalHref()).toBe('http://localhost:4200/en/park/park-1/europa-park/zone/zone-1/monaco-themed-area');
    expect(readMetaContent('meta[name="description"]')).toContain('12 listed places');
  });

  it('localizes park zone list metadata', () => {
    service.applyParkZonesSeo(
      'Phantasialand',
      'fr',
      '/fr/park/park-1/phantasialand/zones',
      null,
      6,
      42
    );

    expect(documentRef.title).toBe('Zones de Phantasialand — Amusement Parks');
    expect(readMetaContent('meta[name="description"]')).toContain('6 zones');
    expect(readMetaContent('meta[name="description"]')).toContain('42 lieux répertoriés');
  });

  it('applies specific metadata to public park reference detail pages', () => {
    service.applyParkReferenceSeo(
      buildParkReference({
        id: 'manufacturer-1',
        kind: 'manufacturer',
        name: 'Ride Technic',
        legalName: 'Ride Technic GmbH',
        attractionsPagination: { totalItems: 4 } as ParkReferenceDetailViewModel['attractionsPagination']
      }),
      'fr',
      '/fr/park-manufacturer/manufacturer-1/old-slug',
      '/fr/park-manufacturer/manufacturer-1/ride-technic'
    );

    expect(documentRef.title).toBe('Ride Technic, constructeur — Amusement Parks');
    expect(readCanonicalHref()).toBe('http://localhost:4200/fr/park-manufacturer/manufacturer-1/ride-technic');
    expect(readMetaContent('meta[name="description"]')).toContain('4 attractions liées');
    expect(readMetaContent('meta[name="robots"]')).toBe('index,follow');
  });

  it('uses available attraction specs in fallback descriptions', () => {
    service.applyParkItemDetailSeo(
      buildParkItemDetail({
        description: null,
        parkName: 'Phantasialand',
        manufacturerName: 'Vekoma',
        modelName: 'Flying Coaster',
        zoneName: 'Rookburgh',
        spotlightRows: [
          { labelKey: 'speed', value: '78 km/h' }
        ],
        accessConditions: [
          {
            metrics: [
              { labelKey: 'height', value: '1.30 m', helperKey: null, iconClass: 'pi pi-ruler' }
            ]
          }
        ] as ParkItemDetailViewModel['accessConditions']
      }),
      'fr',
      '/fr/park/park-1/phantasialand/item/item-1/fly',
      '/fr/park/park-1/phantasialand/item/item-1/f-l-y'
    );

    const description: string | null = readMetaContent('meta[name="description"]');

    expect(readCanonicalHref()).toBe('http://localhost:4200/fr/park/park-1/phantasialand/item/item-1/f-l-y');
    expect(description).toContain('Vekoma');
    expect(description).toContain('Flying Coaster');
    expect(description).toContain('78 km/h');
  });

  it('applies indexable localized metadata to the public rankings page', () => {
    service.applyRouteDefaults('/fr/rankings');

    expect(documentRef.title).toBe('Classements — Amusement Parks');
    expect(readMetaContent('meta[name="description"]'))
      .toBe('Découvre les parcs, attractions, restaurants, hôtels et services les plus régulièrement appréciés des visiteurs.');
    expect(readMetaContent('meta[name="robots"]')).toBe('index,follow');
  });

  it('applies indexable localized metadata to the public technical pages list', () => {
    service.applyRouteDefaults('/fr/technical');

    expect(documentRef.title).toBe('Dossiers techniques - Amusement Parks');
    expect(readMetaContent('meta[name="description"]'))
      .toBe('Explore les lifts, retenues, trains, materiaux et autres systemes techniques des attractions.');
    expect(readMetaContent('meta[name="robots"]')).toBe('index,follow');
    expect(readMetaContent('meta[property="og:locale"]')).toBe('fr_FR');
  });

  it('applies localized metadata to a technical detail page', () => {
    service.applyTechnicalPageSeo(buildTechnicalPage(), 'fr', '/fr/technical/lap-bar');

    expect(documentRef.title).toBe('Lap bar - Amusement Parks');
    expect(readMetaContent('meta[name="description"]')).toBe('Explication technique de la lap bar.');
    expect(readMetaContent('meta[property="og:image"]')).toBe('https://localhost:44391/images/binary/technical-photo-1?width=1200&v=2');
    expect(readMetaContent('meta[property="og:url"]')).toBe('http://localhost:4200/fr/technical/lap-bar');
  });

  it('applies indexable localized metadata to the public manufacturers page', () => {
    service.applyRouteDefaults('/fr/manufacturers');

    expect(documentRef.title).toBe("Constructeurs d'attractions - Amusement Parks");
    expect(readMetaContent('meta[name="description"]'))
      .toBe("Parcours les constructeurs d'attractions et de coasters avec leur fiche publique, leur histoire et leurs liens utiles.");
    expect(readMetaContent('meta[name="robots"]')).toBe('index,follow');
  });

  it('applies indexable localized metadata to the public sitemap page', () => {
    service.applyRouteDefaults('/fr/sitemap');

    expect(documentRef.title).toBe('Plan du site - Amusement Parks');
    expect(readMetaContent('meta[name="description"]'))
      .toBe('Explore le plan public d’Amusement Parks avec les parcs, cartes interactives, dossiers techniques et pages de référence.');
    expect(readMetaContent('meta[name="robots"]')).toBe('index,follow');
    expect(readCanonicalHref()).toBe('http://localhost:4200/fr/sitemap');
  });

  it('applies indexable interactive map metadata to public park map pages', () => {
    service.applyParkMapSeo(buildPark({ name: 'Parc Demo' }), 'fr', '/fr/park/park-1/parc-demo/map');

    expect(documentRef.title).toBe('Carte interactive de Parc Demo — Amusement Parks');
    expect(readMetaContent('meta[name="description"]')).toBe('Carte interactive de Parc Demo.');
    expect(readMetaContent('meta[name="robots"]')).toBe('index,follow');
    expect(readCanonicalHref()).toBe('http://localhost:4200/fr/park/park-1/parc-demo/map');
  });

  it('keeps public park map pages noindex when no map marker is available', () => {
    service.applyParkMapSeo(buildPark({ name: 'Parc Demo' }), 'fr', '/fr/park/park-1/parc-demo/map', null, null, false);

    expect(readMetaContent('meta[name="robots"]')).toBe('noindex,follow');
  });

  function readMetaContent(selector: string): string | null {
    return documentRef.head.querySelector<HTMLMetaElement>(selector)?.content ?? null;
  }

  function readCanonicalHref(): string | null {
    return documentRef.head.querySelector<HTMLLinkElement>('link[rel="canonical"]')?.href ?? null;
  }

  function readBreadcrumbNames(): string[] {
    const breadcrumb = readJsonLdScripts()
      .find((value: Record<string, unknown>): boolean => value['@type'] === 'BreadcrumbList');
    const elements: Array<Record<string, unknown>> = Array.isArray(breadcrumb?.['itemListElement'])
      ? breadcrumb['itemListElement'] as Array<Record<string, unknown>>
      : [];

    return elements.map((element: Record<string, unknown>): string => String(element['name'] ?? ''));
  }

  function readJsonLdScripts(): Array<Record<string, unknown>> {
    return Array.from(documentRef.head.querySelectorAll<HTMLScriptElement>('script[data-managed-by="amusementpark-seo"]'))
      .map((script: HTMLScriptElement): Record<string, unknown> => JSON.parse(script.textContent ?? '{}') as Record<string, unknown>);
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

function buildHistoryTimeline(overrides: Partial<HistoryTimelinePageViewModel> = {}): HistoryTimelinePageViewModel {
  return {
    entityType: 'Park',
    title: 'Mirapolis history',
    subtitle: '1 milestone tracing the story of Mirapolis.',
    ownerName: 'Mirapolis',
    park: buildPark({ name: 'Mirapolis' }),
    parkItem: null,
    includedParkItems: [],
    showParkItemControls: false,
    yearStart: 1987,
    yearEnd: 1987,
    events: [
      {
        id: 'event-1',
        key: 'opening',
        title: 'Opening',
        summary: 'Mirapolis opens to visitors.',
        dateLabel: '1987',
        year: 1987,
        month: 5,
        day: 20,
        eventType: 'Opening',
        eventTypeLabel: 'Opening',
        entityType: 'Park',
        isMajor: false,
        ownerName: 'Mirapolis',
        contextParkName: null,
        parkItemName: null,
        mainImageId: null,
        mainImage: null,
        articleLink: null,
        sourceCount: 0,
        positionPercent: 0,
        isFirstInYear: true
      }
    ],
    ...overrides
  };
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

function buildParkReference(overrides: Partial<ParkReferenceDetailViewModel> = {}): ParkReferenceDetailViewModel {
  return {
    id: 'reference-1',
    kind: 'operator',
    name: 'Demo Reference',
    legalName: null,
    richDescription: null,
    badgeKey: 'badge',
    titleKey: 'title',
    descriptionTitleKey: 'description',
    emptyDescriptionKey: 'empty',
    heroIconClass: 'pi pi-building',
    heroLogoImageId: null,
    facts: [],
    photos: [],
    photoCategories: [],
    attractions: [],
    attractionsPagination: null,
    adminParkGraphUpsertJson: null,
    adminParkGraphUpsertFileName: null,
    ...overrides
  };
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

function buildTechnicalPage(overrides: Partial<TechnicalPage> = {}): TechnicalPage {
  return {
    id: 'technical-lap-bar',
    categoryKey: 'restraint',
    categoryNames: [{ languageCode: 'fr', value: 'Retenues' }],
    slug: 'lap-bar',
    titles: [
      { languageCode: 'fr', value: 'Lap bar' },
      { languageCode: 'en', value: 'Lap bar' }
    ],
    summaries: [
      { languageCode: 'fr', value: 'Explication technique de la lap bar.' },
      { languageCode: 'en', value: 'Technical explanation of the lap bar.' }
    ],
    aliases: [],
    contentBlocks: [
      {
        blockType: 'image',
        imageId: 'technical-photo-1'
      }
    ],
    sortOrder: 0,
    isVisible: true,
    adminReviewStatus: 'Validated',
    ...overrides
  };
}
