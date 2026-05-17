export const PARKS_API_ENDPOINTS = {
  getParksPaginated: (page: number, size: number, visibleOnly: boolean = false) => `parks?page=${page}&size=${size}${visibleOnly ? '&visibleOnly=true' : ''}`,
  getRandomVisibleParks: (limit: number) => `parks/random-visible?limit=${limit}`,
  getVisibleParkMapPoints: (name: string | null = null) => `parks/map-visible${name ? `?name=${encodeURIComponent(name)}` : ''}`,
  getParkById: (id: string) => `parks/${id}`,
  searchParks: (name: string, page: number, size: number, visibleOnly: boolean = false) => `parks?page=${page}&size=${size}&name=${encodeURIComponent(name)}${visibleOnly ? '&visibleOnly=true' : ''}`,
  getParksByLocation: (latitude: number, longitude: number, radius: number) => `parks/geo-search?latitude=${latitude}&longitude=${longitude}&radius=${radius}`,
  updateParkVisibility: (id: string) => `parks/${id}/visibility`,
  createPark: 'parks',
  updatePark: (id: string) => `parks/${id}`,
  getParkExplorer: (parkId: string) => `park-zones/park/${parkId}/explorer`
};
