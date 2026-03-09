export const API_ENDPOINTS = {
  postLogin: 'auth/login',
  googleLogin: 'auth/google-response',

  getUsers: 'users/list',
  getUserById: (id: string) => `users?Id=${id}`,
  putUserById: (id: string | null) => `users/${id}`,

  getParksPaginated: (page: number, size: number) => `parks/list?page=${page}&size=${size}`,
  getParkById: (id: string) => `parks?id=${id}`,

  searchParks: (name: string, page: number, size: number) =>
    `parks/search?name=${encodeURIComponent(name)}&page=${page}&size=${size}`,

  updateParkVisibility: (id: string) => `parks/${id}/visibility`,

  createPark: 'parks',
  updatePark: (id: string) => `parks/${id}`,

  getParkFounders: 'park-founders/list',
  getParkFounderById: (id: string) => `park-founders/${id}`,
  createParkFounder: 'park-founders',
  updateParkFounder: (id: string) => `park-founders/${id}`,

  getParkOperators: 'park-operators/list',
  getAttractionManufacturers: 'attraction-manufacturers/list',

  getParkZonesByParkId: (parkId: string) => `park-zones/park/${parkId}`,
  getParkZoneById: (id: string) => `park-zones/${id}`,
  createParkZone: 'park-zones',
  updateParkZone: (id: string) => `park-zones/${id}`,
  deleteParkZone: (id: string) => `park-zones/${id}`,
  getParkExplorer: (parkId: string) => `park-zones/park/${parkId}/explorer`,

  getParkItemsByParkId: (parkId: string) => `park-items/park/${parkId}`,
  getParkItemsPaginated: (page: number, size: number, parkId?: string | null, search?: string | null) => {
    const parkIdQuery: string = parkId ? `&parkId=${encodeURIComponent(parkId)}` : '';
    const searchQuery: string = search ? `&search=${encodeURIComponent(search)}` : '';
    return `park-items/list?page=${page}&size=${size}${parkIdQuery}${searchQuery}`;
  },
  getParkItemById: (id: string) => `park-items/${id}`,
  createParkItem: 'park-items',
  updateParkItem: (id: string) => `park-items/${id}`,
  deleteParkItem: (id: string) => `park-items/${id}`,
  getParkOperatorById: (id: string) => `park-operators/${id}`,
  createParkOperator: 'park-operators',
  updateParkOperator: (id: string) => `park-operators/${id}`,
  getAttractionManufacturerById: (id: string) => `attraction-manufacturers/${id}`,
  createAttractionManufacturer: 'attraction-manufacturers',
  updateAttractionManufacturer: (id: string) => `attraction-manufacturers/${id}`,

  getSearch: (query: string, categories: string[], page: number, size: number) => {
    const catsParam = categories && categories.length > 0
      ? `&categories=${categories.join(',')}`
      : '';
    return `search?query=${encodeURIComponent(query)}${catsParam}&page=${page}&pageSize=${size}`;
  },

  getCountries: (lang: string) => `countries?lang=${lang}`,

  uploadImage: 'images',
  linkImage: 'images/links',
  getImages: (ownerType: string, ownerId: string, category: string) =>
    `images/${ownerType}/${ownerId}/${category}`,
  getCurrentImage: (ownerType: string, ownerId: string, category: string) =>
    `images/${ownerType}/${ownerId}/${category}/current`,
  setCurrentImage: (imageId: string) => `images/${imageId}/current`,
  deleteImage: (imageId: string) => `images/${imageId}`,
};
