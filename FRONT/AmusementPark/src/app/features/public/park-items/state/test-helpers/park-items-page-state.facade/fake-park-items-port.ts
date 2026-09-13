import { Observable, of } from 'rxjs';

import { ParkItem } from '@app/models/parks/park-item';

import { PagedResult } from '@shared/models/contracts';

import { ParkItemsByParkIdFilters } from '@data-access/park-items/park-items-api-endpoints';

import { ParkItemsPageStateParkItemsApiServicePort } from '../../park-items-page-state-data.ports';

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

export class FakeParkItemsPort implements ParkItemsPageStateParkItemsApiServicePort {
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
