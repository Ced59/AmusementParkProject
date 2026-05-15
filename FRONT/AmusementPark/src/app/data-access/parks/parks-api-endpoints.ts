export const PARKS_API_ENDPOINTS = {
  getParksPaginated: (page: number, size: number) => `parks?page=${page}&size=${size}`,
  getRandomVisibleParks: (limit: number) => `parks/random-visible?limit=${limit}`,
  getParkById: (id: string) => `parks/${id}`,
  searchParks: (name: string, page: number, size: number) => `parks?page=${page}&size=${size}&name=${encodeURIComponent(name)}`,
  getParksByLocation: (latitude: number, longitude: number, radius: number) => `parks/geo-search?latitude=${latitude}&longitude=${longitude}&radius=${radius}`,
  updateParkVisibility: (id: string) => `parks/${id}/visibility`,
  createPark: 'parks',
  updatePark: (id: string) => `parks/${id}`,
  getParkExplorer: (parkId: string) => `park-zones/park/${parkId}/explorer`
};
