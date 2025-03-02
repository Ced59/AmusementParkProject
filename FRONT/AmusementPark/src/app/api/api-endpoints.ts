export const API_ENDPOINTS = {

  postLogin: 'auth/login',
  googleLogin: `auth/google-response`,


  getUsers: 'users/list',
  getUserById: (id: string) => `users?Id=${id}`,
  putUserById: (id: string | null) => `users/${id}`,

  getParksPaginated: (page: number, size: number) => `parks/list?page=${page}&size=${size}`,
  getParkById: (id: string) => `parks?id=${id}`,
};
