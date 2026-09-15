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
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { SearchApiResponse } from '@app/models/search/search-api-response';
import { StandaloneAttractionMapPoint } from '@app/models/standalone-attractions/standalone-attraction-map-point';
import { ClosedEntityFilter } from '@app/models/shared/closed-entity-filter';
import { ParkStatus } from '@app/models/parks/park-status';
import { FakeParksPort } from './test-helpers/park-list-state.facade/fake-parks-port';
import { FakeSearchPort } from './test-helpers/park-list-state.facade/fake-search-port';
import { FakeStandaloneAttractionsPort } from './test-helpers/park-list-state.facade/fake-standalone-attractions-port';

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
  const httpStatus = { setNotFound: vi.fn(), setStatus: vi.fn() };
  let facade: ParkListStateFacade;
  let port: FakeParksPort;
  let searchPort: FakeSearchPort;
  let standalonePort: FakeStandaloneAttractionsPort;

  beforeEach(() => {
    httpStatus.setNotFound.mockClear();
    httpStatus.setStatus.mockClear();
    port = new FakeParksPort();
    searchPort = new FakeSearchPort();
    standalonePort = new FakeStandaloneAttractionsPort();

    TestBed.configureTestingModule({
      providers: [
        ParkListStateFacade,
        { provide: SsrHttpStatusService, useValue: httpStatus },
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

  it('validates only the requested standard page after receiving its data', () => {
    facade.setCurrentLanguage('fr');
    port.pageResponse$ = of(createResponse([createPark('page-2')], createPagination(2, 9, 10)));
    facade.loadParks(2, 9, '', null, true);
    expect(facade.resolvedPage()).toEqual({ language: 'fr', page: 2 });
    expect(facade.parks().map(park => park.id)).toEqual(['page-2']);
    expect(port.mapCalls).toEqual([]);
  });

  it('clears a resolved page while loading and ignores a late response after another page wins', () => {
    facade.loadParks(1, 9, '', null, true);
    const late = new Subject<ParksApiResponse>();
    port.pageResponse$ = late;
    facade.loadParks(2, 9, '', null, true);
    expect(facade.resolvedPage()).toBeNull();
    port.pageResponse$ = of(createResponse([createPark('page-3')], createPagination(3, 9, 19)));
    facade.loadParks(3, 9, '', null, true);
    late.next(createResponse([createPark('late-page-2')], createPagination(2, 9, 19)));
    expect(facade.resolvedPage()?.page).toBe(3);
    expect(facade.parks().map(park => park.id)).toEqual(['page-3']);
  });

  it('returns not found beyond the last page, without retaining earlier cards', () => {
    facade.loadParks(1, 9, '', null, true);
    port.pageResponse$ = of(createResponse([], createPagination(3, 9, 10)));
    facade.loadParks(3, 9, '', null, true);
    expect(httpStatus.setNotFound).toHaveBeenCalledOnce();
    expect(facade.resolvedPage()).toBeNull();
    expect(facade.parks()).toEqual([]);
    expect(facade.state().kind).toBe('error');
  });

  it('keeps an empty first page unvalidated without declaring a missing directory', () => {
    port.pageResponse$ = of(createResponse([], createPagination(1, 9, 0)));
    facade.loadParks(1, 9, '', null, true);
    expect(facade.resolvedPage()).toBeNull();
    expect(facade.state().kind).toBe('empty');
    expect(httpStatus.setNotFound).not.toHaveBeenCalled();
  });

  it('does not validate inconsistent API pagination', () => {
    port.pageResponse$ = of(createResponse([createPark('wrong-page')], createPagination(2, 9, 19)));
    facade.loadParks(1, 9, '', null, true);
    expect(httpStatus.setStatus).toHaveBeenCalledWith(503);
    expect(facade.resolvedPage()).toBeNull();
    expect(facade.parks()).toEqual([]);
  });

  it.each([0, 500, 503])('propagates API failure %s as unavailable, not a false indexable success', status => {
    port.pageResponse$ = throwError(() => ({ status }));
    facade.loadParks(2, 9, '', null, true);
    expect(httpStatus.setStatus).toHaveBeenCalledWith(503);
    expect(facade.resolvedPage()).toBeNull();
  });

  it('does not grant standard-page metadata to custom sizes or active filters', () => {
    facade.loadParks(1, 18, '', null, true);
    expect(facade.resolvedPage()).toBeNull();
    facade.setStatus('Planned');
    facade.loadParks(1, 9, '', null, true);
    expect(facade.resolvedPage()).toBeNull();
    expect(httpStatus.setStatus).not.toHaveBeenCalled();
  });

  it('invalidates in-flight page metadata when its URL is rejected', () => {
    const pending = new Subject<ParksApiResponse>();
    port.pageResponse$ = pending;
    facade.loadParks(1, 9, '', null, true);
    facade.rejectInvalidPage();
    pending.next(createResponse([createPark('old')], createPagination(1, 9, 1)));
    expect(facade.resolvedPage()).toBeNull();
    expect(facade.parks()).toEqual([]);
    expect(httpStatus.setNotFound).toHaveBeenCalledOnce();
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

  it('ignores a stale marker detail response after the selection is cleared', () => {
    const staleResponse: Subject<Park> = new Subject<Park>();
    port.parkResponse$ = staleResponse;

    facade.selectParkFromMap('park-2');
    facade.clearSelectedPark();
    staleResponse.next(createPark('park-2'));

    expect(facade.selectedParkId()).toBeNull();
    expect(facade.selectedParkCard()).toBeNull();
  });

  it('highlights a discovery map point without filtering mixed results to one park', () => {
    facade.loadParks(1, 9, '', null);
    facade.selectParkFromCard(facade.parks()[0]);

    facade.selectDiscoveryPointFromMap('park-2');

    expect(facade.selectedParkId()).toBe('park-2');
    expect(facade.selectedParkCard()).toBeNull();
    expect(facade.displayedParks().map((park) => park.id)).toEqual(['park-1']);
    expect(port.parkByIdCalls).toEqual([]);
  });

  it('keeps previous parks when a reload fails', () => {
    facade.loadParks(1, 9, '', null);
    port.pageResponse$ = throwError(() => new Error('network'));

    facade.loadParks(2, 9, '', null);

    expect(facade.state().kind).toBe('error');
    expect(facade.parks().map((park) => park.id)).toEqual(['park-1']);
  });
});
