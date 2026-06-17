import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ImageTagDto } from '@app/models/images/image-tag-dto';
import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { PagedResult } from '@shared/models/contracts';
import {
  PARK_ITEM_IMAGES_IMAGES_PORT,
  PARK_ITEM_IMAGES_ITEMS_PORT,
  PARK_ITEM_IMAGES_PARKS_PORT,
  ParkItemImagesImagesPort,
  ParkItemImagesItemsPort,
  ParkItemImagesParksPort
} from './park-item-images-data.ports';
import { ParkItemImagesStateFacade } from './park-item-images-state.facade';

class FakeItemsPort implements ParkItemImagesItemsPort {
  public response$: Observable<ParkItem> = of(createParkItem());
  public readonly calls: string[] = [];

  getParkItemById(id: string): Observable<ParkItem> {
    this.calls.push(id);
    return this.response$;
  }
}

class FakeParksPort implements ParkItemImagesParksPort {
  public response$: Observable<Park> = of(createPark());
  public readonly calls: string[] = [];

  getParkById(id: string): Observable<Park> {
    this.calls.push(id);
    return this.response$;
  }
}

class FakeImagesPort implements ParkItemImagesImagesPort {
  public firstPage$: Observable<PagedResult<ImageDto>> = of(createImagePage([createImage('image-1')], 1, 2, 2));
  public nextPage$: Observable<PagedResult<ImageDto>> = of(createImagePage([createImage('image-2')], 2, 2, 2));
  public tags$: Observable<ImageTagDto[]> = of([]);
  public readonly pageCalls: { ownerType: ImageOwnerType; ownerId: string; category: ImageCategory; page?: number; size?: number }[] = [];
  public tagCallCount: number = 0;

  getImagesPage(ownerType: ImageOwnerType, ownerId: string, category: ImageCategory, page?: number, size?: number): Observable<PagedResult<ImageDto>> {
    this.pageCalls.push({ ownerType, ownerId, category, page, size });
    return page === 2 ? this.nextPage$ : this.firstPage$;
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

function createParkItem(overrides: Partial<ParkItem> = {}): ParkItem {
  return {
    id: 'item-1',
    parkId: 'park-1',
    zoneId: null,
    name: 'Taron',
    category: 'Attraction',
    type: 'RollerCoaster',
    latitude: 50.8,
    longitude: 6.8,
    isVisible: true,
    ...overrides
  } as ParkItem;
}

function createImage(id: string): ImageDto {
  return {
    id,
    category: ImageCategory.PARK_ITEM,
    ownerType: ImageOwnerType.PARK_ITEM,
    ownerId: 'item-1',
    path: `items/${id}`,
    description: `Photo ${id}`,
    isCurrent: id === 'image-1',
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

function createImagePage(items: ImageDto[], currentPage: number, totalPages: number, totalItems: number): PagedResult<ImageDto> {
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
  facade: ParkItemImagesStateFacade;
  itemsPort: FakeItemsPort;
  parksPort: FakeParksPort;
  imagesPort: FakeImagesPort;
  ssrStatusService: FakeSsrHttpStatusService;
} {
  const itemsPort: FakeItemsPort = new FakeItemsPort();
  const parksPort: FakeParksPort = new FakeParksPort();
  const imagesPort: FakeImagesPort = new FakeImagesPort();
  const ssrStatusService: FakeSsrHttpStatusService = new FakeSsrHttpStatusService();

  TestBed.configureTestingModule({
    providers: [
      ParkItemImagesStateFacade,
      { provide: PARK_ITEM_IMAGES_ITEMS_PORT, useValue: itemsPort },
      { provide: PARK_ITEM_IMAGES_PARKS_PORT, useValue: parksPort },
      { provide: PARK_ITEM_IMAGES_IMAGES_PORT, useValue: imagesPort },
      { provide: SsrHttpStatusService, useValue: ssrStatusService }
    ]
  });

  return {
    facade: TestBed.inject(ParkItemImagesStateFacade),
    itemsPort,
    parksPort,
    imagesPort,
    ssrStatusService
  };
}

describe('ParkItemImagesStateFacade', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it('loads the item, parent park and first image page through public ports', () => {
    const context = configureFacade();

    context.facade.loadItemImages('item-1');

    expect(context.facade.state().kind).toBe('ready');
    expect(context.facade.item()?.name).toBe('Taron');
    expect(context.facade.park()?.name).toBe('Phantasialand');
    expect(context.facade.totalImages()).toBe(2);
    expect(context.facade.canLoadMore()).toBeTrue();
    expect(context.facade.photos()[0]?.categoryKey).toBe('gallery');
    expect(context.facade.categories()).toEqual([{ key: 'gallery', labelKey: 'parkItems.photos.categories.gallery', count: 1 }]);
    expect(context.itemsPort.calls).toEqual(['item-1']);
    expect(context.parksPort.calls).toEqual(['park-1']);
    expect(context.imagesPort.tagCallCount).toBe(1);
    expect(context.imagesPort.pageCalls).toEqual([
      { ownerType: ImageOwnerType.PARK_ITEM, ownerId: 'item-1', category: ImageCategory.PARK_ITEM, page: 1, size: 100 }
    ]);
  });

  it('appends the next image page', () => {
    const context = configureFacade();

    context.facade.loadItemImages('item-1');
    context.facade.loadNextPage();

    expect(context.facade.photos().map((photo) => photo.imageId)).toEqual(['image-1', 'image-2']);
    expect(context.facade.canLoadMore()).toBeFalse();
    expect(context.imagesPort.pageCalls).toEqual([
      { ownerType: ImageOwnerType.PARK_ITEM, ownerId: 'item-1', category: ImageCategory.PARK_ITEM, page: 1, size: 100 },
      { ownerType: ImageOwnerType.PARK_ITEM, ownerId: 'item-1', category: ImageCategory.PARK_ITEM, page: 2, size: 100 }
    ]);
  });

  it('sets SSR not found when the item lookup returns 404', () => {
    const context = configureFacade();
    context.itemsPort.response$ = throwError(() => ({ status: 404 }));

    context.facade.loadItemImages('missing-item');

    expect(context.facade.state().kind).toBe('error');
    expect(context.ssrStatusService.notFoundCallCount).toBe(1);
  });
});
