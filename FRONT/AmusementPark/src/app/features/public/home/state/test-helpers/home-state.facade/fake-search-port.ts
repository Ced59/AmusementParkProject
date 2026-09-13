import { Observable, of } from 'rxjs';

import { Park } from '@app/models/parks/park';

import { SearchApiResponse } from '@app/models/search/search-api-response';

import { SearchResultItem } from '@app/models/search/search-result-item';

import { Pagination } from '@app/models/shared/pagination';

import { HomeStateSearchApiServicePort } from '../../home-state-data.ports';

function createSearchResult(): SearchResultItem {
  return {
    originalId: 'park-1',
    category: 'Park',
    title: 'Parc de test',
    description: 'Description de test',
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

function createSearchResponse(
  data: SearchResultItem[],
  pagination: Pagination,
): SearchApiResponse {
  return { data, pagination };
}

export class FakeSearchPort implements HomeStateSearchApiServicePort {
  public response$: Observable<SearchApiResponse> = of(
    createSearchResponse([createSearchResult()], createPagination(1, 10, 1)),
  );
  public readonly calls: {
    term: string;
    categories: string[];
    page: number;
    size: number;
  }[] = [];

  getSearch(
    term: string,
    categories: string[],
    page: number,
    size: number,
  ): Observable<SearchApiResponse> {
    this.calls.push({ term, categories, page, size });
    return this.response$;
  }
}
