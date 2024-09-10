export const API_ENDPOINTS = {

  postLogin: 'auth/login',


  getUsers: 'users/list',
  getUserById: (id: string) => `users?Id=${id}`,

  getParksPaginated: (page: number, size: number) => `parks/list?page=${page}&size=${size}`
};
