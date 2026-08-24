import { TestBed } from '@angular/core/testing';
import { Observable, of, Subject, throwError } from 'rxjs';

import { Park } from '@app/models/parks/park';
import { ParkAudienceClassificationFilter } from '@app/models/parks/park-audience-classification';
import { ParkMapPoint } from '@app/models/parks/park-map-point';
import { ParksApiResponse } from '@app/models/parks/parks_api_response';
import { Pagination } from '@app/models/shared/pagination';
import { CountryDisplayService } from '@shared/services/countries/country-display.service';
import { ParkRegionFilter } from '@shared/models/geo/world-region-filter.model';
import { ParkAdminListFilters } from '@data-access/parks/parks-api-endpoints';
import {
  PARK_LIST_STATE_PARKS_API_SERVICE_PORT,
  ParkListStateParksApiServicePort,
  PARK_LIST_STATE_SEARCH_API_SERVICE_PORT,
  ParkListStateSearchApiServicePort,
  PARK_LIST_STATE_STANDALONE_ATTRACTIONS_API_SERVICE_PORT,
  ParkListStateStandaloneAttractionsApiServicePort
} from './park-list-state-data.ports';
import { ParkListStateFacade } from './park-list-state.facade';
import { SearchApiResponse } from '@app/models/search/search-api-response';
import { StandaloneAttractionMapPoint } from '@app/models/standalone-attractions/standalone-attraction-map-point';
import { ClosedEntityFilter } from '@app/models/shared/closed-entity-filter';
import { ParkStatus } from '@app/models/parks/park-status';

class FakeParksPort implements ParkListStateParksApiServicePort {
  public parkResponse$: Observable<Park> = of(createPark('park-2'));
  public pageResponse$: Observable<ParksApiResponse> = of(createResponse([createPark('park-1')], createPagination(1, 9, 1)));
  public searchResponse$: Observable<ParksApiResponse> = of(createResponse([createPark('searched-park')], createPagination(1, 9, 1)));
  public mapPointsResponse$: Observable<ParkMapPoint[]> = of([createMapPoint('park-1')]);
  public readonly pageCalls: { page: number; size: number; visibleOnly: boolean; region: ParkRegionFilter | null; filters: ParkAdminListFilters | null }[] = [];
  public readonly searchCalls: { term: string; page: number; size: number; visibleOnly: boolean; region: ParkRegionFilter | null; filters: ParkAdminListFilters | null }[] = [];
  public readonly mapCalls: {
    term: string | null;
    region: ParkRegionFilter | null;
    closedFilter: ClosedEntityFilter | null;
    status: ParkStatus | null;
    audienceClassificationFilter: ParkAudienceClassificationFilter | null;
  }[] = [];
  public readonly parkByIdCalls: string[] = [];

  getParkById(id: string): Observable<Park> {
    this.parkByIdCalls.push(id);
    return this.parkResponse$;
  }

  getParksPaginated(page: number, size: number, visibleOnly: boolean = false, region: ParkRegionFilter | null = null, filters: ParkAdminListFilters | null = null): Observable<ParksApiResponse> {
    this.pageCalls.push({ page, size, visibleOnly, region, filters });
    return this.pageResponse$;
  }

  getVisibleParkMapPoints(query: string | null = null, region: ParkRegionFilter | null = null, options: {
    closedFilter?: ClosedEntityFilter;
    status?: ParkStatus | null;
    audienceClassificationFilter?: ParkAudienceClassificationFilter | null;
  } = {}): Observable<ParkMapPoint[]> {
    this.mapCalls.push({
      term: query,
      region,
      closedFilter: options.closedFilter ?? null,
      status: options.status ?? null,
      audienceClassificationFilter: options.audienceClassificationFilter ?? null
    });
    return this.mapPointsResponse$;
  }

  searchParks(query: string, page: number, size: number, visibleOnly: boolean = false, region: ParkRegionFilter | null = null, filters: ParkAdminListFilters | null = null): Observable<ParksApiResponse> {
    this.searchCalls.push({ term: query, page, size, visibleOnly, region, filters });
    return this.searchResponse$;
  }
}

class FakeSearchPort implements ParkListStateSearchApiServicePort {
  public response$: Observable<SearchApiResponse> = of({
    data: [{ originalId: 'standaloneAttraction_standalone-1', category: 'standaloneAttraction', title: 'Pendolino', description: 'Description' }],
    pagination: createPagination(1, 9, 1)
  });
  public readonly calls: Array<{ query: string; categories: string[]; page: number; size: number; region: ParkRegionFilter | null }> = [];

  getSearch(query: string, categories: string[], page: number, size: number, _options: object = {}, region: ParkRegionFilter | null = null): Observable<SearchApiResponse> {
    this.calls.push({ query, categories, page, size, region });
    return this.response$;
  }
}

class FakeStandaloneAttractionsPort implements ParkListStateStandaloneAttractionsApiServicePort {
  public response$: Observable<StandaloneAttractionMapPoint[]> = of([createStandaloneMapPoint()]);
  public readonly calls: Array<{ query: string; region: ParkRegionFilter | null }> = [];

  getVisibleMapPoints(query: string = '', region: ParkRegionFilter | null = null): Observable<StandaloneAttractionMapPoint[]> {
    this.calls.push({ query, region });
    return this.response$;
  }
}

function createPark(id: string): Park {
  return {
    id,
    name: id,
    countryCode: 'FR',
    latitude: 48.8,
    longitude: 2.3,
    isVisible: true,
    city: 'Paris',
    descriptions: [{ languageCode: 'en', value: '<p>Park description.</p>' }]
  };
}

function createMapPoint(id: string): ParkMapPoint {
  return {
    id,
    name: id,
    countryCode: 'FR',
    city: 'Paris',
    latitude: 48.8,
    longitude: 2.3,
    currentLogoImageId: null
  };
}

function createStandaloneMapPoint(): StandaloneAttractionMapPoint {
  return {
    id: 'standalone-1',
    name: 'Pendolino',
    countryCode: 'AT',
    type: 'RollerCoaster',
    subtype: 'Mountain Coaster',
    status: 'Operating',
    city: 'Nassfeld',
    street: null,
    postalCode: null,
    latitude: 46.56,
    longitude: 13.25
  };
}

function createPagination(currentPage: number, itemsPerPage: number, totalItems: number): Pagination {
  return {
    currentPage,
    itemsPerPage,
    totalItems,
    totalPages: Math.ceil(totalItems / itemsPerPage)
  };
}

function createResponse(data: Park[], pagination: Pagination): ParksApiResponse {
  return { data, pagination };
}

describe('ParkListStateFacade', () => {
  let facade: ParkListStateFacade;
  let port: FakeParksPort;
  let searchPort: FakeSearchPort;
  let standalonePort: FakeStandaloneAttractionsPort;

  beforeEach(() => {
    port = new FakeParksPort();
    searchPort = new FakeSearchPort();
    standalonePort = new FakeStandaloneAttractionsPort();

    TestBed.configureTestingModule({
      providers: [
        ParkListStateFacade,
        CountryDisplayService,
        { provide: PARK_LIST_STATE_PARKS_API_SERVICE_PORT, useValue: port },
        { provide: PARK_LIST_STATE_SEARCH_API_SERVICE_PORT, useValue: searchPort },
        { provide: PARK_LIST_STATE_STANDALONE_ATTRACTIONS_API_SERVICE_PORT, useValue: standalonePort }
      ]
    });

    facade = TestBed.inject(ParkListStateFacade);
  });

  it('loads paginated parks when no search term is provided', () => {
    facade.loadParks(1, 9, '   ', null);

    expect(port.pageCalls).toEqual([{ page: 1, size: 9, visibleOnly: true, region: null, filters: null }]);
    expect(port.searchCalls).toEqual([]);
    expect(facade.parks().map((park) => park.id)).toEqual(['park-1']);
    expect(facade.state().kind).toBe('ready');
  });

  it('searches parks when a term is provided', () => {
    facade.loadParks(2, 6, ' taron ', 'europe');

    expect(port.searchCalls).toEqual([{ term: 'taron', page: 2, size: 6, visibleOnly: true, region: 'europe', filters: null }]);
    expect(port.pageCalls).toEqual([]);
    expect(facade.parks().map((park) => park.id)).toEqual(['searched-park']);
  });

  it('sets an empty state when no park is returned', () => {
    port.pageResponse$ = of(createResponse([], createPagination(1, 9, 0)));

    facade.loadParks(1, 9, '', null);

    expect(facade.state().kind).toBe('empty');
    expect(facade.parks()).toEqual([]);
  });

  it('loads visible map points and exposes country coverage', () => {
    facade.loadVisibleMapPoints(' paris ', null);

    expect(port.mapCalls).toEqual([{
      term: ' paris ',
      region: null,
      closedFilter: 'openOnly',
      status: 'Operating',
      audienceClassificationFilter: null
    }]);
    expect(facade.visibleMapPoints().map((point) => point.id)).toEqual(['park-1']);
    expect(facade.visibleCountryCount()).toBe(1);
  });

  it('passes audience classification filters to list and map requests', () => {
    facade.setAudienceClassificationFilter('Unspecified');

    facade.loadParks(1, 9, '', null);
    facade.loadVisibleMapPoints('', null);

    expect(port.pageCalls[0].filters).toEqual({ audienceClassification: 'Unspecified' });
    expect(port.mapCalls[0].audienceClassificationFilter).toBe('Unspecified');
  });

  it('loads standalone discovery results through the shared search categories', () => {
    facade.setDiscoveryScope('standaloneAttractions');

    facade.loadDiscoveryResults('standaloneAttractions', 1, 9, ' pendolino ', 'europe');

    expect(searchPort.calls).toEqual([{
      query: 'pendolino',
      categories: ['standaloneAttractions'],
      page: 1,
      size: 9,
      region: 'europe'
    }]);
    expect(facade.searchResults().map((result) => result.originalId)).toEqual(['standaloneAttraction_standalone-1']);
  });

  it('combines park and standalone attraction points for the discovery map', () => {
    facade.setDiscoveryScope('parksAndStandaloneAttractions');

    facade.loadVisibleMapPoints('', null, 'parksAndStandaloneAttractions');

    expect(facade.visibleMapPoints().map((point) => point.kind)).toEqual(['park', 'standaloneAttraction']);
    expect(port.mapCalls[0]).toMatchObject({ closedFilter: 'all', status: null });
    expect(standalonePort.calls).toEqual([{ query: '', region: null }]);
  });

  it('ignores a stale discovery response after the parks scope reloads', () => {
    const staleResponse: Subject<SearchApiResponse> = new Subject<SearchApiResponse>();
    searchPort.response$ = staleResponse;

    facade.loadDiscoveryResults('standaloneAttractions', 1, 9, '', null);
    facade.loadParks(1, 9, '', null);
    staleResponse.next({
      data: [{ originalId: 'standaloneAttraction_stale', category: 'standaloneAttraction', title: 'Stale', description: 'Stale' }],
      pagination: createPagination(1, 9, 1)
    });

    expect(facade.parks().map((park) => park.id)).toEqual(['park-1']);
    expect(facade.searchResults()).toEqual([]);
    expect(facade.state().kind).toBe('ready');
  });

  it('ignores stale standalone map points after the parks map reloads', () => {
    const staleResponse: Subject<StandaloneAttractionMapPoint[]> = new Subject<StandaloneAttractionMapPoint[]>();
    standalonePort.response$ = staleResponse;

    facade.loadVisibleMapPoints('', null, 'standaloneAttractions');
    facade.loadVisibleMapPoints('', null, 'parks');
    staleResponse.next([createStandaloneMapPoint()]);

    expect(facade.visibleMapPoints().map((point) => point.id)).toEqual(['park-1']);
    expect(facade.visibleMapPoints().map((point) => point.kind)).toEqual(['park']);
    expect(facade.mapState().kind).toBe('ready');
  });

  it('keeps previous parks when a reload fails', () => {
    facade.loadParks(1, 9, '', null);
    port.pageResponse$ = throwError(() => new Error('network'));

    facade.loadParks(2, 9, '', null);

    expect(facade.state().kind).toBe('error');
    expect(facade.parks().map((park) => park.id)).toEqual(['park-1']);
  });
});
