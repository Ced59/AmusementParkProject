export const USER_ADMIN_API_ENDPOINTS = {
  assignRoleToUser: (id: string) => `users/roles/assign/${encodeURIComponent(id)}`,
  removeRoleFromUser: (id: string) => `users/roles/remove/${encodeURIComponent(id)}`,
  lockUser: 'users/lock',
  unlockUser: 'users/unlock',
  changeUserPassword: (id: string) => `users/change-password?idUser=${encodeURIComponent(id)}`
};
