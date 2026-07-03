import { TestBed } from '@angular/core/testing';
import { Observable, of } from 'rxjs';

import { ImageDto } from '@app/models/images/image-dto';
import { AttractionManufacturer } from '@app/models/parks/attraction-manufacturer';
import { Park } from '@app/models/parks/park';
import { ParkExplorer } from '@app/models/parks/park-explorer';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkMapItems } from '@app/models/parks/park-map-items';
import { ParkZone } from '@app/models/parks/park-zone';
import { ClosedEntityFilter } from '@app/models/shared/closed-entity-filter';
import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { SsrRuntimeService } from '@core/ssr/ssr-runtime.service';
import { PagedResult } from '@shared/models/contracts';
import { MeasurementPreferenceService } from '@app/services/measurements/measurement-preference.service';
import { MeasurementConversionService } from '@shared/services/measurements/measurement-conversion.service';
import { NaturalTextTruncatorService } from '@shared/services/text/natural-text-truncator.service';

import { ParkItemsByParkIdFilters } from '@data-access/park-items/park-items-api-endpoints';

import {
  PARK_ITEMS_PAGE_STATE_IMAGES_API_SERVICE_PORT,
  PARK_ITEMS_PAGE_STATE_MANUFACTURERS_API_SERVICE_PORT,
  PARK_ITEMS_PAGE_STATE_PARK_ITEMS_API_SERVICE_PORT,
  PARK_ITEMS_PAGE_STATE_PARK_ZONES_API_SERVICE_PORT,
  PARK_ITEMS_PAGE_STATE_PARKS_API_SERVICE_PORT,
  ParkItemsPageStateImagesApiServicePort,
  ParkItemsPageStateManufacturersApiServicePort,
  ParkItemsPageStateParkItemsApiServicePort,
  ParkItemsPageStateParkZonesApiServicePort,
  ParkItemsPageStateParksApiServicePort
} from './park-items-page-state-data.ports';
import { ParkItemsPageStateFacade } from './park-items-page-state.facade';

interface ClosedFilterHttpOptions extends AnonymousHttpOptions {
  closedFilter?: ClosedEntityFilter;
}

class FakeParksPort implements ParkItemsPageStateParksApiServicePort {
  public parkResponse$: Observable<Park> = of(createPark('Operating'));
  public explorerResponse$: Observable<ParkExplorer> = of(createExplorer());
  public mapItemsResponse$: Observable<ParkMapItems> = of(createMapItems(createPark('Operating')));
  public readonly parkCalls: string[] = [];
  public readonly explorerCalls: Array<{ parkId: string; closedFilter?: ClosedEntityFilter }> = [];
  public readonly mapItemsCalls: Array<{ parkId: string; closedFilter?: ClosedEntityFilter }> = [];

  getParkById(id: string): Observable<Park> {
    this.parkCalls.push(id);
    return this.parkResponse$;
  }

  getParkExplorer(parkId: string, options?: ClosedFilterHttpOptions): Observable<ParkExplorer> {
    this.explorerCalls.push({ parkId, closedFilter: options?.closedFilter });
    return this.explorerResponse$;
  }

  getParkMapItems(id: string, options?: ClosedFilterHttpOptions): Observable<ParkMapItems> {
    this.mapItemsCalls.push({ parkId: id, closedFilter: options?.closedFilter });
    return this.mapItemsResponse$;
  }
}

class FakeParkItemsPort implements ParkItemsPageStateParkItemsApiServicePort {
  public pageResponse$: Observable<PagedResult<ParkItem>> = of(createItemsPage());
  public readonly pageCalls: Array<{ parkId: string; page: number; size: number; filters: ParkItemsByParkIdFilters | null }> = [];

  getParkItemsByParkIdPage(
    parkId: string,
    page: number,
    size: number,
    filters: ParkItemsByParkIdFilters | null = null
  ): Observable<PagedResult<ParkItem>> {
    this.pageCalls.push({ parkId, page, size, filters });
    return this.pageResponse$;
  }
}

class FakeImagesPort implements ParkItemsPageStateImagesApiServicePort {
  getImages(): Observable<ImageDto[]> {
    return of([]);
  }

  buildImageUrl(): string {
    return '';
  }

  buildImageSrcSet(): string {
    return '';
  }
}

class FakeManufacturersPort implements ParkItemsPageStateManufacturersApiServicePort {
  getAttractionManufacturers(): Observable<AttractionManufacturer[]> {
    return of([]);
  }
}

class FakeZonesPort implements ParkItemsPageStateParkZonesApiServicePort {
  getParkZonesByParkId(): Observable<ParkZone[]> {
    return of([]);
  }
}

class FakeSsrRuntimeService {
  shouldUseMinimalPublicData(): boolean {
    return true;
  }
}

function createPark(status: Park['status']): Park {
  return {
    id: 'park-1',
    name: 'Six Flags New Orleans',
    status,
    countryCode: 'US',
    latitude: 30.0,
    longitude: -89.9,
    isVisible: true
  };
}

function createExplorer(): ParkExplorer {
  return {
    parkId: 'park-1',
    hasZones: false,
    overview: {
      id: null,
      name: 'overview',
      names: [],
      slug: null,
      isVirtual: true,
      totalItems: 0,
      countsByCategory: [],
      countsByType: []
    },
    zones: [],
    unassigned: null
  };
}

function createMapItems(park: Park): ParkMapItems {
  return {
    park,
    zones: [],
    items: [],
    unlocatedItems: []
  };
}

function createItemsPage(): PagedResult<ParkItem> {
  return {
    items: [],
    pagination: {
      currentPage: 1,
      totalPages: 0,
      totalItems: 0,
      itemsPerPage: 12
    }
  };
}

function configureFacade(): {
  facade: ParkItemsPageStateFacade;
  parksPort: FakeParksPort;
  parkItemsPort: FakeParkItemsPort;
} {
  const parksPort: FakeParksPort = new FakeParksPort();
  const parkItemsPort: FakeParkItemsPort = new FakeParkItemsPort();

  TestBed.configureTestingModule({
    providers: [
      ParkItemsPageStateFacade,
      { provide: PARK_ITEMS_PAGE_STATE_PARKS_API_SERVICE_PORT, useValue: parksPort },
      { provide: PARK_ITEMS_PAGE_STATE_PARK_ITEMS_API_SERVICE_PORT, useValue: parkItemsPort },
      { provide: PARK_ITEMS_PAGE_STATE_IMAGES_API_SERVICE_PORT, useClass: FakeImagesPort },
      { provide: PARK_ITEMS_PAGE_STATE_MANUFACTURERS_API_SERVICE_PORT, useClass: FakeManufacturersPort },
      { provide: PARK_ITEMS_PAGE_STATE_PARK_ZONES_API_SERVICE_PORT, useClass: FakeZonesPort },
      { provide: NaturalTextTruncatorService, useValue: {} },
      { provide: MeasurementPreferenceService, useValue: { preferredSystem: (): string => 'metric' } },
      { provide: MeasurementConversionService, useValue: {} },
      { provide: SsrRuntimeService, useClass: FakeSsrRuntimeService }
    ]
  });

  return {
    facade: TestBed.inject(ParkItemsPageStateFacade),
    parksPort,
    parkItemsPort
  };
}

describe('ParkItemsPageStateFacade', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it('reloads explorer, map items and list with all items for definitively closed parks', () => {
    const context = configureFacade();
    context.parksPort.parkResponse$ = of(createPark('ClosedDefinitively'));
    context.parksPort.mapItemsResponse$ = of(createMapItems(createPark('ClosedDefinitively')));

    context.facade.loadData('park-1', 'fr');

    expect(context.facade.selectedClosedFilter()).toBe('all');
    expect(context.parksPort.parkCalls).toEqual(['park-1']);
    expect(context.parksPort.explorerCalls).toEqual([
      { parkId: 'park-1', closedFilter: 'openOnly' },
      { parkId: 'park-1', closedFilter: 'all' }
    ]);
    expect(context.parksPort.mapItemsCalls).toEqual([
      { parkId: 'park-1', closedFilter: 'openOnly' },
      { parkId: 'park-1', closedFilter: 'all' }
    ]);
    expect(context.parkItemsPort.pageCalls.map((call) => call.filters?.closedFilter)).toEqual(['openOnly', 'all']);
  });
});
