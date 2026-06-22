import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkItemSiblingNavigation } from '@app/models/parks/park-item-sibling-navigation';
import { TechnicalPage } from '@app/models/technical-pages/technical-page';
import { VideoDto } from '@app/models/videos/video-dto';
import { VideoHostingProvider } from '@app/models/videos/video-hosting-provider';
import { VideoOwnerType } from '@app/models/videos/video-owner-type';
import { VideoSearchQuery } from '@app/models/videos/video-search-query';
import { VideoType } from '@app/models/videos/video-type';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { SsrRuntimeService } from '@core/ssr/ssr-runtime.service';
import { PagedResult } from '@shared/models/contracts';
import {
  PARK_ITEM_DETAIL_IMAGES_PORT,
  PARK_ITEM_DETAIL_ITEMS_PORT,
  PARK_ITEM_DETAIL_MANUFACTURERS_PORT,
  PARK_ITEM_DETAIL_PARKS_PORT,
  PARK_ITEM_DETAIL_TECHNICAL_PAGES_PORT,
  PARK_ITEM_DETAIL_VIDEOS_PORT,
  PARK_ITEM_DETAIL_ZONES_PORT,
  ParkItemDetailImagesPort,
  ParkItemDetailItemsPort,
  ParkItemDetailManufacturersPort,
  ParkItemDetailParksPort,
  ParkItemDetailTechnicalPagesPort,
  ParkItemDetailVideosPort,
  ParkItemDetailZonesPort
} from './park-item-detail-data.ports';
import { ParkItemDetailStateFacade } from './park-item-detail-state.facade';

class FakeItemsPort implements ParkItemDetailItemsPort {
  public itemResponse$: Observable<ParkItem> = of(createParkItem());
  public relatedResponse$: Observable<ParkItem[]> = of([]);
  public siblingResponse$: Observable<ParkItemSiblingNavigation> = of(createSiblingNavigation());
  public readonly itemCalls: string[] = [];
  public readonly relatedCalls: string[] = [];
  public readonly siblingCalls: string[] = [];

  getParkItemById(id: string): Observable<ParkItem> {
    this.itemCalls.push(id);
    return this.itemResponse$;
  }

  getParkItemSiblingNavigation(itemId: string): Observable<ParkItemSiblingNavigation> {
    this.siblingCalls.push(itemId);
    return this.siblingResponse$;
  }

  getRelatedParkItems(itemId: string, limit: number = 3): Observable<ParkItem[]> {
    this.relatedCalls.push(`${itemId}:${limit}`);
    return this.relatedResponse$;
  }
}

class FakeParksPort implements ParkItemDetailParksPort {
  public parkResponse$: Observable<Park> = of(createPark());
  public readonly calls: string[] = [];

  getParkById(id: string): Observable<Park> {
    this.calls.push(id);
    return this.parkResponse$;
  }
}

class FakeManufacturersPort implements ParkItemDetailManufacturersPort {
  public response$: Observable<{ name?: string | null }> = of({ name: 'Intamin' });
  public readonly calls: string[] = [];

  getAttractionManufacturerById(id: string): Observable<{ name?: string | null }> {
    this.calls.push(id);
    return this.response$;
  }
}

class FakeZonesPort implements ParkItemDetailZonesPort {
  public response$: Observable<{ name?: string | null }> = of({ name: 'Mexico' });
  public readonly calls: string[] = [];

  getParkZoneById(id: string): Observable<{ name?: string | null }> {
    this.calls.push(id);
    return this.response$;
  }
}

class FakeImagesPort implements ParkItemDetailImagesPort {
  public photosResponse$: Observable<ImageDto[]> = of([]);
  public readonly imageCalls: { ownerType: ImageOwnerType; ownerId: string; category: ImageCategory; page?: number; size?: number }[] = [];

  getImages(ownerType: ImageOwnerType, ownerId: string, category: ImageCategory, page?: number, size?: number): Observable<ImageDto[]> {
    this.imageCalls.push({ ownerType, ownerId, category, page, size });
    return this.photosResponse$;
  }
}

class FakeVideosPort implements ParkItemDetailVideosPort {
  public videosResponse$: Observable<PagedResult<VideoDto>> = of(createVideosPage(1));
  public readonly calls: VideoSearchQuery[] = [];

  getVideosPage(query: VideoSearchQuery = {}): Observable<PagedResult<VideoDto>> {
    this.calls.push(query);
    return this.videosResponse$;
  }
}

class FakeTechnicalPagesPort implements ParkItemDetailTechnicalPagesPort {
  public response$: Observable<TechnicalPage[]> = of([createTechnicalPage()]);
  public callCount = 0;

  getPublicLinkIndex(): Observable<TechnicalPage[]> {
    this.callCount += 1;
    return this.response$;
  }
}

class FakeSsrRuntimeService {
  public useMinimalPublicData = false;

  shouldUseMinimalPublicData(): boolean {
    return this.useMinimalPublicData;
  }
}

class FakeSsrHttpStatusService {
  public notFoundCallCount = 0;

  setNotFound(): void {
    this.notFoundCallCount += 1;
  }
}

function createPark(): Park {
  return {
    id: 'park-1',
    name: 'Phantasialand',
    countryCode: 'DE',
    latitude: 50.8,
    longitude: 6.8,
    isVisible: true,
    descriptions: []
  };
}

function createParkItem(overrides: Partial<ParkItem> = {}): ParkItem {
  return {
    id: 'item-1',
    parkId: 'park-1',
    zoneId: 'zone-1',
    name: 'Taron',
    category: 'Attraction',
    type: 'RollerCoaster',
    latitude: 50.8,
    longitude: 6.8,
    isVisible: true,
    attractionDetails: {
      manufacturerId: 'manufacturer-1',
      manufacturerName: null,
      model: 'Launch Coaster',
      restraintType: 'Lap bar'
    },
    ...overrides
  } as ParkItem;
}

function createTechnicalPage(): TechnicalPage {
  return {
    id: 'technical-lap-bar',
    categoryKey: 'restraint',
    categoryNames: [
      { languageCode: 'fr', value: 'Retenues' },
      { languageCode: 'en', value: 'Restraints' }
    ],
    slug: 'lap-bar',
    titles: [
      { languageCode: 'fr', value: 'Lap bar' },
      { languageCode: 'en', value: 'Lap bar' }
    ],
    summaries: [
      { languageCode: 'fr', value: 'Explication technique de la lap bar.' },
      { languageCode: 'en', value: 'Technical explanation of the lap bar.' }
    ],
    aliases: [
      {
        categoryKey: 'restraint',
        labels: [
          { languageCode: 'fr', value: 'Lap bar' },
          { languageCode: 'en', value: 'Lap bar' }
        ]
      }
    ],
    contentBlocks: [],
    sortOrder: 0,
    isVisible: true,
    adminReviewStatus: 'Validated',
    updatedAtUtc: '2026-01-01T00:00:00Z'
  };
}

function createImage(id: string): ImageDto {
  return {
    id,
    category: ImageCategory.PARK_ITEM,
    ownerType: ImageOwnerType.PARK_ITEM,
    ownerId: 'item-1',
    path: `items/${id}.jpg`,
    description: 'Taron main image',
    isCurrent: true,
    isPublished: true,
    width: 1200,
    height: 800,
    sizeInBytes: 1000,
    originalFileName: `${id}.jpg`,
    contentType: 'image/jpeg',
    geoLocation: null,
    altTexts: [],
    captions: [],
    credits: [],
    tagIds: [],
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z'
  };
}

function createVideo(): VideoDto {
  return {
    id: 'video-1',
    hostingProvider: VideoHostingProvider.YOUTUBE,
    ownerType: VideoOwnerType.PARK_ITEM,
    ownerId: 'item-1',
    type: VideoType.ON_RIDE,
    originalUrl: 'https://www.youtube.com/watch?v=item',
    canonicalUrl: 'https://www.youtube.com/watch?v=item',
    embedUrl: null,
    externalId: 'item',
    title: 'Item video',
    description: null,
    creatorName: null,
    creatorUrl: null,
    thumbnailUrl: null,
    thumbnailImageId: null,
    durationSeconds: null,
    publishedAtUtc: null,
    languageCodes: ['fr'],
    titles: [],
    descriptions: [],
    tagIds: [],
    externalMetadata: {},
    isPublished: true,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z'
  };
}

function createVideosPage(totalItems: number): PagedResult<VideoDto> {
  return {
    items: totalItems > 0 ? [createVideo()] : [],
    pagination: {
      totalItems,
      totalPages: totalItems > 0 ? 1 : 0,
      currentPage: 1,
      itemsPerPage: 1
    }
  };
}

function createSiblingNavigation(): ParkItemSiblingNavigation {
  return {
    parkId: 'park-1',
    currentItemId: 'item-1',
    currentPosition: 1,
    totalItems: 2,
    remainingItems: 1,
    previous: null,
    next: { id: 'item-2', name: 'Raik' }
  };
}

function configureFacade(): {
  facade: ParkItemDetailStateFacade;
  itemsPort: FakeItemsPort;
  parksPort: FakeParksPort;
  manufacturersPort: FakeManufacturersPort;
  zonesPort: FakeZonesPort;
  imagesPort: FakeImagesPort;
  videosPort: FakeVideosPort;
  technicalPagesPort: FakeTechnicalPagesPort;
  ssrStatusService: FakeSsrHttpStatusService;
  ssrRuntimeService: FakeSsrRuntimeService;
} {
  const itemsPort: FakeItemsPort = new FakeItemsPort();
  const parksPort: FakeParksPort = new FakeParksPort();
  const manufacturersPort: FakeManufacturersPort = new FakeManufacturersPort();
  const zonesPort: FakeZonesPort = new FakeZonesPort();
  const imagesPort: FakeImagesPort = new FakeImagesPort();
  const videosPort: FakeVideosPort = new FakeVideosPort();
  const technicalPagesPort: FakeTechnicalPagesPort = new FakeTechnicalPagesPort();
  const ssrStatusService: FakeSsrHttpStatusService = new FakeSsrHttpStatusService();
  const ssrRuntimeService: FakeSsrRuntimeService = new FakeSsrRuntimeService();

  TestBed.configureTestingModule({
    providers: [
      ParkItemDetailStateFacade,
      { provide: PARK_ITEM_DETAIL_ITEMS_PORT, useValue: itemsPort },
      { provide: PARK_ITEM_DETAIL_PARKS_PORT, useValue: parksPort },
      { provide: PARK_ITEM_DETAIL_MANUFACTURERS_PORT, useValue: manufacturersPort },
      { provide: PARK_ITEM_DETAIL_ZONES_PORT, useValue: zonesPort },
      { provide: PARK_ITEM_DETAIL_IMAGES_PORT, useValue: imagesPort },
      { provide: PARK_ITEM_DETAIL_VIDEOS_PORT, useValue: videosPort },
      { provide: PARK_ITEM_DETAIL_TECHNICAL_PAGES_PORT, useValue: technicalPagesPort },
      { provide: SsrHttpStatusService, useValue: ssrStatusService },
      { provide: SsrRuntimeService, useValue: ssrRuntimeService }
    ]
  });

  return {
    facade: TestBed.inject(ParkItemDetailStateFacade),
    itemsPort,
    parksPort,
    manufacturersPort,
    zonesPort,
    imagesPort,
    videosPort,
    technicalPagesPort,
    ssrStatusService,
    ssrRuntimeService
  };
}

describe('ParkItemDetailStateFacade', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it('loads the park item and orchestrates related detail data through ports', () => {
    const context = configureFacade();
    context.itemsPort.relatedResponse$ = of([createParkItem({
      id: 'item-2',
      name: 'Raik',
      descriptions: [{ languageCode: 'fr', value: 'Description similaire tres detaillee '.repeat(12) }]
    })]);
    context.imagesPort.photosResponse$ = of([createImage('image-1')]);

    context.facade.setCurrentLanguage('fr');
    context.facade.loadItem('item-1');

    expect(context.facade.state().kind).toBe('ready');
    expect(context.facade.detail()?.name).toBe('Taron');
    expect(context.facade.detail()?.parkName).toBe('Phantasialand');
    expect(context.facade.detail()?.manufacturerName).toBe('Intamin');
    expect(context.facade.detail()?.zoneName).toBe('Mexico');
    expect(context.facade.detail()?.heroPhoto?.imageId).toBe('image-1');
    expect(context.facade.detail()?.imagesLink).toEqual(['/', 'fr', 'park', 'park-1', 'phantasialand', 'item', 'item-1', 'taron', 'images']);
    expect(context.facade.detail()?.videosLink).toEqual(['/', 'fr', 'park', 'park-1', 'phantasialand', 'item', 'item-1', 'taron', 'videos']);
    expect(context.facade.detail()?.siblingNavigation?.next?.routerLink).toEqual(['/', 'fr', 'park', 'park-1', 'phantasialand', 'item', 'item-2', 'raik']);
    expect(context.facade.detail()?.relatedItems[0]?.description?.length).toBeLessThanOrEqual(160);
    expect(context.facade.detail()?.relatedItems[0]?.description?.endsWith('...')).toBeTrue();
    expect(context.facade.detail()?.specGroups[0]?.rows.find((row) => row.labelKey === 'parkItems.fields.restraintType')?.routerLink).toEqual([
      '/',
      'fr',
      'technical',
      'lap-bar'
    ]);
    expect(context.itemsPort.itemCalls).toEqual(['item-1']);
    expect(context.parksPort.calls).toEqual(['park-1']);
    expect(context.itemsPort.siblingCalls).toEqual(['item-1']);
    expect(context.itemsPort.relatedCalls).toEqual(['item-1:3']);
    expect(context.manufacturersPort.calls).toEqual(['manufacturer-1']);
    expect(context.zonesPort.calls).toEqual(['zone-1']);
    expect(context.imagesPort.imageCalls).toEqual([
      { ownerType: ImageOwnerType.PARK_ITEM, ownerId: 'item-1', category: ImageCategory.PARK_ITEM, page: 1, size: 1 }
    ]);
    expect(context.videosPort.calls).toEqual([{
      page: 1,
      size: 1,
      ownerType: VideoOwnerType.PARK_ITEM,
      ownerId: 'item-1'
    }]);
    expect(context.technicalPagesPort.callCount).toBe(1);
  });

  it('skips technical link index and deep related data during minimal SSR rendering', () => {
    const context = configureFacade();
    context.ssrRuntimeService.useMinimalPublicData = true;

    context.facade.loadItem('item-1');

    expect(context.technicalPagesPort.callCount).toBe(0);
    expect(context.itemsPort.siblingCalls).toEqual([]);
    expect(context.itemsPort.relatedCalls).toEqual([]);
    expect(context.manufacturersPort.calls).toEqual([]);
    expect(context.zonesPort.calls).toEqual([]);
    expect(context.facade.detail()?.specGroups[0]?.rows.find((row) => row.labelKey === 'parkItems.fields.restraintType')?.routerLink).toBeNull();
  });

  it('hides the videos link when the park item has no published videos', () => {
    const context = configureFacade();
    context.videosPort.videosResponse$ = of(createVideosPage(0));

    context.facade.setCurrentLanguage('fr');
    context.facade.loadItem('item-1');

    expect(context.facade.detail()?.videosLink).toBeNull();
  });

  it('sets SSR not found when the main item lookup returns 404', () => {
    const context = configureFacade();
    context.itemsPort.itemResponse$ = throwError(() => ({ status: 404 }));

    context.facade.loadItem('missing-item');

    expect(context.facade.state().kind).toBe('error');
    expect(context.ssrStatusService.notFoundCallCount).toBe(1);
  });

  it('keeps the primary detail ready when optional related data fails', () => {
    const context = configureFacade();
    context.imagesPort.photosResponse$ = throwError(() => new Error('Image API unavailable'));
    context.itemsPort.relatedResponse$ = throwError(() => new Error('Related items unavailable'));
    context.manufacturersPort.response$ = throwError(() => new Error('Manufacturer unavailable'));
    context.zonesPort.response$ = throwError(() => new Error('Zone unavailable'));
    context.technicalPagesPort.response$ = throwError(() => new Error('Technical pages unavailable'));

    context.facade.loadItem('item-1');

    expect(context.facade.state().kind).toBe('ready');
    expect(context.facade.detail()?.name).toBe('Taron');
    expect(context.facade.detail()?.manufacturerName).toBeNull();
    expect(context.facade.detail()?.zoneName).toBeNull();
    expect(context.facade.detail()?.heroPhoto).toBeNull();
    expect(context.facade.detail()?.imagesLink).toBeNull();
    expect(context.facade.detail()?.relatedItems).toEqual([]);
    expect(context.facade.detail()?.specGroups[0]?.rows.find((row) => row.labelKey === 'parkItems.fields.restraintType')?.routerLink).toBeNull();
  });
});
