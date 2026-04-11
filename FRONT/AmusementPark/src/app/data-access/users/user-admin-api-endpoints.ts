export const USER_ADMIN_API_ENDPOINTS = {
  assignRoleToUser: (id: string) => `users/${id}/roles`,
  removeRoleFromUser: (id: string) => `users/${id}/roles`,
  lockUser: 'users/lock',
  unlockUser: 'users/unlock',
  changeUserPassword: (id: string) => `users/${id}/password`
};
