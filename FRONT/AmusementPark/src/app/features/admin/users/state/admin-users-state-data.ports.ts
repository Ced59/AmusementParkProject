import { inject, InjectionToken } from '@angular/core';
import { UsersApiService } from '@data-access/users/users-api.service';

export interface AdminUsersStateUsersApiServicePort extends Pick<UsersApiService, 'getUsers'> {
}

export const ADMIN_USERS_STATE_USERS_API_SERVICE_PORT = new InjectionToken<AdminUsersStateUsersApiServicePort>('ADMIN_USERS_STATE_USERS_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(UsersApiService)
});
