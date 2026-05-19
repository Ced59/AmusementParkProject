import { AdminReviewStatus } from '@app/models/admin/admin-review-status';
import { ParkItemCategory } from '@app/models/parks/park-item-category';
import { ParkItemType } from '@app/models/parks/park-item-type';

export interface ParkItemAdminListFilters {
  isVisible?: boolean | null;
  adminReviewStatus?: AdminReviewStatus | null;
  category?: ParkItemCategory | null;
  type?: ParkItemType | null;
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

  return params.length > 0 ? `&${params.join('&')}` : '';
}

export const PARK_ITEMS_API_ENDPOINTS = {
  getParkItemsByParkId: (parkId: string) => `park-items/park/${parkId}`,
  getParkItemsPaginated: (page: number, size: number, parkId?: string | null, search?: string | null, filters: ParkItemAdminListFilters | null = null) => {
    const parkIdQuery: string = parkId ? `&parkId=${encodeURIComponent(parkId)}` : '';
    const searchQuery: string = search ? `&search=${encodeURIComponent(search)}` : '';
    return `park-items?page=${page}&size=${size}${parkIdQuery}${searchQuery}${buildParkItemAdminListQuery(filters)}`;
  },
  getParkItemById: (id: string) => `park-items/${id}`,
  createParkItem: 'park-items',
  updateParkItem: (id: string) => `park-items/${id}`,
  deleteParkItem: (id: string) => `park-items/${id}`,
  updateParkItemsBulkAdministration: 'park-items/bulk-administration'
};
