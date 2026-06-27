import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ImageTagDto } from '@app/models/images/image-tag-dto';
import { ParkItemImageDto } from '@app/models/images/park-item-image-dto';
import { Park } from '@app/models/parks/park';
import { ParkDetailSummary } from '@app/models/parks/park-detail-summary';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { PagedResult } from '@shared/models/contracts';
import {
  PARK_IMAGES_IMAGES_PORT,
  PARK_IMAGES_PARKS_PORT,
  ParkImagesImagesPort,
  ParkImagesParksPort
} from './park-images-data.ports';
import { ParkImagesStateFacade } from './park-images-state.facade';

class FakeParksPort implements ParkImagesParksPort {
  public response$: Observable<ParkDetailSummary> = of(createSummary());
  public readonly calls: string[] = [];

  getParkDetailSummary(id: string): Observable<ParkDetailSummary> {
    this.calls.push(id);
    return this.response$;
  }
}

class FakeImagesPort implements ParkImagesImagesPort {
  public firstPage$: Observable<PagedResult<ImageDto>> = of(createImagePage([createImage('image-1')], 1, 2, 2));
  public nextPage$: Observable<PagedResult<ImageDto>> = of(createImagePage([createImage('image-2')], 2, 2, 2));
  public logoPage$: Observable<PagedResult<ImageDto>> = of(createImagePage([], 1, 0, 0));
  public itemProbePage$: Observable<PagedResult<ParkItemImageDto>> = of(createImagePage([], 1, 0, 0));
  public firstItemPage$: Observable<PagedResult<ParkItemImageDto>> = of(createImagePage([createParkItemImage('item-image-1')], 1, 2, 2));
  public nextItemPage$: Observable<PagedResult<ParkItemImageDto>> = of(createImagePage([createParkItemImage('item-image-2')], 2, 2, 2));
  public tags$: Observable<ImageTagDto[]> = of([createImageTag()]);
  public readonly pageCalls: { ownerType: ImageOwnerType; ownerId: string; category: ImageCategory; page?: number; size?: number }[] = [];
  public readonly itemImageCalls: { parkId: string; page?: number; size?: number }[] = [];
  public tagCallCount: number = 0;

  getImagesPage(ownerType: ImageOwnerType, ownerId: string, category: ImageCategory, page?: number, size?: number): Observable<PagedResult<ImageDto>> {
    this.pageCalls.push({ ownerType, ownerId, category, page, size });
    if (category === ImageCategory.LOGO) {
      return this.logoPage$;
    }

    return page === 2 ? this.nextPage$ : this.firstPage$;
  }

  getParkItemImagesByPark(parkId: string, page?: number, size?: number): Observable<PagedResult<ParkItemImageDto>> {
    this.itemImageCalls.push({ parkId, page, size });
    if (size === 1) {
      return this.itemProbePage$;
    }

    return page === 2 ? this.nextItemPage$ : this.firstItemPage$;
  }

  getImageTags(): Observable<ImageTagDto[]> {
    this.tagCallCount += 1;
    return this.tags$;
  }
}

class FakeSsrHttpStatusService {
  public notFoundCallCount: number = 0;

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

function createSummary(): ParkDetailSummary {
  return {
    park: createPark(),
    mainImage: null,
    references: {
      founderName: null,
      operatorName: null
    },
    stats: {
      totalItems: 0,
      zoneCount: 0,
      attractionCount: 0,
      restaurantCount: 0,
      showCount: 0,
      shopCount: 0,
      hotelCount: 0,
      countsByCategory: {}
    }
  };
}

function createImage(id: string): ImageDto {
  return {
    id,
    category: ImageCategory.PARK,
    ownerType: ImageOwnerType.PARK,
    ownerId: 'park-1',
    path: `parks/${id}`,
    description: `Photo ${id}`,
    isCurrent: id === 'image-1',
    isWatermarked: false,
    isPublished: true,
    width: 1200,
    height: 800,
    sizeInBytes: 1000,
    originalFileName: `${id}.jpg`,
    contentType: 'image/jpeg',
    geoLocation: null,
    exifMetadata: {
      takenOnUtc: '2024-04-02T00:00:00Z'
    },
    altTexts: [],
    captions: [],
    credits: [],
    tagIds: ['tag-map'],
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z'
  };
}

function createLogoImage(id: string): ImageDto {
  return {
    ...createImage(id),
    category: ImageCategory.LOGO,
    ownerType: ImageOwnerType.PARK,
    ownerId: 'park-1',
    tagIds: [],
    isCurrent: true
  };
}

function createParkItemImage(id: string): ParkItemImageDto {
  return {
    item: {
      id: 'item-1',
      parkId: 'park-1',
      name: 'Family Ride',
      category: 'Attraction',
      type: 'FlatRide',
      latitude: null,
      longitude: null
    },
    image: {
      ...createImage(id),
      category: ImageCategory.PARK_ITEM,
      ownerType: ImageOwnerType.PARK_ITEM,
      ownerId: 'item-1',
      tagIds: [],
      isCurrent: id === 'item-image-1'
    }
  };
}

function createImageTag(): ImageTagDto {
  return {
    id: 'tag-map',
    slug: 'park-map',
    labels: [{ languageCode: 'en', value: 'Official park map' }],
    descriptions: [],
    isActive: true,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z'
  };
}

function createImagePage<TItem>(items: TItem[], currentPage: number, totalPages: number, totalItems: number): PagedResult<TItem> {
  return {
    items,
    pagination: {
      currentPage,
      totalPages,
      totalItems,
      itemsPerPage: 100
    }
  };
}

function configureFacade(): {
  facade: ParkImagesStateFacade;
  parksPort: FakeParksPort;
  imagesPort: FakeImagesPort;
  ssrStatusService: FakeSsrHttpStatusService;
} {
  const parksPort: FakeParksPort = new FakeParksPort();
  const imagesPort: FakeImagesPort = new FakeImagesPort();
  const ssrStatusService: FakeSsrHttpStatusService = new FakeSsrHttpStatusService();

  TestBed.configureTestingModule({
    providers: [
      ParkImagesStateFacade,
      { provide: PARK_IMAGES_PARKS_PORT, useValue: parksPort },
      { provide: PARK_IMAGES_IMAGES_PORT, useValue: imagesPort },
      { provide: SsrHttpStatusService, useValue: ssrStatusService }
    ]
  });

  return {
    facade: TestBed.inject(ParkImagesStateFacade),
    parksPort,
    imagesPort,
    ssrStatusService
  };
}

describe('ParkImagesStateFacade', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it('loads the park summary, public tags and first image page through public ports', () => {
    const context = configureFacade();

    context.facade.loadParkImages('park-1');

    expect(context.facade.state().kind).toBe('ready');
    expect(context.facade.park()?.name).toBe('Phantasialand');
    expect(context.facade.totalImages()).toBe(2);
    expect(context.facade.canLoadMore()).toBeTrue();
    expect(context.facade.photos()[0]?.categoryKey).toBe('park-map');
    expect(context.facade.photos()[0]?.year).toBe('2024');
    expect(context.facade.categories()).toEqual([{ key: 'park-map', labelKey: 'parks.photos.categories.map', count: 1 }]);
    expect(context.parksPort.calls).toEqual(['park-1']);
    expect(context.imagesPort.tagCallCount).toBe(1);
    expect(context.imagesPort.pageCalls).toEqual([
      { ownerType: ImageOwnerType.PARK, ownerId: 'park-1', category: ImageCategory.PARK, page: 1, size: 100 },
      { ownerType: ImageOwnerType.PARK, ownerId: 'park-1', category: ImageCategory.LOGO, page: 1, size: 100 }
    ]);
    expect(context.imagesPort.itemImageCalls).toEqual([{ parkId: 'park-1', page: 1, size: 1 }]);
  });

  it('appends the next image page', () => {
    const context = configureFacade();

    context.facade.loadParkImages('park-1');
    context.facade.loadNextPage();

    expect(context.facade.photos().map((photo) => photo.imageId)).toEqual(['image-1', 'image-2']);
    expect(context.facade.canLoadMore()).toBeFalse();
    expect(context.imagesPort.pageCalls).toEqual([
      { ownerType: ImageOwnerType.PARK, ownerId: 'park-1', category: ImageCategory.PARK, page: 1, size: 100 },
      { ownerType: ImageOwnerType.PARK, ownerId: 'park-1', category: ImageCategory.LOGO, page: 1, size: 100 },
      { ownerType: ImageOwnerType.PARK, ownerId: 'park-1', category: ImageCategory.PARK, page: 2, size: 100 }
    ]);
  });

  it('shows park logos before park gallery photos', () => {
    const context = configureFacade();
    context.imagesPort.logoPage$ = of(createImagePage([createLogoImage('logo-1')], 1, 1, 1));

    context.facade.loadParkImages('park-1');

    expect(context.facade.parkTabImageCount()).toBe(3);
    expect(context.facade.photos().map((photo) => photo.imageId)).toEqual(['logo-1', 'image-1']);
    expect(context.facade.categories()[0]).toEqual({ key: 'park-logo', labelKey: 'parks.photos.categories.logo', count: 1 });
  });

  it('loads park item photos only when their tab is selected', () => {
    const context = configureFacade();
    context.imagesPort.itemProbePage$ = of(createImagePage([createParkItemImage('item-probe-1')], 1, 2, 2));

    context.facade.loadParkImages('park-1');

    expect(context.facade.activeTab()).toBe('park');
    expect(context.facade.showItemTab()).toBeTrue();
    expect(context.facade.itemTabImageCount()).toBe(2);
    expect(context.imagesPort.itemImageCalls).toEqual([{ parkId: 'park-1', page: 1, size: 1 }]);

    context.facade.selectTab('items');

    expect(context.facade.activeTab()).toBe('items');
    expect(context.facade.photos().map((photo) => photo.imageId)).toEqual(['item-image-1']);
    expect(context.facade.canLoadMore()).toBeTrue();
    expect(context.imagesPort.itemImageCalls).toEqual([
      { parkId: 'park-1', page: 1, size: 1 },
      { parkId: 'park-1', page: 1, size: 100 }
    ]);

    context.facade.loadNextPage();

    expect(context.facade.photos().map((photo) => photo.imageId)).toEqual(['item-image-1', 'item-image-2']);
    expect(context.facade.canLoadMore()).toBeFalse();
    expect(context.imagesPort.itemImageCalls).toEqual([
      { parkId: 'park-1', page: 1, size: 1 },
      { parkId: 'park-1', page: 1, size: 100 },
      { parkId: 'park-1', page: 2, size: 100 }
    ]);
  });

  it('selects the park item tab when the park has no direct images', () => {
    const context = configureFacade();
    context.imagesPort.firstPage$ = of(createImagePage([], 1, 0, 0));
    context.imagesPort.itemProbePage$ = of(createImagePage([createParkItemImage('item-probe-1')], 1, 2, 2));

    context.facade.loadParkImages('park-1');

    expect(context.facade.activeTab()).toBe('items');
    expect(context.facade.photos().map((photo) => photo.imageId)).toEqual(['item-image-1']);
    expect(context.imagesPort.itemImageCalls).toEqual([
      { parkId: 'park-1', page: 1, size: 1 },
      { parkId: 'park-1', page: 1, size: 100 }
    ]);
  });

  it('sets SSR not found when the summary lookup returns 404', () => {
    const context = configureFacade();
    context.parksPort.response$ = throwError(() => ({ status: 404 }));

    context.facade.loadParkImages('missing-park');

    expect(context.facade.state().kind).toBe('error');
    expect(context.ssrStatusService.notFoundCallCount).toBe(1);
  });
});
