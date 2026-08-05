export const USER_ADMIN_API_ENDPOINTS = {
  assignRoleToUser: (id: string) => `users/roles/assign/${encodeURIComponent(id)}`,
  removeRoleFromUser: (id: string) => `users/roles/remove/${encodeURIComponent(id)}`,
  lockUser: 'users/lock',
  unlockUser: 'users/unlock',
  changeUserPassword: (id: string) => `users/change-password?idUser=${encodeURIComponent(id)}`,
  parkDataEditorTokens: (id: string) => `admin/users/${encodeURIComponent(id)}/park-data-editor-tokens`,
  parkDataEditorToken: (userId: string, tokenId: string) =>
    `admin/users/${encodeURIComponent(userId)}/park-data-editor-tokens/${encodeURIComponent(tokenId)}`
};
