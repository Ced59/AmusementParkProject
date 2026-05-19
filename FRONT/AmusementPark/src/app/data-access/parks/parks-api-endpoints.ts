import { ParkRegionFilter } from '@shared/models/geo/world-region-filter.model';

function buildRegionQuery(region: ParkRegionFilter | null = null): string {
  return region ? `&region=${encodeURIComponent(region)}` : '';
}

export const PARKS_API_ENDPOINTS = {
  getParksPaginated: (page: number, size: number, visibleOnly: boolean = false, region: ParkRegionFilter | null = null) =>
    `parks?page=${page}&size=${size}${visibleOnly ? '&visibleOnly=true' : ''}${buildRegionQuery(region)}`,
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
  searchParks: (query: string, page: number, size: number, visibleOnly: boolean = false, region: ParkRegionFilter | null = null) =>
    `parks?page=${page}&size=${size}&query=${encodeURIComponent(query)}${visibleOnly ? '&visibleOnly=true' : ''}${buildRegionQuery(region)}`,
  getParksByLocation: (latitude: number, longitude: number, radiusMeters: number) => `parks/geo-search?latitude=${latitude}&longitude=${longitude}&radiusMeters=${radiusMeters}`,
  updateParkVisibility: (id: string) => `parks/${id}/visibility`,
  createPark: 'parks',
  updatePark: (id: string) => `parks/${id}`,
  getParkExplorer: (parkId: string) => `park-zones/park/${parkId}/explorer`
};
