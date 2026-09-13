import { Observable, of } from 'rxjs';

import { Pagination } from '@app/models/shared/pagination';

import { ParkRegionFilter } from '@shared/models/geo/world-region-filter.model';

import { ParkListStateSearchApiServicePort } from '../../park-list-state-data.ports';

import { SearchApiResponse } from '@app/models/search/search-api-response';

function createPagination(currentPage: number, itemsPerPage: number, totalItems: number): Pagination {
  return {
    currentPage,
    itemsPerPage,
    totalItems,
    totalPages: Math.ceil(totalItems / itemsPerPage)
  };
}

export class FakeSearchPort implements ParkListStateSearchApiServicePort {
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
