import { inject, InjectionToken } from '@angular/core';
import { AuthApiService } from '@data-access/auth/auth-api.service';

export interface ForgotPasswordPageStateAuthApiServicePort extends Pick<AuthApiService, 'forgotPassword'> {
}

export const FORGOT_PASSWORD_PAGE_STATE_AUTH_API_SERVICE_PORT = new InjectionToken<ForgotPasswordPageStateAuthApiServicePort>('FORGOT_PASSWORD_PAGE_STATE_AUTH_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(AuthApiService)
});
