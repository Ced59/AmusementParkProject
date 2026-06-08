export const PARK_ZONES_API_ENDPOINTS = {
  getParkZonesByParkId: (parkId: string, page: number = 1, size: number = 100) => `park-zones/park/${parkId}?page=${page}&size=${size}`,
  getParkZoneById: (id: string) => `park-zones/${id}`,
  createParkZone: 'park-zones',
  updateParkZone: (id: string) => `park-zones/${id}`,
  deleteParkZone: (id: string) => `park-zones/${id}`
};
