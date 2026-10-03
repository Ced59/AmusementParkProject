import { inject, InjectionToken } from '@angular/core';
import { UsersApiService } from '@data-access/users/users-api.service';

export interface AccountDeletionDataPort extends Pick<UsersApiService, 'deleteCurrentAccount'> {
}

export const ACCOUNT_DELETION_DATA_PORT = new InjectionToken<AccountDeletionDataPort>(
  'ACCOUNT_DELETION_DATA_PORT',
  {
    providedIn: 'root',
    factory: () => inject(UsersApiService)
  }
);
