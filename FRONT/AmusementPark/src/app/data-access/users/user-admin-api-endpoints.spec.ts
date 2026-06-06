import { USER_ADMIN_API_ENDPOINTS } from './user-admin-api-endpoints';

describe('USER_ADMIN_API_ENDPOINTS', () => {
  it('encodes user ids in role endpoints', () => {
    expect(USER_ADMIN_API_ENDPOINTS.assignRoleToUser('user/1')).toBe('users/roles/assign/user%2F1');
    expect(USER_ADMIN_API_ENDPOINTS.removeRoleFromUser('user 1')).toBe('users/roles/remove/user%201');
  });

  it('encodes ids in password endpoints', () => {
    expect(USER_ADMIN_API_ENDPOINTS.changeUserPassword('user+1')).toBe('users/change-password?idUser=user%2B1');
  });
});
