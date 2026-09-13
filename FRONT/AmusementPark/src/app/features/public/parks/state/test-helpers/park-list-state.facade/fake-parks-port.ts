import { Observable, of } from 'rxjs';

import { Park } from '@app/models/parks/park';

import { ParkAudienceClassificationFilter } from '@app/models/parks/park-audience-classification';

import { ParkMapPoint } from '@app/models/parks/park-map-point';

import { ParksApiResponse } from '@app/models/parks/parks_api_response';

import { Pagination } from '@app/models/shared/pagination';

import { ParkRegionFilter } from '@shared/models/geo/world-region-filter.model';

import { ParkAdminListFilters } from '@data-access/parks/parks-api-endpoints';

import { ParkListStateParksApiServicePort } from '../../park-list-state-data.ports';

import { ClosedEntityFilter } from '@app/models/shared/closed-entity-filter';

import { ParkStatus } from '@app/models/parks/park-status';

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

export class FakeParksPort implements ParkListStateParksApiServicePort {
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
