import { Observable, of } from 'rxjs';

import { ParkItemAdminRow } from '@app/models/parks/park-item-admin-row';

import { ApiResponse } from '@app/models/shared/api_reponse';

import { AdminPhotoBatchParkItemsPort } from '../../admin-photo-batch-state-data.ports';

function createParkItemRow(id: string, name: string): ParkItemAdminRow {
  return {
    id,
    parkId: 'park-1',
    parkName: 'Demo Park',
    name,
    category: 'Attraction',
    type: 'RollerCoaster',
    isVisible: true,
    adminReviewStatus: 'Validated',
  };
}

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

export class FakeParkItemsPort implements AdminPhotoBatchParkItemsPort {
  public responsesByPage: Record<number, ApiResponse<ParkItemAdminRow>> = {};
  public readonly calls: Array<{
    page: number;
    size: number;
    parkId: string | null | undefined;
  }> = [];

  getParkItemsPaginated(
    page: number,
    size: number,
    parkId?: string | null,
  ): Observable<ApiResponse<ParkItemAdminRow>> {
    this.calls.push({ page, size, parkId });
    return of(
      this.responsesByPage[page] ?? {
        data: [createParkItemRow('item-1', 'Demo Coaster')],
        pagination: createPagination(1),
      },
    );
  }
}
