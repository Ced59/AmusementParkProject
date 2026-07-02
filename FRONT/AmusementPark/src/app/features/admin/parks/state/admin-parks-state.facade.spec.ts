import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { BulkAdministrationUpdateRequest, BulkAdministrationUpdateResult } from '@app/models/admin/admin-review-status';
import { Park } from '@app/models/parks/park';
import { ParksApiResponse } from '@app/models/parks/parks_api_response';
import { Pagination } from '@app/models/shared/pagination';
import { ParkAdminListFilters, ParkAdminListSort } from '@data-access/parks/parks-api-endpoints';
import {
  ADMIN_PARKS_STATE_PARKS_API_SERVICE_PORT,
  AdminParksStateParksApiServicePort
} from './admin-parks-state-data.ports';
import { AdminParksStateFacade } from './admin-parks-state.facade';

class FakeParksPort implements AdminParksStateParksApiServicePort {
  public pageResponse$: Observable<ParksApiResponse> = of(createResponse([createPark('park-1')], createPagination(1, 10, 1)));
  public searchResponse$: Observable<ParksApiResponse> = of(createResponse([createPark('searched-park')], createPagination(1, 10, 1)));
  public bulkResponse$: Observable<BulkAdministrationUpdateResult> = of({ requestedCount: 1, updatedCount: 1 });
  public readonly pageCalls: { page: number; size: number; filters: ParkAdminListFilters | null; sort: ParkAdminListSort | null }[] = [];
  public readonly searchCalls: { query: string; page: number; size: number; filters: ParkAdminListFilters | null; sort: ParkAdminListSort | null }[] = [];
  public readonly bulkCalls: BulkAdministrationUpdateRequest[] = [];

  getParksPaginated(page: number, size: number, visibleOnly: boolean = false, region = null, filters: ParkAdminListFilters | null = null, options: { sort?: ParkAdminListSort } = {}): Observable<ParksApiResponse> {
    this.pageCalls.push({ page, size, filters, sort: options.sort ?? null });
    return this.pageResponse$;
  }

  searchParks(query: string, page: number, size: number, visibleOnly: boolean = false, region = null, filters: ParkAdminListFilters | null = null, options: { sort?: ParkAdminListSort } = {}): Observable<ParksApiResponse> {
    this.searchCalls.push({ query, page, size, filters, sort: options.sort ?? null });
    return this.searchResponse$;
  }

  updateParksBulkAdministration(request: BulkAdministrationUpdateRequest): Observable<BulkAdministrationUpdateResult> {
    this.bulkCalls.push(request);
    return this.bulkResponse$;
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
    descriptions: []
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

describe('AdminParksStateFacade', () => {
  let facade: AdminParksStateFacade;
  let port: FakeParksPort;

  beforeEach(() => {
    port = new FakeParksPort();

    TestBed.configureTestingModule({
      providers: [
        AdminParksStateFacade,
        { provide: ADMIN_PARKS_STATE_PARKS_API_SERVICE_PORT, useValue: port }
      ]
    });

    facade = TestBed.inject(AdminParksStateFacade);
  });

  it('loads parks through the paginated endpoint by default', () => {
    facade.loadParks(2, 25);

    expect(port.pageCalls.length).toBe(1);
    expect(port.pageCalls[0].page).toBe(2);
    expect(port.pageCalls[0].size).toBe(25);
    expect(port.searchCalls).toEqual([]);
    expect(facade.parks().map((park: Park) => park.id)).toEqual(['park-1']);
    expect(facade.state().kind).toBe('ready');
  });

  it('switches to search when a query is set', () => {
    facade.setSearchQuery(' phantasialand ');

    facade.loadParks(1, 10);

    expect(port.pageCalls).toEqual([]);
    expect(port.searchCalls.length).toBe(1);
    expect(port.searchCalls[0].query).toBe('phantasialand');
    expect(facade.parks().map((park: Park) => park.id)).toEqual(['searched-park']);
  });

  it('applies filters and resets to the first page', () => {
    facade.updateFilters({ isVisible: true, countryCode: 'DE', hasValidCoordinates: true, audienceClassification: 'Unspecified' });

    expect(facade.visibilityFilter()).toBeTrue();
    expect(facade.countryCodeFilter()).toBe('DE');
    expect(facade.validCoordinatesFilter()).toBeTrue();
    expect(port.pageCalls[0].page).toBe(1);
    expect(port.pageCalls[0].filters).toEqual({
      isVisible: true,
      adminReviewStatus: null,
      type: null,
      audienceClassification: 'Unspecified',
      countryCode: 'DE',
      hasValidCoordinates: true,
      openingHoursStatus: 'all'
    });
  });

  it('passes admin sort options to park list requests', () => {
    const changed: boolean = facade.updateSort('parkItemsVisibleCount', 'desc');

    facade.loadParks(1, 10);

    expect(changed).toBeTrue();
    expect(port.pageCalls[0].sort).toEqual({ sortBy: 'parkItemsVisibleCount', sortDirection: 'desc' });
  });

  it('delegates bulk administration updates to the port', () => {
    const request: BulkAdministrationUpdateRequest = {
      ids: ['park-1'],
      isVisible: false,
      adminReviewStatus: 'Validated'
    };

    facade.updateBulkAdministration(request).subscribe((result: BulkAdministrationUpdateResult) => {
      expect(result.updatedCount).toBe(1);
    });

    expect(port.bulkCalls).toEqual([request]);
  });

  it('makes filtered valid-coordinate parks visible through criteria bulk update', () => {
    facade.updateFilters({ adminReviewStatus: 'ToReview', type: 'ThemePark', countryCode: 'FR', hasValidCoordinates: false, audienceClassification: 'National' });
    port.bulkCalls.length = 0;

    facade.makeFilteredValidCoordinateParksVisible().subscribe((result: BulkAdministrationUpdateResult) => {
      expect(result.updatedCount).toBe(1);
    });

    expect(port.bulkCalls).toEqual([{
      ids: [],
      isVisible: true,
      adminReviewStatus: null,
      filterIsVisible: null,
      filterAdminReviewStatus: 'ToReview',
      filterType: 'ThemePark',
      filterAudienceClassification: 'National',
      filterCountryCode: 'FR',
      filterHasValidCoordinates: true
    }]);
  });

  it('keeps previous data when a reload fails', () => {
    spyOn(console, 'error');
    facade.loadParks(1, 10);
    port.pageResponse$ = throwError(() => new Error('network'));

    facade.loadParks(2, 10);

    expect(console.error).toHaveBeenCalled();
    expect(facade.state().kind).toBe('error');
    expect(facade.parks().map((park: Park) => park.id)).toEqual(['park-1']);
  });
});
