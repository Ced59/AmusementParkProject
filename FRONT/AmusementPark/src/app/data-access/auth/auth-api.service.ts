import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AuthMessageResponse } from '@app/models/auth/auth-message-response';
import { UserCredentials } from '@app/models/users/user_credentials';
import { UserDto } from '@app/models/users/user_dto';
import { UserRegister } from '@app/models/users/user-register';
import { UserToken } from '@app/models/users/user_token';
import { MeasurementSystem } from '@shared/models/measurements/measurement-system.model';
import { AUTH_API_ENDPOINTS } from './auth-api-endpoints';
import { RefreshTokenResponse } from './models/api/refresh-token-response.model';

@Injectable({
  providedIn: 'root'
})
export class AuthApiService {
  private readonly jsonHttpOptions = {
    headers: new HttpHeaders({
      'Content-Type': 'application/json'
    })
  };

  private readonly credentialedJsonHttpOptions = {
    ...this.jsonHttpOptions,
    withCredentials: true
  };

  constructor(private readonly http: HttpClient) {
  }

  login(credentials: UserCredentials): Observable<UserToken> {
    const url: string = `${environment.apiBaseUrl}${AUTH_API_ENDPOINTS.login}`;
    return this.http.post<UserToken>(url, JSON.stringify(credentials), this.credentialedJsonHttpOptions);
  }

  refreshToken(): Observable<RefreshTokenResponse> {
    const url: string = `${environment.apiBaseUrl}${AUTH_API_ENDPOINTS.refreshToken}`;
    return this.http.post<RefreshTokenResponse>(url, {}, this.credentialedJsonHttpOptions);
  }

  logout(): Observable<void> {
    const url: string = `${environment.apiBaseUrl}${AUTH_API_ENDPOINTS.logout}`;
    return this.http.post<void>(url, {}, this.credentialedJsonHttpOptions);
  }

  register(request: UserRegister): Observable<UserDto> {
    const url: string = `${environment.apiBaseUrl}${AUTH_API_ENDPOINTS.register}`;
    return this.http.post<UserDto>(url, request, this.jsonHttpOptions);
  }

  confirmEmail(token: string): Observable<AuthMessageResponse> {
    const url: string = `${environment.apiBaseUrl}${AUTH_API_ENDPOINTS.confirmEmail}`;
    return this.http.post<AuthMessageResponse>(url, { token }, this.jsonHttpOptions);
  }

  resendConfirmation(email: string): Observable<AuthMessageResponse> {
    const url: string = `${environment.apiBaseUrl}${AUTH_API_ENDPOINTS.resendConfirmation}`;
    return this.http.post<AuthMessageResponse>(url, { email }, this.jsonHttpOptions);
  }

  forgotPassword(email: string): Observable<AuthMessageResponse> {
    const url: string = `${environment.apiBaseUrl}${AUTH_API_ENDPOINTS.forgotPassword}`;
    return this.http.post<AuthMessageResponse>(url, { email }, this.jsonHttpOptions);
  }

  resetPassword(token: string, newPassword: string, newPasswordConfirm: string): Observable<AuthMessageResponse> {
    const url: string = `${environment.apiBaseUrl}${AUTH_API_ENDPOINTS.resetPassword}`;
    return this.http.post<AuthMessageResponse>(url, { token, newPassword, newPasswordConfirm }, this.jsonHttpOptions);
  }

  externalLogin(provider: string, token: string, nonce?: string, preferredMeasurementSystem?: MeasurementSystem): Observable<UserToken> {
    const url: string = `${environment.apiBaseUrl}${AUTH_API_ENDPOINTS.externalLogin(provider)}`;
    const body: { token: string; nonce?: string; preferredMeasurementSystem?: MeasurementSystem } = {
      token
    };

    if (nonce) {
      body.nonce = nonce;
    }

    if (preferredMeasurementSystem) {
      body.preferredMeasurementSystem = preferredMeasurementSystem;
    }

    return this.http.post<UserToken>(url, body, this.credentialedJsonHttpOptions);
  }

  googleLogin(token: string): Observable<UserToken> {
    return this.externalLogin('google', token);
  }

  getCurrentUserById(id: string): Observable<UserDto> {
    const url: string = `${environment.apiBaseUrl}${AUTH_API_ENDPOINTS.getCurrentUserById(id)}`;
    return this.http.get<UserDto>(url);
  }
}
