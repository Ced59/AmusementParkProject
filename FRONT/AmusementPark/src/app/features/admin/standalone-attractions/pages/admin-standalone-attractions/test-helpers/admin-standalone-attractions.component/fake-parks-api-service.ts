import { Observable, of } from 'rxjs';

import { Park } from '@app/models/parks/park';

import { ParksApiResponse } from '@app/models/parks/parks_api_response';

import { ParkAdminListFilters, ParkAdminListSort } from '@data-access/parks/parks-api-endpoints';

import { PaginationContract } from '@shared/models/contracts';

function createPagination(): PaginationContract {
  return {
    currentPage: 1,
    itemsPerPage: 10,
    totalItems: 1,
    totalPages: 1,
  };
}

function createPark(id: string): Park {
  return {
    id,
    name: 'Bardonecchia Alpine Coaster',
    countryCode: 'IT',
    type: 'ThemePark',
    latitude: 45.07,
    longitude: 6.7,
    isVisible: false,
    adminReviewStatus: 'ToReview',
    city: 'Bardonecchia',
    parkItemsTotalCount: 1,
    parkItemsVisibleCount: 0,
    descriptions: [],
  };
}

export class FakeParksApiService {
  public searchResponse$: Observable<ParksApiResponse> = of({
    data: [createPark('legacy-park-1')],
    pagination: createPagination(),
  });
  public parkByIdResponse$: Observable<Park> = of(
    createPark('legacy-park-by-id'),
  );
  public readonly searchCalls: Array<{
    query: string;
    page: number;
    size: number;
    filters: ParkAdminListFilters | null;
    options: {
      closedFilter?: string;
      sort?: ParkAdminListSort;
    };
  }> = [];
  public readonly getByIdCalls: string[] = [];

  searchParks(
    query: string,
    page: number,
    size: number,
    _visibleOnly: boolean = false,
    _region = null,
    filters: ParkAdminListFilters | null = null,
    options: {
      closedFilter?: string;
      sort?: ParkAdminListSort;
    } = {},
  ): Observable<ParksApiResponse> {
    this.searchCalls.push({ query, page, size, filters, options });
    return this.searchResponse$;
  }

  getParkById(id: string): Observable<Park> {
    this.getByIdCalls.push(id);
    return this.parkByIdResponse$;
  }
}
