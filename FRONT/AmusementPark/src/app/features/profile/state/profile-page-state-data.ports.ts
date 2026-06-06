import { inject, InjectionToken } from '@angular/core';
import { UsersApiService } from '@data-access/users/users-api.service';

export interface ProfilePageStateUsersApiServicePort extends Pick<UsersApiService, 'getUserById'> {
}

export const PROFILE_PAGE_STATE_USERS_API_SERVICE_PORT = new InjectionToken<ProfilePageStateUsersApiServicePort>('PROFILE_PAGE_STATE_USERS_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(UsersApiService)
});
