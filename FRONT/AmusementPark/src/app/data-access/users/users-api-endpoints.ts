export const USERS_API_ENDPOINTS = {
  getUsers: (page: number, size: number) => `users?page=${page}&size=${size}`,
  getUserById: (id: string) => `users/${id}`,
  putUserById: (id: string | null) => `users/${id}`
};
