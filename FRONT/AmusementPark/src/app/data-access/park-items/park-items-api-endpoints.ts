import { AdminReviewStatus } from '@app/models/admin/admin-review-status';
import { ParkItemCategory } from '@app/models/parks/park-item-category';
import { ParkItemType } from '@app/models/parks/park-item-type';
import { ClosedEntityFilter, DEFAULT_CLOSED_ENTITY_FILTER } from '@app/models/shared/closed-entity-filter';

export interface ParkItemAdminListFilters {
  isVisible?: boolean | null;
  adminReviewStatus?: AdminReviewStatus | null;
  category?: ParkItemCategory | null;
  type?: ParkItemType | null;
  zoneId?: string | null;
  manufacturerId?: string | null;
  contentBacklogFilter?: ParkItemContentBacklogFilter | null;
}

export type ParkItemAdminSortField = 'default' | 'name' | 'category' | 'type' | 'isVisible' | 'adminReviewStatus' | 'parkId' | 'zoneId' | 'dataCompletenessScore';
export type ParkItemAdminSortDirection = 'asc' | 'desc';
export type ParkItemContentBacklogFilter = 'MissingDescriptionFr' | 'MissingDescriptionEn' | 'MissingAnyDescription' | 'MissingZone' | 'MissingPreciseType' | 'VisibleIncomplete';

export interface ParkItemAdminListSort {
  sortBy: ParkItemAdminSortField;
  sortDirection: ParkItemAdminSortDirection;
}

export interface ParkItemsByParkIdFilters {
  closedFilter?: ClosedEntityFilter | null;
  search?: string | null;
  category?: ParkItemCategory | null;
  type?: ParkItemType | null;
  zoneId?: string | null;
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

function buildClosedFilterQuery(closedFilter: ClosedEntityFilter | null | undefined, prefix: '?' | '&' = '&'): string {
  if (!closedFilter || closedFilter === DEFAULT_CLOSED_ENTITY_FILTER) {
    return '';
  }

  return `${prefix}closedFilter=${encodeURIComponent(closedFilter)}`;
}

function buildParkItemsByParkIdQuery(filters: ParkItemsByParkIdFilters | null = null): string {
  if (!filters) {
    return '';
  }

  const params: string[] = [];
  if (filters.closedFilter && filters.closedFilter !== DEFAULT_CLOSED_ENTITY_FILTER) {
    params.push(`closedFilter=${encodeURIComponent(filters.closedFilter)}`);
  }
  if (filters.search?.trim()) {
    params.push(`search=${encodeURIComponent(filters.search.trim())}`);
  }
  if (filters.category) {
    params.push(`category=${encodeURIComponent(filters.category)}`);
  }
  if (filters.type) {
    params.push(`type=${encodeURIComponent(filters.type)}`);
  }
  if (filters.zoneId?.trim()) {
    params.push(`zoneId=${encodeURIComponent(filters.zoneId.trim())}`);
  }

  return params.length > 0 ? `&${params.join('&')}` : '';
}

export const PARK_ITEMS_API_ENDPOINTS = {
  getParkItemsByParkId: (parkId: string, page: number = 1, size: number = 100, filters: ParkItemsByParkIdFilters | ClosedEntityFilter | null = null) => {
    const normalizedFilters: ParkItemsByParkIdFilters | null = typeof filters === 'string' ? { closedFilter: filters } : filters;
    return `park-items/park/${encodeURIComponent(parkId)}?page=${page}&size=${size}${buildParkItemsByParkIdQuery(normalizedFilters)}`;
  },
  getParkItemSiblingNavigation: (parkItemId: string, closedFilter?: ClosedEntityFilter | null) => `park-items/${encodeURIComponent(parkItemId)}/siblings${buildClosedFilterQuery(closedFilter, '?')}`,
  getRelatedParkItems: (parkItemId: string, limit: number = 3, closedFilter?: ClosedEntityFilter | null) => `park-items/${encodeURIComponent(parkItemId)}/related?limit=${limit}${buildClosedFilterQuery(closedFilter)}`,
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
  updateParkItemsVisibilityByParkIds: 'park-items/bulk-park-visibility',
  updateParkItemsBulkFields: 'park-items/bulk-fields',
  previewParkItemsBulkCreate: 'park-items/bulk-create/preview',
  applyParkItemsBulkCreate: 'park-items/bulk-create/apply'
};
