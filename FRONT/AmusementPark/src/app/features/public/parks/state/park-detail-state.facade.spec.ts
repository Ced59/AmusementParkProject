import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ImageTagDto } from '@app/models/images/image-tag-dto';
import { Park } from '@app/models/parks/park';
import { ParkDistanceResponse } from '@app/models/parks/park-distance';
import { ParkExplorer } from '@app/models/parks/park-explorer';
import { ParkFounder } from '@app/models/parks/park-founder';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkOperator } from '@app/models/parks/park-operator';
import { ParkZone } from '@app/models/parks/park-zone';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { CountryDisplayService } from '@shared/services/countries/country-display.service';
import {
  PARK_DETAIL_FOUNDERS_PORT,
  PARK_DETAIL_IMAGES_PORT,
  PARK_DETAIL_ITEMS_PORT,
  PARK_DETAIL_OPERATORS_PORT,
  PARK_DETAIL_PARKS_PORT,
  PARK_DETAIL_ZONES_PORT,
  ParkDetailFoundersPort,
  ParkDetailImagesPort,
  ParkDetailItemsPort,
  ParkDetailOperatorsPort,
  ParkDetailParksPort,
  ParkDetailZonesPort
} from './park-detail-data.ports';
import { ParkDetailStateFacade } from './park-detail-state.facade';

class FakeParksPort implements ParkDetailParksPort {
  public parkByIdResponse$: Observable<Park> = of(createPark());
  public explorerResponse$: Observable<ParkExplorer> = of(createExplorer(0));
  public nearestResponse$: Observable<ParkDistanceResponse> = of(createDistanceResponse([]));
  public readonly parkByIdCalls: string[] = [];
  public readonly explorerCalls: string[] = [];
  public readonly nearestCalls: { sourceParkId: string; limit?: number; maxDistanceKilometers?: number | null }[] = [];

  getParkById(id: string): Observable<Park> {
    this.parkByIdCalls.push(id);
    return this.parkByIdResponse$;
  }

  getParkExplorer(parkId: string): Observable<ParkExplorer> {
    this.explorerCalls.push(parkId);
    return this.explorerResponse$;
  }

  getNearestParks(sourceParkId: string, limit?: number, maxDistanceKilometers?: number | null): Observable<ParkDistanceResponse> {
    this.nearestCalls.push({ sourceParkId, limit, maxDistanceKilometers });
    return this.nearestResponse$;
  }
}

class FakeImagesPort implements ParkDetailImagesPort {
  public tagsResponse$: Observable<ImageTagDto[]> = of([]);
  public readonly imageCalls: { ownerType: ImageOwnerType; ownerId: string; category: ImageCategory; page?: number; size?: number }[] = [];

  getImages(ownerType: ImageOwnerType, ownerId: string, category: ImageCategory, page?: number, size?: number): Observable<ImageDto[]> {
    this.imageCalls.push({ ownerType, ownerId, category, page, size });
    return of([]);
  }

  getAdminImageTags(): Observable<ImageTagDto[]> {
    return this.tagsResponse$;
  }
}

class FakeItemsPort implements ParkDetailItemsPort {
  public itemsResponse$: Observable<ParkItem[]> = of([]);
  public readonly calls: string[] = [];

  getParkItemsByParkId(parkId: string): Observable<ParkItem[]> {
    this.calls.push(parkId);
    return this.itemsResponse$;
  }
}

class FakeZonesPort implements ParkDetailZonesPort {
  public zonesResponse$: Observable<ParkZone[]> = of([]);
  public readonly calls: string[] = [];

  getParkZonesByParkId(parkId: string): Observable<ParkZone[]> {
    this.calls.push(parkId);
    return this.zonesResponse$;
  }
}

class FakeFoundersPort implements ParkDetailFoundersPort {
  getParkFounderById(id: string): Observable<ParkFounder> {
    return of({ id, name: 'Founder' });
  }
}

class FakeOperatorsPort implements ParkDetailOperatorsPort {
  getParkOperatorById(id: string): Observable<ParkOperator> {
    return of({ id, name: 'Operator' });
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
    name: 'Bellewaerde',
    countryCode: 'BE',
    latitude: 50.845,
    longitude: 2.945,
    isVisible: true,
    descriptions: [
      { languageCode: 'en', value: '<p>Belgian park.</p>' }
    ]
  };
}

function createParkItem(id: string, category: ParkItem['category'], type: ParkItem['type']): ParkItem {
  return {
    id,
    parkId: 'park-1',
    name: id,
    category,
    type,
    latitude: 50.845,
    longitude: 2.945,
    isVisible: true
  };
}

function createExplorer(totalItems: number): ParkExplorer {
  return {
    parkId: 'park-1',
    hasZones: false,
    overview: {
      name: 'overview',
      isVirtual: true,
      totalItems,
      countsByCategory: [],
      countsByType: []
    },
    zones: [],
    unassigned: null
  };
}

function createDistanceResponse(targets: ParkDistanceResponse['targets']): ParkDistanceResponse {
  return {
    source: {
      id: 'park-1',
      name: 'Bellewaerde',
      countryCode: 'BE',
      latitude: 50.845,
      longitude: 2.945
    },
    distanceUnit: 'km',
    calculationKind: 'Haversine',
    targets,
    missingTargetParkIds: [],
    unavailableTargetParkIds: []
  };
}

function configureFacade(): {
  facade: ParkDetailStateFacade;
  parksPort: FakeParksPort;
  imagesPort: FakeImagesPort;
  itemsPort: FakeItemsPort;
  zonesPort: FakeZonesPort;
  ssrStatusService: FakeSsrHttpStatusService;
} {
  const parksPort: FakeParksPort = new FakeParksPort();
  const imagesPort: FakeImagesPort = new FakeImagesPort();
  const itemsPort: FakeItemsPort = new FakeItemsPort();
  const zonesPort: FakeZonesPort = new FakeZonesPort();
  const ssrStatusService: FakeSsrHttpStatusService = new FakeSsrHttpStatusService();

  TestBed.configureTestingModule({
    providers: [
      ParkDetailStateFacade,
      { provide: PARK_DETAIL_PARKS_PORT, useValue: parksPort },
      { provide: PARK_DETAIL_IMAGES_PORT, useValue: imagesPort },
      { provide: PARK_DETAIL_ITEMS_PORT, useValue: itemsPort },
      { provide: PARK_DETAIL_ZONES_PORT, useValue: zonesPort },
      { provide: PARK_DETAIL_FOUNDERS_PORT, useClass: FakeFoundersPort },
      { provide: PARK_DETAIL_OPERATORS_PORT, useClass: FakeOperatorsPort },
      {
        provide: CountryDisplayService,
        useValue: {
          resolveLocalizedCountryName: (): string => 'Belgique'
        }
      },
      { provide: SsrHttpStatusService, useValue: ssrStatusService }
    ]
  });

  return {
    facade: TestBed.inject(ParkDetailStateFacade),
    parksPort,
    imagesPort,
    itemsPort,
    zonesPort,
    ssrStatusService
  };
}

describe('ParkDetailStateFacade', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it('loads the main park and orchestrates secondary public data through ports', () => {
    const context = configureFacade();
    context.parksPort.explorerResponse$ = of(createExplorer(3));
    context.itemsPort.itemsResponse$ = of([
      createParkItem('coaster-1', 'Attraction', 'RollerCoaster')
    ]);

    context.facade.setCurrentLanguage('fr');
    context.facade.loadPark('park-1');

    expect(context.facade.state().kind).toBe('ready');
    expect(context.facade.park()?.name).toBe('Bellewaerde');
    expect(context.facade.park()?.countryName).toBe('Belgique');
    expect(context.facade.park()?.stats[0].value).toBe(3);
    expect(context.parksPort.parkByIdCalls).toEqual(['park-1']);
    expect(context.parksPort.explorerCalls).toEqual(['park-1']);
    expect(context.parksPort.nearestCalls).toEqual([{ sourceParkId: 'park-1', limit: 4, maxDistanceKilometers: null }]);
    expect(context.itemsPort.calls).toEqual(['park-1']);
    expect(context.zonesPort.calls).toEqual(['park-1']);
    expect(context.imagesPort.imageCalls.some((call) => call.ownerType === ImageOwnerType.PARK && call.ownerId === 'park-1')).toBeTrue();
  });

  it('falls back to item counts when the explorer endpoint fails', () => {
    spyOn(console, 'error');
    const context = configureFacade();
    context.parksPort.explorerResponse$ = throwError(() => new Error('explorer unavailable'));
    context.itemsPort.itemsResponse$ = of([
      createParkItem('coaster-1', 'Attraction', 'RollerCoaster'),
      createParkItem('restaurant-1', 'Restaurant', 'Restaurant')
    ]);

    context.facade.loadPark('park-1');

    expect(context.facade.state().kind).toBe('ready');
    expect(context.itemsPort.calls).toEqual(['park-1', 'park-1']);
    expect(context.facade.park()?.stats[0].value).toBe(2);
    expect(context.facade.summary()?.entries.find((entry) => entry.labelKey === 'parkVisitor.summary.totalItems')?.count).toBe(2);
  });

  it('sets the SSR 404 status and preserves the previous data when the main park is missing', () => {
    spyOn(console, 'error');
    const context = configureFacade();
    context.facade.loadPark('park-1');
    context.parksPort.parkByIdResponse$ = throwError(() => ({ status: 404 }));

    context.facade.loadPark('missing-park');

    expect(context.facade.state().kind).toBe('error');
    expect(context.facade.state().error).toBe('parks.detail.errorMessage');
    expect(context.facade.park()?.id).toBe('park-1');
    expect(context.ssrStatusService.notFoundCallCount).toBe(1);
  });
});
