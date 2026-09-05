import { TestBed } from '@angular/core/testing';
import { Observable, of } from 'rxjs';

import { Park } from '@app/models/parks/park';
import { ParkDetailSummary } from '@app/models/parks/park-detail-summary';
import { ParkMapItems } from '@app/models/parks/park-map-items';
import { ClosedEntityFilter } from '@app/models/shared/closed-entity-filter';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { SKIP_AUTHORIZATION_HEADER } from '@core/http/auth/auth-request-policy';

import { PARK_MAP_PARKS_PORT, ParkMapHttpOptions, ParkMapParksPort } from './park-map-data.ports';
import { ParkMapStateFacade } from './park-map-state.facade';

class FakeParksPort implements ParkMapParksPort {
  public summaryResponse$: Observable<ParkDetailSummary> = of(createSummary('Operating', 1));
  public mapItemsResponse$: Observable<ParkMapItems> = of(createMapItems(createPark('Operating')));
  public summaryResponses$: Observable<ParkDetailSummary>[] = [];
  public mapItemsResponses$: Observable<ParkMapItems>[] = [];
  public readonly summaryCalls: Array<{ id: string; closedFilter?: ClosedEntityFilter }> = [];
  public readonly mapItemsCalls: Array<{ id: string; closedFilter?: ClosedEntityFilter }> = [];
  public readonly officialMapFileCalls: Array<{ url: string; skipsAuthorization: boolean }> = [];

  getParkDetailSummary(id: string, options?: ParkMapHttpOptions): Observable<ParkDetailSummary> {
    this.summaryCalls.push({ id, closedFilter: options?.closedFilter });
    return this.summaryResponses$.shift() ?? this.summaryResponse$;
  }

  getParkMapItems(id: string, options?: ParkMapHttpOptions): Observable<ParkMapItems> {
    this.mapItemsCalls.push({ id, closedFilter: options?.closedFilter });
    return this.mapItemsResponses$.shift() ?? this.mapItemsResponse$;
  }

  getParkOfficialMapFile(url: string, options?: AnonymousHttpOptions): Observable<Blob> {
    this.officialMapFileCalls.push({
      url,
      skipsAuthorization: options?.context.get(SKIP_AUTHORIZATION_HEADER) ?? false
    });
    return of(new Blob(['map'], { type: 'application/pdf' }));
  }
}

class FakeSsrHttpStatusService {
  setNotFound(): void {
  }
}

function createPark(status: Park['status'] = 'Operating'): Park {
  return {
    id: 'park-1',
    name: 'Walibi Belgium',
    status,
    countryCode: 'BE',
    latitude: 50.7,
    longitude: 4.5,
    isVisible: true
  };
}

function createSummary(status: Park['status'], totalItems: number): ParkDetailSummary {
  return {
    park: createPark(status),
    mainImage: null,
    references: {
      founderName: null,
      operatorName: null
    },
    stats: {
      totalItems,
      zoneCount: totalItems > 0 ? 1 : 0,
      attractionCount: totalItems,
      restaurantCount: 0,
      showCount: 0,
      shopCount: 0,
      hotelCount: 0,
      countsByCategory: {
        Attraction: totalItems
      }
    }
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

function configureFacade(): { facade: ParkMapStateFacade; parksPort: FakeParksPort } {
  const parksPort: FakeParksPort = new FakeParksPort();

  TestBed.configureTestingModule({
    providers: [
      ParkMapStateFacade,
      { provide: PARK_MAP_PARKS_PORT, useValue: parksPort },
      { provide: SsrHttpStatusService, useClass: FakeSsrHttpStatusService }
    ]
  });

  return {
    facade: TestBed.inject(ParkMapStateFacade),
    parksPort
  };
}

describe('ParkMapStateFacade', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  afterEach(() => {
    TestBed.resetTestingModule();
    vi.restoreAllMocks();
  });

  it('keeps the default closed filter for operating parks', () => {
    const context = configureFacade();

    context.facade.loadParkMap('park-1');

    expect(context.facade.selectedClosedFilter()).toBe('openOnly');
    expect(context.parksPort.summaryCalls).toEqual([{ id: 'park-1', closedFilter: 'openOnly' }]);
    expect(context.parksPort.mapItemsCalls).toEqual([{ id: 'park-1', closedFilter: 'openOnly' }]);
  });

  it('reloads the public map data with all items for definitively closed parks', () => {
    const context = configureFacade();
    context.parksPort.summaryResponses$ = [
      of(createSummary('ClosedDefinitively', 0)),
      of(createSummary('ClosedDefinitively', 3))
    ];
    context.parksPort.mapItemsResponses$ = [
      of(createMapItems(createPark('ClosedDefinitively'))),
      of(createMapItems(createPark('ClosedDefinitively')))
    ];

    context.facade.loadParkMap('park-1');

    expect(context.facade.selectedClosedFilter()).toBe('all');
    expect(context.parksPort.summaryCalls).toEqual([
      { id: 'park-1', closedFilter: 'openOnly' },
      { id: 'park-1', closedFilter: 'all' }
    ]);
    expect(context.parksPort.mapItemsCalls).toEqual([
      { id: 'park-1', closedFilter: 'openOnly' },
      { id: 'park-1', closedFilter: 'all' }
    ]);
  });

  it('selects the newest official map year and falls back to official maps without markers', () => {
    const context = configureFacade();
    context.parksPort.mapItemsResponse$ = of({
      ...createMapItems(createPark()),
      officialMaps: [
        { id: 'map-2024', year: 2024, format: 'Pdf', documentUrl: 'https://park.example/2024.pdf' },
        { id: 'map-2026', year: 2026, format: 'Pdf', documentUrl: 'https://park.example/2026.pdf' }
      ]
    });

    context.facade.loadParkMap('park-1');

    expect(context.facade.activeTab()).toBe('official');
    expect(context.facade.selectedOfficialMapYear()).toBe(2026);
    expect(context.facade.visibleOfficialMaps().map(map => map.id)).toEqual(['map-2026']);
  });

  it('loads hidden stored maps through the HTTP port for admin preview', () => {
    vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:protected-map');
    vi.spyOn(URL, 'revokeObjectURL').mockImplementation((): void => {});
    const context = configureFacade();
    context.parksPort.mapItemsResponse$ = of({
      ...createMapItems(createPark()),
      officialMaps: [{
        id: 'map-2026',
        year: 2026,
        format: 'Pdf',
        documentUrl: 'parks/park-1/official-maps/map-2026/file',
        isVisible: false
      }]
    });

    context.facade.loadParkMap('park-1');

    expect(context.parksPort.officialMapFileCalls).toHaveLength(1);
    expect(context.parksPort.officialMapFileCalls[0].url).toContain(
      'parks/park-1/official-maps/map-2026/file',
    );
    expect(context.parksPort.officialMapFileCalls[0].skipsAuthorization).toBe(true);
    expect(context.facade.visibleOfficialMaps()[0].displayDocumentUrl).toBe('blob:protected-map');
  });

  it('keeps the interactive map selected when a marker is displayable', () => {
    const context = configureFacade();
    context.parksPort.mapItemsResponse$ = of({
      ...createMapItems(createPark()),
      items: [{
        id: 'ride-1',
        name: 'Ride',
        category: 'Attraction',
        type: 'RollerCoaster',
        latitude: 50.7,
        longitude: 4.5
      }],
      officialMaps: [
        { id: 'map-2026', year: 2026, format: 'Pdf', documentUrl: 'https://park.example/2026.pdf' }
      ]
    });

    context.facade.loadParkMap('park-1');

    expect(context.facade.activeTab()).toBe('interactive');
  });

  it('defers hidden official map downloads until the official tab is selected', () => {
    vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:protected-map');
    const context = configureFacade();
    context.parksPort.mapItemsResponse$ = of({
      ...createMapItems(createPark()),
      items: [{
        id: 'ride-1',
        name: 'Ride',
        category: 'Attraction',
        type: 'RollerCoaster',
        latitude: 50.7,
        longitude: 4.5
      }],
      officialMaps: [{
        id: 'map-2026',
        year: 2026,
        format: 'Pdf',
        documentUrl: 'parks/park-1/official-maps/map-2026/file',
        isVisible: false
      }]
    });

    context.facade.loadParkMap('park-1');

    expect(context.facade.activeTab()).toBe('interactive');
    expect(context.parksPort.officialMapFileCalls).toHaveLength(0);

    context.facade.selectTab('official');

    expect(context.parksPort.officialMapFileCalls).toHaveLength(1);
    expect(context.facade.visibleOfficialMaps()[0].displayDocumentUrl).toBe('blob:protected-map');
  });

  it('does not override a manual interactive selection when the same park reloads', () => {
    const context = configureFacade();
    context.parksPort.mapItemsResponse$ = of({
      ...createMapItems(createPark()),
      officialMaps: [
        { id: 'map-2026', year: 2026, format: 'Pdf', documentUrl: 'https://park.example/2026.pdf' }
      ]
    });

    context.facade.loadParkMap('park-1');
    context.facade.selectTab('interactive');
    context.facade.loadParkMap('park-1', 'all');

    expect(context.facade.activeTab()).toBe('interactive');
  });
});
