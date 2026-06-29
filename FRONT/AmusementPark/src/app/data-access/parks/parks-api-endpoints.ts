import { ParkRegionFilter } from '@shared/models/geo/world-region-filter.model';
import { AdminReviewStatus } from '@app/models/admin/admin-review-status';
import { ParkType } from '@app/models/parks/park-type';
import { ParkOpeningHoursAdminFilter } from '@app/models/parks/park-opening-hours';
import { ClosedEntityFilter, DEFAULT_CLOSED_ENTITY_FILTER } from '@app/models/shared/closed-entity-filter';


export interface ParkAdminListFilters {
  isVisible?: boolean | null;
  adminReviewStatus?: AdminReviewStatus | null;
  type?: ParkType | null;
  countryCode?: string | null;
  hasValidCoordinates?: boolean | null;
  openingHoursStatus?: ParkOpeningHoursAdminFilter | null;
}

export type ParkAdminListSortField = 'default' | 'name' | 'parkItemsTotalCount' | 'parkItemsVisibleCount' | 'openingHoursStatus';
export type ParkAdminListSortDirection = 'asc' | 'desc';

export interface ParkAdminListSort {
  sortBy?: ParkAdminListSortField | null;
  sortDirection?: ParkAdminListSortDirection | null;
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
  if (filters.openingHoursStatus && filters.openingHoursStatus !== 'all') {
    params.push(`openingHoursStatus=${encodeURIComponent(filters.openingHoursStatus)}`);
  }

  return params.length > 0 ? `&${params.join('&')}` : '';
}

function buildAdminSortQuery(sort: ParkAdminListSort | null = null): string {
  if (!sort?.sortBy || sort.sortBy === 'default') {
    return '';
  }

  const direction: ParkAdminListSortDirection = sort.sortDirection === 'desc' ? 'desc' : 'asc';
  return `&sortBy=${encodeURIComponent(sort.sortBy)}&sortDirection=${direction}`;
}

function buildRegionQuery(region: ParkRegionFilter | null = null): string {
  return region ? `&region=${encodeURIComponent(region)}` : '';
}

function buildClosedFilterQuery(closedFilter: ClosedEntityFilter | null | undefined, prefix: '?' | '&' = '&'): string {
  if (!closedFilter || closedFilter === DEFAULT_CLOSED_ENTITY_FILTER) {
    return '';
  }

  return `${prefix}closedFilter=${encodeURIComponent(closedFilter)}`;
}

export const PARKS_API_ENDPOINTS = {
  getParksPaginated: (page: number, size: number, visibleOnly: boolean = false, region: ParkRegionFilter | null = null, filters: ParkAdminListFilters | null = null, sort: ParkAdminListSort | null = null, closedFilter?: ClosedEntityFilter | null) =>
    `parks?page=${page}&size=${size}${visibleOnly ? '&visibleOnly=true' : ''}${buildRegionQuery(region)}${buildAdminListQuery(filters)}${buildAdminSortQuery(sort)}${buildClosedFilterQuery(closedFilter)}`,
  getRandomVisibleParks: (limit: number) => `parks/random-visible?limit=${limit}`,
  getVisibleParkMapPoints: (query: string | null = null, region: ParkRegionFilter | null = null, closedFilter?: ClosedEntityFilter | null) => {
    const params: string[] = [];
    if (query) {
      params.push(`query=${encodeURIComponent(query)}`);
    }
    if (region) {
      params.push(`region=${encodeURIComponent(region)}`);
    }
    if (closedFilter && closedFilter !== DEFAULT_CLOSED_ENTITY_FILTER) {
      params.push(`closedFilter=${encodeURIComponent(closedFilter)}`);
    }

    return `parks/map-visible${params.length > 0 ? `?${params.join('&')}` : ''}`;
  },
  getParkById: (id: string) => `parks/${id}`,
  getParkWeather: (id: string, days: number = 7) => `parks/${encodeURIComponent(id)}/weather?days=${days}`,
  getParkWeatherHistoricalComparisons: (id: string, days: number = 7, years: number = 10) => `parks/${encodeURIComponent(id)}/weather/historical-comparisons?days=${days}&years=${years}`,
  getParkOpeningHours: (id: string, from?: string | null, to?: string | null) => {
    const params: string[] = [];
    if (from) {
      params.push(`from=${encodeURIComponent(from)}`);
    }
    if (to) {
      params.push(`to=${encodeURIComponent(to)}`);
    }

    return `parks/${encodeURIComponent(id)}/opening-hours${params.length > 0 ? `?${params.join('&')}` : ''}`;
  },
  getAdminParkOpeningHours: (id: string) => `admin/parks/${encodeURIComponent(id)}/opening-hours`,
  upsertAdminParkOpeningHours: (id: string) => `admin/parks/${encodeURIComponent(id)}/opening-hours`,
  getParkDetailSummary: (id: string, closedFilter?: ClosedEntityFilter | null) => `parks/${id}/detail-summary${buildClosedFilterQuery(closedFilter, '?')}`,
  getParkMapItems: (id: string, closedFilter?: ClosedEntityFilter | null) => `parks/${id}/map-items${buildClosedFilterQuery(closedFilter, '?')}`,
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
  searchParks: (query: string, page: number, size: number, visibleOnly: boolean = false, region: ParkRegionFilter | null = null, filters: ParkAdminListFilters | null = null, sort: ParkAdminListSort | null = null, closedFilter?: ClosedEntityFilter | null) =>
    `parks?page=${page}&size=${size}&query=${encodeURIComponent(query)}${visibleOnly ? '&visibleOnly=true' : ''}${buildRegionQuery(region)}${buildAdminListQuery(filters)}${buildAdminSortQuery(sort)}${buildClosedFilterQuery(closedFilter)}`,
  getParksByLocation: (latitude: number, longitude: number, radiusMeters: number, closedFilter?: ClosedEntityFilter | null) => `parks/geo-search?latitude=${latitude}&longitude=${longitude}&radiusMeters=${radiusMeters}${buildClosedFilterQuery(closedFilter)}`,
  updateParkVisibility: (id: string) => `parks/${id}/visibility`,
  updateParksBulkAdministration: 'parks/bulk-administration',
  createPark: 'parks',
  updatePark: (id: string) => `parks/${id}`,
  getParkExplorer: (parkId: string, closedFilter?: ClosedEntityFilter | null) => `park-zones/park/${parkId}/explorer${buildClosedFilterQuery(closedFilter, '?')}`
};
