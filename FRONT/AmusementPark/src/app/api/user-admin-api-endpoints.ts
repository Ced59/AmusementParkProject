export const USER_ADMIN_API_ENDPOINTS = {
  assignRoleToUser: (id: string) => `users/roles/assign/${id}`,
  removeRoleFromUser: (id: string) => `users/roles/remove/${id}`,
  lockUser: 'users/lock',
  unlockUser: 'users/unlock',
  changeUserPassword: (id: string) => `users/change-password?idUser=${id}`
};
