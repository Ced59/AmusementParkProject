import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ImageTagDto } from '@app/models/images/image-tag-dto';
import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import {
  PARK_ITEM_DETAIL_IMAGES_PORT,
  PARK_ITEM_DETAIL_ITEMS_PORT,
  PARK_ITEM_DETAIL_MANUFACTURERS_PORT,
  PARK_ITEM_DETAIL_PARKS_PORT,
  PARK_ITEM_DETAIL_ZONES_PORT,
  ParkItemDetailImagesPort,
  ParkItemDetailItemsPort,
  ParkItemDetailManufacturersPort,
  ParkItemDetailParksPort,
  ParkItemDetailZonesPort
} from './park-item-detail-data.ports';
import { ParkItemDetailStateFacade } from './park-item-detail-state.facade';

class FakeItemsPort implements ParkItemDetailItemsPort {
  public itemResponse$: Observable<ParkItem> = of(createParkItem());
  public relatedResponse$: Observable<ParkItem[]> = of([]);
  public readonly itemCalls: string[] = [];
  public readonly relatedCalls: string[] = [];

  getParkItemById(id: string): Observable<ParkItem> {
    this.itemCalls.push(id);
    return this.itemResponse$;
  }

  getParkItemsByParkId(parkId: string): Observable<ParkItem[]> {
    this.relatedCalls.push(parkId);
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
  public tagsResponse$: Observable<ImageTagDto[]> = of([]);
  public photosResponse$: Observable<ImageDto[]> = of([]);
  public readonly imageCalls: { ownerType: ImageOwnerType; ownerId: string; category: ImageCategory; page?: number; size?: number }[] = [];

  getImages(ownerType: ImageOwnerType, ownerId: string, category: ImageCategory, page?: number, size?: number): Observable<ImageDto[]> {
    this.imageCalls.push({ ownerType, ownerId, category, page, size });
    return this.photosResponse$;
  }

  getAdminImageTags(): Observable<ImageTagDto[]> {
    return this.tagsResponse$;
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
      model: 'Launch Coaster'
    },
    ...overrides
  } as ParkItem;
}

function configureFacade(): {
  facade: ParkItemDetailStateFacade;
  itemsPort: FakeItemsPort;
  parksPort: FakeParksPort;
  manufacturersPort: FakeManufacturersPort;
  zonesPort: FakeZonesPort;
  imagesPort: FakeImagesPort;
  ssrStatusService: FakeSsrHttpStatusService;
} {
  const itemsPort: FakeItemsPort = new FakeItemsPort();
  const parksPort: FakeParksPort = new FakeParksPort();
  const manufacturersPort: FakeManufacturersPort = new FakeManufacturersPort();
  const zonesPort: FakeZonesPort = new FakeZonesPort();
  const imagesPort: FakeImagesPort = new FakeImagesPort();
  const ssrStatusService: FakeSsrHttpStatusService = new FakeSsrHttpStatusService();

  TestBed.configureTestingModule({
    providers: [
      ParkItemDetailStateFacade,
      { provide: PARK_ITEM_DETAIL_ITEMS_PORT, useValue: itemsPort },
      { provide: PARK_ITEM_DETAIL_PARKS_PORT, useValue: parksPort },
      { provide: PARK_ITEM_DETAIL_MANUFACTURERS_PORT, useValue: manufacturersPort },
      { provide: PARK_ITEM_DETAIL_ZONES_PORT, useValue: zonesPort },
      { provide: PARK_ITEM_DETAIL_IMAGES_PORT, useValue: imagesPort },
      { provide: SsrHttpStatusService, useValue: ssrStatusService }
    ]
  });

  return {
    facade: TestBed.inject(ParkItemDetailStateFacade),
    itemsPort,
    parksPort,
    manufacturersPort,
    zonesPort,
    imagesPort,
    ssrStatusService
  };
}

describe('ParkItemDetailStateFacade', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it('loads the park item and orchestrates related detail data through ports', () => {
    const context = configureFacade();
    context.itemsPort.relatedResponse$ = of([createParkItem({ id: 'item-2', name: 'Raik' })]);

    context.facade.setCurrentLanguage('fr');
    context.facade.loadItem('item-1');

    expect(context.facade.state().kind).toBe('ready');
    expect(context.facade.detail()?.name).toBe('Taron');
    expect(context.facade.detail()?.parkName).toBe('Phantasialand');
    expect(context.facade.detail()?.manufacturerName).toBe('Intamin');
    expect(context.facade.detail()?.zoneName).toBe('Mexico');
    expect(context.itemsPort.itemCalls).toEqual(['item-1']);
    expect(context.parksPort.calls).toEqual(['park-1']);
    expect(context.itemsPort.relatedCalls).toEqual(['park-1']);
    expect(context.manufacturersPort.calls).toEqual(['manufacturer-1']);
    expect(context.zonesPort.calls).toEqual(['zone-1']);
    expect(context.imagesPort.imageCalls).toEqual([
      { ownerType: ImageOwnerType.ATTRACTION, ownerId: 'item-1', category: ImageCategory.ATTRACTION, page: undefined, size: undefined }
    ]);
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
    context.imagesPort.tagsResponse$ = throwError(() => new Error('Tags API unavailable'));
    context.itemsPort.relatedResponse$ = throwError(() => new Error('Related items unavailable'));
    context.manufacturersPort.response$ = throwError(() => new Error('Manufacturer unavailable'));
    context.zonesPort.response$ = throwError(() => new Error('Zone unavailable'));

    context.facade.loadItem('item-1');

    expect(context.facade.state().kind).toBe('ready');
    expect(context.facade.detail()?.name).toBe('Taron');
    expect(context.facade.detail()?.manufacturerName).toBeNull();
    expect(context.facade.detail()?.zoneName).toBeNull();
    expect(context.facade.detail()?.photos).toEqual([]);
    expect(context.facade.detail()?.relatedItems).toEqual([]);
  });
});
