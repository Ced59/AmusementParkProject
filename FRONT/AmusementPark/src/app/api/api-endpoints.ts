export const API_ENDPOINTS = {
  postLogin: 'auth/login',
  postRegister: 'users',
  confirmEmail: 'users/confirm-email',
  resendConfirmation: 'users/resend-confirmation',
  forgotPassword: 'users/forgot-password',
  resetPassword: 'users/reset-password',
  externalLogin: (provider: string) => `auth/external/${provider}`,

  getUsers: (page: number, size: number) => `users?page=${page}&size=${size}`,
  getUserById: (id: string) => `users/${id}`,
  putUserById: (id: string | null) => `users/${id}`,

  getParksPaginated: (page: number, size: number) => `parks?page=${page}&size=${size}`,
  getParkById: (id: string) => `parks/${id}`,

  searchParks: (name: string, page: number, size: number) =>
    `parks?page=${page}&size=${size}&name=${encodeURIComponent(name)}`,

  getParksByLocation: (latitude: number, longitude: number, radius: number) =>
    `parks/geo-search?latitude=${latitude}&longitude=${longitude}&radius=${radius}`,

  updateParkVisibility: (id: string) => `parks/${id}/visibility`,

  createPark: 'parks',
  updatePark: (id: string) => `parks/${id}`,

  getParkFounders: 'park-founders',
  getParkFounderById: (id: string) => `park-founders/${id}`,
  createParkFounder: 'park-founders',
  updateParkFounder: (id: string) => `park-founders/${id}`,

  getParkOperators: 'park-operators',
  getAttractionManufacturers: 'attraction-manufacturers',

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
    return `park-items?page=${page}&size=${size}${parkIdQuery}${searchQuery}`;
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
  getAdminImages: 'images',
  getAdminImageById: (id: string) => `images/${id}/metadata`,
  updateAdminImage: (id: string) => `images/${id}/metadata`,
  getAdminImageTags: 'images/tags',
  createAdminImageTag: 'images/tags',
  updateAdminImageTag: (id: string) => `images/tags/${id}`
};
