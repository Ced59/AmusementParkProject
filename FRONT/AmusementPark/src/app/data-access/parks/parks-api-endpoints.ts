import { ParkRegionFilter } from '@shared/models/geo/world-region-filter.model';
import { AdminReviewStatus } from '@app/models/admin/admin-review-status';
import { ParkType } from '@app/models/parks/park-type';


export interface ParkAdminListFilters {
  isVisible?: boolean | null;
  adminReviewStatus?: AdminReviewStatus | null;
  type?: ParkType | null;
  countryCode?: string | null;
  hasValidCoordinates?: boolean | null;
}

function buildAdminListQuery(filters: ParkAdminListFilters | null = null): string {
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
  if (filters.type) {
    params.push(`type=${encodeURIComponent(filters.type)}`);
  }
  if (filters.countryCode?.trim()) {
    params.push(`countryCode=${encodeURIComponent(filters.countryCode.trim())}`);
  }
  if (filters.hasValidCoordinates !== null && filters.hasValidCoordinates !== undefined) {
    params.push(`hasValidCoordinates=${filters.hasValidCoordinates}`);
  }

  return params.length > 0 ? `&${params.join('&')}` : '';
}

function buildRegionQuery(region: ParkRegionFilter | null = null): string {
  return region ? `&region=${encodeURIComponent(region)}` : '';
}

export const PARKS_API_ENDPOINTS = {
  getParksPaginated: (page: number, size: number, visibleOnly: boolean = false, region: ParkRegionFilter | null = null, filters: ParkAdminListFilters | null = null) =>
    `parks?page=${page}&size=${size}${visibleOnly ? '&visibleOnly=true' : ''}${buildRegionQuery(region)}${buildAdminListQuery(filters)}`,
  getRandomVisibleParks: (limit: number) => `parks/random-visible?limit=${limit}`,
  getVisibleParkMapPoints: (query: string | null = null, region: ParkRegionFilter | null = null) => {
    const params: string[] = [];
    if (query) {
      params.push(`query=${encodeURIComponent(query)}`);
    }
    if (region) {
      params.push(`region=${encodeURIComponent(region)}`);
    }

    return `parks/map-visible${params.length > 0 ? `?${params.join('&')}` : ''}`;
  },
  getParkById: (id: string) => `parks/${id}`,
  getParkDetailSummary: (id: string) => `parks/${id}/detail-summary`,
  getParkMapItems: (id: string) => `parks/${id}/map-items`,
  getParkDistances: (sourceParkId: string, targetParkIds: string[]) => {
    const params: string = targetParkIds
      .map((targetParkId: string) => `targetParkIds=${encodeURIComponent(targetParkId)}`)
      .join('&');

    return `parks/${encodeURIComponent(sourceParkId)}/distances${params ? `?${params}` : ''}`;
  },
  getNearestParks: (sourceParkId: string, limit: number = 4, maxDistanceKilometers: number | null = null) => {
    const params: string[] = [`limit=${limit}`];
    if (maxDistanceKilometers !== null && Number.isFinite(maxDistanceKilometers)) {
      params.push(`maxDistanceKilometers=${maxDistanceKilometers}`);
    }

    return `parks/${encodeURIComponent(sourceParkId)}/nearby?${params.join('&')}`;
  },
  searchParks: (query: string, page: number, size: number, visibleOnly: boolean = false, region: ParkRegionFilter | null = null, filters: ParkAdminListFilters | null = null) =>
    `parks?page=${page}&size=${size}&query=${encodeURIComponent(query)}${visibleOnly ? '&visibleOnly=true' : ''}${buildRegionQuery(region)}${buildAdminListQuery(filters)}`,
  getParksByLocation: (latitude: number, longitude: number, radiusMeters: number) => `parks/geo-search?latitude=${latitude}&longitude=${longitude}&radiusMeters=${radiusMeters}`,
  updateParkVisibility: (id: string) => `parks/${id}/visibility`,
  updateParksBulkAdministration: 'parks/bulk-administration',
  createPark: 'parks',
  updatePark: (id: string) => `parks/${id}`,
  getParkExplorer: (parkId: string) => `park-zones/park/${parkId}/explorer`
};
