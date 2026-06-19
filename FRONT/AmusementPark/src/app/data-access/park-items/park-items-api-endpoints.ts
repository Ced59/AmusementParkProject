import { AdminReviewStatus } from '@app/models/admin/admin-review-status';
import { ParkItemCategory } from '@app/models/parks/park-item-category';
import { ParkItemType } from '@app/models/parks/park-item-type';

export interface ParkItemAdminListFilters {
  isVisible?: boolean | null;
  adminReviewStatus?: AdminReviewStatus | null;
  category?: ParkItemCategory | null;
  type?: ParkItemType | null;
  zoneId?: string | null;
  manufacturerId?: string | null;
  contentBacklogFilter?: ParkItemContentBacklogFilter | null;
}

export type ParkItemAdminSortField = 'default' | 'name' | 'category' | 'type' | 'isVisible' | 'adminReviewStatus' | 'parkId' | 'zoneId';
export type ParkItemAdminSortDirection = 'asc' | 'desc';
export type ParkItemContentBacklogFilter = 'MissingDescriptionFr' | 'MissingDescriptionEn' | 'MissingAnyDescription' | 'MissingZone' | 'MissingPreciseType' | 'VisibleIncomplete';

export interface ParkItemAdminListSort {
  sortBy: ParkItemAdminSortField;
  sortDirection: ParkItemAdminSortDirection;
}

function buildParkItemAdminListQuery(filters: ParkItemAdminListFilters | null = null): string {
  if (!filters) {
    return '';
  }

  const params: string[] = [];
  if (filters.isVisible !== null && filters.isVisible !== undefined) {
    params.push(`isVisible=${filters.isVisible}`);
  }
  if (filters.adminReviewStatus) {
    params.push(`adminReviewStatus=${encodeURIComponent(filters.adminReviewStatus)}`);
  }
  if (filters.category) {
    params.push(`category=${encodeURIComponent(filters.category)}`);
  }
  if (filters.type) {
    params.push(`type=${encodeURIComponent(filters.type)}`);
  }
  if (filters.zoneId) {
    params.push(`zoneId=${encodeURIComponent(filters.zoneId)}`);
  }
  if (filters.manufacturerId) {
    params.push(`manufacturerId=${encodeURIComponent(filters.manufacturerId)}`);
  }
  if (filters.contentBacklogFilter) {
    params.push(`contentBacklogFilter=${encodeURIComponent(filters.contentBacklogFilter)}`);
  }

  return params.length > 0 ? `&${params.join('&')}` : '';
}

function buildParkItemAdminSortQuery(sort: ParkItemAdminListSort | null = null): string {
  if (!sort || sort.sortBy === 'default') {
    return '';
  }

  return `&sortBy=${encodeURIComponent(sort.sortBy)}&sortDirection=${encodeURIComponent(sort.sortDirection)}`;
}

export const PARK_ITEMS_API_ENDPOINTS = {
  getParkItemsByParkId: (parkId: string, page: number = 1, size: number = 100) => `park-items/park/${parkId}?page=${page}&size=${size}`,
  getParkItemSiblingNavigation: (parkItemId: string) => `park-items/${encodeURIComponent(parkItemId)}/siblings`,
  getRelatedParkItems: (parkItemId: string, limit: number = 3) => `park-items/${encodeURIComponent(parkItemId)}/related?limit=${limit}`,
  getParkItemsPaginated: (
    page: number,
    size: number,
    parkId?: string | null,
    search?: string | null,
    filters: ParkItemAdminListFilters | null = null,
    sort: ParkItemAdminListSort | null = null
  ) => {
    const parkIdQuery: string = parkId ? `&parkId=${encodeURIComponent(parkId)}` : '';
    const searchQuery: string = search ? `&search=${encodeURIComponent(search)}` : '';
    return `park-items?page=${page}&size=${size}${parkIdQuery}${searchQuery}${buildParkItemAdminListQuery(filters)}${buildParkItemAdminSortQuery(sort)}`;
  },
  getParkItemById: (id: string) => `park-items/${id}`,
  createParkItem: 'park-items',
  updateParkItem: (id: string) => `park-items/${id}`,
  deleteParkItem: (id: string) => `park-items/${id}`,
  updateParkItemsBulkAdministration: 'park-items/bulk-administration',
  updateParkItemsBulkFields: 'park-items/bulk-fields',
  previewParkItemsBulkCreate: 'park-items/bulk-create/preview',
  applyParkItemsBulkCreate: 'park-items/bulk-create/apply'
};
