export const PARK_ZONES_API_ENDPOINTS = {
  getParkZonesByParkId: (parkId: string) => `park-zones/park/${parkId}`,
  getParkZoneById: (id: string) => `park-zones/${id}`,
  createParkZone: 'park-zones',
  updateParkZone: (id: string) => `park-zones/${id}`,
  deleteParkZone: (id: string) => `park-zones/${id}`
};
