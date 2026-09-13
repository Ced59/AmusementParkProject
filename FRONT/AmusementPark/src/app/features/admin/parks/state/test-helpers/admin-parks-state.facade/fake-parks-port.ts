import { Observable, of } from 'rxjs';

import { BulkAdministrationUpdateRequest, BulkAdministrationUpdateResult } from '@app/models/admin/admin-review-status';

import { Park } from '@app/models/parks/park';

import { ParksApiResponse } from '@app/models/parks/parks_api_response';

import { Pagination } from '@app/models/shared/pagination';

import { ParkAdminListFilters, ParkAdminListSort } from '@data-access/parks/parks-api-endpoints';

import { AdminParksStateParksApiServicePort } from '../../admin-parks-state-data.ports';

function createPark(id: string): Park {
  return {
    id,
    name: id,
    countryCode: 'FR',
    latitude: 48.8,
    longitude: 2.3,
    isVisible: true,
    descriptions: [],
  };
}

function createPagination(
  currentPage: number,
  itemsPerPage: number,
  totalItems: number,
): Pagination {
  return {
    currentPage,
    itemsPerPage,
    totalItems,
    totalPages: Math.ceil(totalItems / itemsPerPage),
  };
}

function createResponse(
  data: Park[],
  pagination: Pagination,
): ParksApiResponse {
  return { data, pagination };
}

export class FakeParksPort implements AdminParksStateParksApiServicePort {
  public pageResponse$: Observable<ParksApiResponse> = of(
    createResponse([createPark('park-1')], createPagination(1, 10, 1)),
  );
  public searchResponse$: Observable<ParksApiResponse> = of(
    createResponse([createPark('searched-park')], createPagination(1, 10, 1)),
  );
  public bulkResponse$: Observable<BulkAdministrationUpdateResult> = of({
    requestedCount: 1,
    updatedCount: 1,
  });
  public readonly pageCalls: {
    page: number;
    size: number;
    filters: ParkAdminListFilters | null;
    sort: ParkAdminListSort | null;
  }[] = [];
  public readonly searchCalls: {
    query: string;
    page: number;
    size: number;
    filters: ParkAdminListFilters | null;
    sort: ParkAdminListSort | null;
  }[] = [];
  public readonly bulkCalls: BulkAdministrationUpdateRequest[] = [];

  getParksPaginated(
    page: number,
    size: number,
    visibleOnly: boolean = false,
    region = null,
    filters: ParkAdminListFilters | null = null,
    options: {
      sort?: ParkAdminListSort;
    } = {},
  ): Observable<ParksApiResponse> {
    this.pageCalls.push({ page, size, filters, sort: options.sort ?? null });
    return this.pageResponse$;
  }

  searchParks(
    query: string,
    page: number,
    size: number,
    visibleOnly: boolean = false,
    region = null,
    filters: ParkAdminListFilters | null = null,
    options: {
      sort?: ParkAdminListSort;
    } = {},
  ): Observable<ParksApiResponse> {
    this.searchCalls.push({
      query,
      page,
      size,
      filters,
      sort: options.sort ?? null,
    });
    return this.searchResponse$;
  }

  updateParksBulkAdministration(
    request: BulkAdministrationUpdateRequest,
  ): Observable<BulkAdministrationUpdateResult> {
    this.bulkCalls.push(request);
    return this.bulkResponse$;
  }
}
