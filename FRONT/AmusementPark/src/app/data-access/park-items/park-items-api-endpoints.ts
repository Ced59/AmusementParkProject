export const PARK_ITEMS_API_ENDPOINTS = {
  getParkItemsByParkId: (parkId: string) => `park-items/park/${parkId}`,
  getParkItemsPaginated: (page: number, size: number, parkId?: string | null, search?: string | null) => {
    const parkIdQuery: string = parkId ? `&parkId=${encodeURIComponent(parkId)}` : '';
    const searchQuery: string = search ? `&search=${encodeURIComponent(search)}` : '';
    return `park-items?page=${page}&size=${size}${parkIdQuery}${searchQuery}`;
  },
  getParkItemById: (id: string) => `park-items/${id}`,
  createParkItem: 'park-items',
  updateParkItem: (id: string) => `park-items/${id}`,
  deleteParkItem: (id: string) => `park-items/${id}`
};
