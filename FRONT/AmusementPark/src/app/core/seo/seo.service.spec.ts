import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';

import { SeoService } from './seo.service';
import { ParkDetailViewModel } from '@features/public/parks/models/park-detail-view.model';
import { ParkReferenceDetailViewModel } from '@features/public/parks/models/park-reference-detail-view.model';
import { ParkItemDetailViewModel } from '@features/public/park-items/models/park-item-detail-view.model';
import { HistoryArticlePageViewModel, HistoryTimelinePageViewModel } from '@features/public/history/models/history-view.model';
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
    expect(readMetaContent('meta[property="og:image:height"]')).toBeNull();
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
    expect(readMetaContent('meta[property="og:image:width"]')).toBe('1024');
    expect(readMetaContent('meta[property="og:image:height"]')).toBe('1024');
    expect(readMetaContent('meta[name="twitter:image"]')).toBe('http://localhost:4200/assets/general-icon/logo-amusementpark.png');
  });

  it('removes stale social image height when a resized image replaces the fallback', () => {
    service.applyParkDetailSeo(buildParkDetail({ primaryPhoto: null }), 'fr', '/fr/park/park-1/demo-park');

    expect(readMetaContent('meta[property="og:image:height"]')).toBe('1024');

    service.applyParkDetailSeo(buildParkDetail({
      primaryPhoto: {
        imageId: 'park-photo-1',
      } as ParkDetailViewModel['primaryPhoto']
    }), 'fr', '/fr/park/park-1/demo-park');

    expect(readMetaContent('meta[property="og:image:width"]')).toBe('1200');
    expect(readMetaContent('meta[property="og:image:height"]')).toBeNull();
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

  it('applies article Open Graph metadata to history articles and resets the type on regular pages', () => {
    service.applyHistoryArticleSeo(
      buildHistoryArticle({
        title: 'Opening of Mirapolis',
        summary: 'Article detaille sur l ouverture de Mirapolis.',
        mainImageId: 'history-photo-1'
      }),
      'fr',
      '/fr/park/park-1/mirapolis/history/event-1/opening'
    );

    expect(readMetaContent('meta[property="og:type"]')).toBe('article');
    expect(readMetaContent('meta[property="og:title"]')).toContain('Opening of Mirapolis');
    expect(readMetaContent('meta[property="og:description"]')).toBe('Article detaille sur l ouverture de Mirapolis.');
    expect(readMetaContent('meta[property="og:image"]')).toBe('https://localhost:44391/images/binary/history-photo-1?width=1200&v=2');
    expect(readMetaContent('meta[property="og:image:alt"]')).toBe('Opening of Mirapolis');
    expect(readMetaContent('meta[name="twitter:image"]')).toBe('https://localhost:44391/images/binary/history-photo-1?width=1200&v=2');

    service.applyParkDetailSeo(buildParkDetail(), 'fr', '/fr/park/park-1/demo-park');

    expect(readMetaContent('meta[property="og:type"]')).toBe('website');
  });

  it('keeps Open Graph titles and descriptions distinct across languages for shareable public pages', () => {
    const languages: readonly string[] = ['en', 'fr', 'de', 'nl', 'it', 'es', 'pl', 'pt'];
    const localizedArticleTitles: Record<string, string> = {
      en: 'Opening of Mirapolis',
      fr: 'Ouverture de Mirapolis',
      de: 'Eröffnung von Mirapolis',
      nl: 'Opening van Mirapolis',
      it: 'Apertura di Mirapolis',
      es: 'Apertura de Mirapolis',
      pl: 'Otwarcie Mirapolis',
      pt: 'Abertura de Mirapolis'
    };
    const cases: Array<{ name: string; apply: (language: string) => void }> = [
      {
        name: 'park images',
        apply: (language: string): void => service.applyParkImagesSeo(
          buildPark({ name: 'Demo Park', countryCode: 'FR' }),
          language,
          `/${language}/park/park-1/demo-park/images`,
          4,
          'park-image-1')
      },
      {
        name: 'park item images',
        apply: (language: string): void => service.applyParkItemImagesSeo(
          buildParkItem({ name: 'Demo Item' }),
          buildPark({ name: 'Demo Park', countryCode: 'FR' }),
          language,
          `/${language}/park/park-1/demo-park/item/item-1/demo-item/images`,
          4,
          'item-image-1')
      },
      {
        name: 'park videos',
        apply: (language: string): void => service.applyParkVideosSeo(
          buildPark({ name: 'Demo Park', countryCode: 'FR' }),
          language,
          `/${language}/park/park-1/demo-park/videos`,
          3,
          'video-thumb-1',
          'park-image-1')
      },
      {
        name: 'park item videos',
        apply: (language: string): void => service.applyParkItemVideosSeo(
          buildParkItem({ name: 'Demo Item' }),
          buildPark({ name: 'Demo Park', countryCode: 'FR' }),
          language,
          `/${language}/park/park-1/demo-park/item/item-1/demo-item/videos`,
          3,
          'video-thumb-1',
          'item-image-1',
          'park-image-1')
      },
      {
        name: 'park map',
        apply: (language: string): void => service.applyParkMapSeo(
          buildPark({ name: 'Demo Park', countryCode: 'FR' }),
          language,
          `/${language}/park/park-1/demo-park/map`,
          'park-image-1')
      },
      {
        name: 'park weather',
        apply: (language: string): void => service.applyParkWeatherSeo(
          'Demo Park',
          language,
          `/${language}/park/park-1/demo-park/weather`,
          7,
          'park-image-1')
      },
      {
        name: 'park opening hours',
        apply: (language: string): void => service.applyParkOpeningHoursSeo(
          'Demo Park',
          language,
          `/${language}/park/park-1/demo-park/opening-hours`,
          30,
          'park-image-1')
      },
      {
        name: 'park zones',
        apply: (language: string): void => service.applyParkZonesSeo(
          'Demo Park',
          language,
          `/${language}/park/park-1/demo-park/zones`,
          'park-image-1',
          5,
          42)
      },
      {
        name: 'park zone',
        apply: (language: string): void => service.applyParkZoneSeo(
          'Demo Park',
          'Old West',
          language,
          `/${language}/park/park-1/demo-park/zone/zone-1/old-west`,
          'park-image-1',
          12)
      },
      {
        name: 'history timeline',
        apply: (language: string): void => service.applyHistoryTimelineSeo(
          buildHistoryTimeline({ title: localizedArticleTitles[language] }),
          language,
          `/${language}/park/park-1/mirapolis/history`)
      },
      {
        name: 'history article',
        apply: (language: string): void => service.applyHistoryArticleSeo(
          buildHistoryArticle({
            title: localizedArticleTitles[language],
            summary: ''
          }),
          language,
          `/${language}/park/park-1/mirapolis/history/event-1/opening`)
      },
      {
        name: 'park reference',
        apply: (language: string): void => service.applyParkReferenceSeo(
          buildParkReference({
            id: 'manufacturer-1',
            kind: 'manufacturer',
            name: 'Ride Technic',
            attractionsPagination: { totalItems: 4 } as ParkReferenceDetailViewModel['attractionsPagination']
          }),
          language,
          `/${language}/park-manufacturer/manufacturer-1/ride-technic`)
      }
    ];

    for (const routeCase of cases) {
      const titles: string[] = [];
      const descriptions: string[] = [];

      for (const language of languages) {
        routeCase.apply(language);

        titles.push(readMetaContent('meta[property="og:title"]') ?? '');
        descriptions.push(readMetaContent('meta[property="og:description"]') ?? '');
      }

      expect(new Set(titles).size).withContext(`${routeCase.name} og:title`).toBe(languages.length);
      expect(new Set(descriptions).size).withContext(`${routeCase.name} og:description`).toBe(languages.length);
    }
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

  it('normalizes legacy park video URLs before building breadcrumb JSON-LD items', () => {
    service.applyParkVideoSeo(
      buildVideo({ title: 'Demo video' }),
      buildPark({ name: 'Demo Park' }),
      'fr',
      '/fr/park/park-1/demo-park/video/s/video-1/demo-video'
    );

    expect(readBreadcrumbUrls()).toEqual([
      'http://localhost:4200/fr/home',
      'http://localhost:4200/fr/parks',
      'http://localhost:4200/fr/park/park-1/demo-park',
      'http://localhost:4200/fr/park/park-1/demo-park/videos',
      'http://localhost:4200/fr/park/park-1/demo-park/videos/video-1/demo-video'
    ]);
  });

  it('normalizes legacy park item video URLs before building breadcrumb JSON-LD items', () => {
    service.applyParkItemVideoSeo(
      buildVideo({ ownerType: VideoOwnerType.PARK_ITEM, ownerId: 'item-1', title: 'Demo item video' }),
      buildParkItem({ name: 'Demo Item' }),
      buildPark({ name: 'Demo Park' }),
      'fr',
      '/fr/park/park-1/demo-park/item/item-1/demo-item/video/s/video-1/demo-video'
    );

    expect(readBreadcrumbUrls()).toEqual([
      'http://localhost:4200/fr/home',
      'http://localhost:4200/fr/parks',
      'http://localhost:4200/fr/park/park-1/demo-park',
      'http://localhost:4200/fr/park/park-1/demo-park/item/item-1/demo-item',
      'http://localhost:4200/fr/park/park-1/demo-park/item/item-1/demo-item/videos',
      'http://localhost:4200/fr/park/park-1/demo-park/item/item-1/demo-item/videos/video-1/demo-video'
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
    const elements: Array<Record<string, unknown>> = readBreadcrumbElements();

    return elements.map((element: Record<string, unknown>): string => String(element['name'] ?? ''));
  }

  function readBreadcrumbUrls(): string[] {
    const elements: Array<Record<string, unknown>> = readBreadcrumbElements();

    return elements.map((element: Record<string, unknown>): string => String(element['item'] ?? ''));
  }

  function readBreadcrumbElements(): Array<Record<string, unknown>> {
    const breadcrumb = readJsonLdScripts()
      .find((value: Record<string, unknown>): boolean => value['@type'] === 'BreadcrumbList');

    return Array.isArray(breadcrumb?.['itemListElement'])
      ? breadcrumb['itemListElement'] as Array<Record<string, unknown>>
      : [];
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

function buildHistoryArticle(overrides: Partial<HistoryArticlePageViewModel> = {}): HistoryArticlePageViewModel {
  return {
    title: 'Opening of Mirapolis',
    subtitle: '',
    summary: 'Article detaille sur l ouverture de Mirapolis.',
    dateLabel: '1987',
    eventTypeLabel: 'Opening',
    ownerName: 'Mirapolis',
    park: buildPark({ name: 'Mirapolis' }),
    parkItem: null,
    contextPark: null,
    mainImageId: null,
    mainImage: null,
    blocks: [],
    sources: [],
    timelineLink: ['/fr', 'park', 'park-1', 'mirapolis', 'history'],
    canonicalPath: null,
    event: {
      id: 'event-1',
      key: 'opening',
      entityType: 'Park',
      ownerId: 'park-1',
      parkId: 'park-1',
      parkItemId: null,
      contextParkId: 'park-1',
      year: 1987,
      month: 5,
      day: 20,
      datePrecision: 'Day',
      eventType: 'Opening',
      isMajor: true,
      isVisible: true,
      slug: 'opening',
      titles: [],
      summaries: [],
      mainImageId: null,
      relatedParkIds: [],
      relatedParkItemIds: [],
      sources: [],
      article: null,
      createdAtUtc: '2026-01-01T00:00:00Z',
      updatedAtUtc: '2026-01-02T00:00:00Z'
    },
    article: {
      slug: 'opening',
      titles: [],
      subtitles: [],
      summaries: [],
      mainImageId: null,
      blocks: [],
      sources: [],
      isPublished: true
    },
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
