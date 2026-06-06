import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { Park } from '@app/models/parks/park';
import { ParkMapPoint } from '@app/models/parks/park-map-point';
import { ParksApiResponse } from '@app/models/parks/parks_api_response';
import { Pagination } from '@app/models/shared/pagination';
import { CountryDisplayService } from '@shared/services/countries/country-display.service';
import { ParkRegionFilter } from '@shared/models/geo/world-region-filter.model';
import {
  PARK_LIST_STATE_PARKS_API_SERVICE_PORT,
  ParkListStateParksApiServicePort
} from './park-list-state-data.ports';
import { ParkListStateFacade } from './park-list-state.facade';

class FakeParksPort implements ParkListStateParksApiServicePort {
  public parkResponse$: Observable<Park> = of(createPark('park-2'));
  public pageResponse$: Observable<ParksApiResponse> = of(createResponse([createPark('park-1')], createPagination(1, 9, 1)));
  public searchResponse$: Observable<ParksApiResponse> = of(createResponse([createPark('searched-park')], createPagination(1, 9, 1)));
  public mapPointsResponse$: Observable<ParkMapPoint[]> = of([createMapPoint('park-1')]);
  public readonly pageCalls: { page: number; size: number; visibleOnly: boolean; region: ParkRegionFilter | null }[] = [];
  public readonly searchCalls: { term: string; page: number; size: number; visibleOnly: boolean; region: ParkRegionFilter | null }[] = [];
  public readonly mapCalls: { term: string | null; region: ParkRegionFilter | null }[] = [];
  public readonly parkByIdCalls: string[] = [];

  getParkById(id: string): Observable<Park> {
    this.parkByIdCalls.push(id);
    return this.parkResponse$;
  }

  getParksPaginated(page: number, size: number, visibleOnly: boolean = false, region: ParkRegionFilter | null = null): Observable<ParksApiResponse> {
    this.pageCalls.push({ page, size, visibleOnly, region });
    return this.pageResponse$;
  }

  getVisibleParkMapPoints(query: string | null = null, region: ParkRegionFilter | null = null): Observable<ParkMapPoint[]> {
    this.mapCalls.push({ term: query, region });
    return this.mapPointsResponse$;
  }

  searchParks(query: string, page: number, size: number, visibleOnly: boolean = false, region: ParkRegionFilter | null = null): Observable<ParksApiResponse> {
    this.searchCalls.push({ term: query, page, size, visibleOnly, region });
    return this.searchResponse$;
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

  beforeEach(() => {
    port = new FakeParksPort();

    TestBed.configureTestingModule({
      providers: [
        ParkListStateFacade,
        CountryDisplayService,
        { provide: PARK_LIST_STATE_PARKS_API_SERVICE_PORT, useValue: port }
      ]
    });

    facade = TestBed.inject(ParkListStateFacade);
  });

  it('loads paginated parks when no search term is provided', () => {
    facade.loadParks(1, 9, '   ', null);

    expect(port.pageCalls).toEqual([{ page: 1, size: 9, visibleOnly: true, region: null }]);
    expect(port.searchCalls).toEqual([]);
    expect(facade.parks().map((park) => park.id)).toEqual(['park-1']);
    expect(facade.state().kind).toBe('ready');
  });

  it('searches parks when a term is provided', () => {
    facade.loadParks(2, 6, ' taron ', 'europe');

    expect(port.searchCalls).toEqual([{ term: 'taron', page: 2, size: 6, visibleOnly: true, region: 'europe' }]);
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

    expect(port.mapCalls).toEqual([{ term: ' paris ', region: null }]);
    expect(facade.visibleMapPoints().map((point) => point.id)).toEqual(['park-1']);
    expect(facade.visibleCountryCount()).toBe(1);
  });

  it('keeps previous parks when a reload fails', () => {
    facade.loadParks(1, 9, '', null);
    port.pageResponse$ = throwError(() => new Error('network'));

    facade.loadParks(2, 9, '', null);

    expect(facade.state().kind).toBe('error');
    expect(facade.parks().map((park) => park.id)).toEqual(['park-1']);
  });
});
