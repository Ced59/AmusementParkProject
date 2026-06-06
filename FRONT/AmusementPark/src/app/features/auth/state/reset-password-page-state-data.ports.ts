import { inject, InjectionToken } from '@angular/core';
import { AuthApiService } from '@data-access/auth/auth-api.service';

export interface ResetPasswordPageStateAuthApiServicePort extends Pick<AuthApiService, 'resetPassword'> {
}

export const RESET_PASSWORD_PAGE_STATE_AUTH_API_SERVICE_PORT = new InjectionToken<ResetPasswordPageStateAuthApiServicePort>('RESET_PASSWORD_PAGE_STATE_AUTH_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(AuthApiService)
});
