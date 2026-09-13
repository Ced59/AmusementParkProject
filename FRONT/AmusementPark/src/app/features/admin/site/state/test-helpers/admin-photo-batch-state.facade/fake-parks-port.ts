import { Observable, of } from 'rxjs';

import { ParksApiResponse } from '@app/models/parks/parks_api_response';

import { AdminPhotoBatchParksPort } from '../../admin-photo-batch-state-data.ports';

function createPagination(
  totalItems: number,
  currentPage: number = 1,
  totalPages: number = 1,
  itemsPerPage: number = Math.max(totalItems, 1),
) {
  return {
    totalItems,
    totalPages,
    currentPage,
    itemsPerPage,
  };
}

export class FakeParksPort implements AdminPhotoBatchParksPort {
  getParksPaginated(): Observable<ParksApiResponse> {
    return of({
      data: [
        {
          id: 'park-1',
          name: 'Demo Park',
          latitude: 1,
          longitude: 2,
          descriptions: [],
          isVisible: true,
        },
      ],
      pagination: createPagination(1),
    });
  }

  searchParks(): Observable<ParksApiResponse> {
    return this.getParksPaginated();
  }
}
