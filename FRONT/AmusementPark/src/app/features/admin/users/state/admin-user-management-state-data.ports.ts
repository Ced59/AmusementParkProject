import { inject, InjectionToken } from '@angular/core';
import { UsersApiService } from '@data-access/users/users-api.service';

export interface AdminUserManagementStateUsersApiServicePort extends Pick<UsersApiService, 'getUserById'> {
}

export const ADMIN_USER_MANAGEMENT_STATE_USERS_API_SERVICE_PORT = new InjectionToken<AdminUserManagementStateUsersApiServicePort>('ADMIN_USER_MANAGEMENT_STATE_USERS_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(UsersApiService)
});
