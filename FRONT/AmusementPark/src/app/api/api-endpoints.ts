export const API_ENDPOINTS = {

  postLogin: 'auth/login',
  googleLogin: `auth/google-response`,


  getUsers: 'users/list',
  getUserById: (id: string) => `users?Id=${id}`,
  putUserById: (id: string | null) => `users/${id}`,

  getParksPaginated: (page: number, size: number) => `parks/list?page=${page}&size=${size}`,
  getParkById: (id: string) => `parks?id=${id}`,

  searchParks: (name: string, page: number, size: number) =>
    `parks/search?name=${encodeURIComponent(name)}&page=${page}&size=${size}`,

  updateParkVisibility: (id: string) => `parks/${id}/visibility`,

  getSearch: (query: string, categories: string[], page: number, size: number) => {
    const catsParam = categories && categories.length > 0
      ? `&categories=${categories.join(',')}`
      : '';
    return `search?query=${encodeURIComponent(query)}${catsParam}&page=${page}&pageSize=${size}`;
  },
};
