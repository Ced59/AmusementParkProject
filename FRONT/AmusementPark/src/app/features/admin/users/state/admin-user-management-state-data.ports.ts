import { inject, InjectionToken } from '@angular/core';
import { UsersApiService } from '@data-access/users/users-api.service';
import { UserAdminApiService } from '@data-access/users/user-admin-api.service';

export interface AdminUserManagementStateUsersApiServicePort extends Pick<UsersApiService, 'getUserById'> {
}

export const ADMIN_USER_MANAGEMENT_STATE_USERS_API_SERVICE_PORT = new InjectionToken<AdminUserManagementStateUsersApiServicePort>('ADMIN_USER_MANAGEMENT_STATE_USERS_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(UsersApiService)
});

export interface AdminUserManagementStateUserAdminApiServicePort extends Pick<
  UserAdminApiService,
  'getParkDataEditorTokens' | 'revokeParkDataEditorToken' | 'revokeAllParkDataEditorTokens'
> {
}

export const ADMIN_USER_MANAGEMENT_STATE_USER_ADMIN_API_SERVICE_PORT = new InjectionToken<AdminUserManagementStateUserAdminApiServicePort>('ADMIN_USER_MANAGEMENT_STATE_USER_ADMIN_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(UserAdminApiService)
});
