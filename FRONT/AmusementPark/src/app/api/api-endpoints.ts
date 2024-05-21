export const API_ENDPOINTS = {

  postLogin: 'auth/login',


  getUsers: 'users/list',
  getUserById: (id: string) => `users/${id}`,
  // Ajoutez d'autres endpoints ici
};
