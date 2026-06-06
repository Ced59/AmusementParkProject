import { inject, InjectionToken } from '@angular/core';
import { AuthApiService } from '@data-access/auth/auth-api.service';

export interface ConfirmAccountPageStateAuthApiServicePort extends Pick<AuthApiService, 'confirmEmail'> {
}

export const CONFIRM_ACCOUNT_PAGE_STATE_AUTH_API_SERVICE_PORT = new InjectionToken<ConfirmAccountPageStateAuthApiServicePort>('CONFIRM_ACCOUNT_PAGE_STATE_AUTH_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(AuthApiService)
});
