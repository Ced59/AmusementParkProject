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
