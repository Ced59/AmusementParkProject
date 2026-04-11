import { HttpBackend, HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, catchError, finalize, map, of, shareReplay } from 'rxjs';
import { jwtDecode } from 'jwt-decode';

import { environment } from '../../../environments/environment';
import { AUTH_API_ENDPOINTS } from '@data-access/auth/auth-api-endpoints';
import { RefreshTokenResponse } from '@data-access/auth/models/api/refresh-token-response.model';
import { JwtPayload } from '../../models/users/jwt_payload';
import { UserToken } from '../../models/users/user_token';
import { GoogleIdentityService } from './google-identity.service';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly accessTokenStorageKey: string = 'auth_token';
  private readonly refreshTokenStorageKey: string = 'refresh_token';
  private readonly refreshTokenExpiresAtStorageKey: string = 'refresh_token_expires_at';
  private readonly rawHttpClient: HttpClient;
  private refreshAccessTokenRequest$: Observable<string | null> | null = null;

  constructor(
    private readonly googleIdentityService: GoogleIdentityService,
    httpBackend: HttpBackend
  ) {
    this.rawHttpClient = new HttpClient(httpBackend);
  }

  getToken(): string | null {
    if (typeof window !== 'undefined') {
      return localStorage.getItem(this.accessTokenStorageKey);
    }

    return null;
  }

  getRefreshToken(): string | null {
    if (typeof window !== 'undefined') {
      return localStorage.getItem(this.refreshTokenStorageKey);
    }

    return null;
  }

  setToken(token: string): void {
    if (typeof window === 'undefined') {
      return;
    }

    localStorage.setItem(this.accessTokenStorageKey, token);
  }

  setAuthenticatedSession(result: UserToken | RefreshTokenResponse): void {
    const accessToken: string | undefined = 'accessToken' in result ? result.accessToken : result.token;
    if (!accessToken) {
      return;
    }

    this.setToken(accessToken);

    if (typeof window === 'undefined') {
      return;
    }

    if (result.refreshToken) {
      localStorage.setItem(this.refreshTokenStorageKey, result.refreshToken);
    } else {
      localStorage.removeItem(this.refreshTokenStorageKey);
    }

    if (result.refreshTokenExpiresAtUtc) {
      localStorage.setItem(this.refreshTokenExpiresAtStorageKey, result.refreshTokenExpiresAtUtc);
    } else {
      localStorage.removeItem(this.refreshTokenExpiresAtStorageKey);
    }
  }

  getTokenDecoded(): JwtPayload | null {
    const token: string | null = this.getToken();
    if (token) {
      try {
        return jwtDecode<JwtPayload>(token);
      } catch (error) {
        console.error('Error decoding token:', error);
        return null;
      }
    }

    return null;
  }

  isLoggedIn(): boolean {
    return this.hasValidAccessToken() || this.hasUsableRefreshToken();
  }

  getUserIdFromToken(): string | null {
    if (!this.isLoggedIn()) {
      return null;
    }

    const decoded: JwtPayload | null = this.getTokenDecoded();
    if (decoded) {
      return decoded.sub;
    }

    return null;
  }

  ensureValidAccessToken(): Observable<string | null> {
    const token: string | null = this.getToken();
    if (this.hasValidAccessToken() && token) {
      return of(token);
    }

    if (!this.hasUsableRefreshToken()) {
      return of(null);
    }

    if (this.refreshAccessTokenRequest$) {
      return this.refreshAccessTokenRequest$;
    }

    const refreshToken: string | null = this.getRefreshToken();
    if (!refreshToken) {
      return of(null);
    }

    const url: string = `${environment.apiBaseUrl}${AUTH_API_ENDPOINTS.refreshToken}`;
    const options = {
      headers: new HttpHeaders({
        'Content-Type': 'application/json'
      })
    };

    this.refreshAccessTokenRequest$ = this.rawHttpClient
      .post<RefreshTokenResponse>(url, { refreshToken }, options)
      .pipe(
        map((response: RefreshTokenResponse) => {
          this.setAuthenticatedSession(response);
          return response.accessToken;
        }),
        catchError((error: unknown) => {
          console.error('Error refreshing access token:', error);
          this.logout();
          return of(null);
        }),
        finalize(() => {
          this.refreshAccessTokenRequest$ = null;
        }),
        shareReplay(1)
      );

    return this.refreshAccessTokenRequest$;
  }

  logout(): void {
    if (typeof window !== 'undefined') {
      localStorage.removeItem(this.accessTokenStorageKey);
      localStorage.removeItem(this.refreshTokenStorageKey);
      localStorage.removeItem(this.refreshTokenExpiresAtStorageKey);
    }

    this.googleIdentityService.disableAutoSelect();
  }

  hasRole(expectedRole: string): boolean {
    if (!this.isLoggedIn()) {
      return false;
    }

    const decoded: JwtPayload | null = this.getTokenDecoded();
    if (!decoded) {
      return false;
    }

    const possibleClaims: string[] = [
      'role',
      'roles',
      'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'
    ];

    let roles: unknown = null;

    for (const key of possibleClaims) {
      const value: unknown = (decoded as Record<string, unknown>)[key];
      if (value) {
        roles = value;
        break;
      }
    }

    if (!roles) {
      return false;
    }

    if (Array.isArray(roles)) {
      return roles.includes(expectedRole);
    }

    if (typeof roles === 'string') {
      return roles.split(/[ ,]/).includes(expectedRole);
    }

    return false;
  }

  private hasValidAccessToken(): boolean {
    const decoded: JwtPayload | null = this.getTokenDecoded();
    return !!(decoded && decoded.exp && Date.now() < decoded.exp * 1000);
  }

  private hasUsableRefreshToken(): boolean {
    const refreshToken: string | null = this.getRefreshToken();
    if (!refreshToken) {
      return false;
    }

    const refreshTokenExpiresAtUtc: string | null = typeof window !== 'undefined'
      ? localStorage.getItem(this.refreshTokenExpiresAtStorageKey)
      : null;

    if (!refreshTokenExpiresAtUtc) {
      return true;
    }

    const expiration: number = Date.parse(refreshTokenExpiresAtUtc);
    if (Number.isNaN(expiration)) {
      return true;
    }

    return Date.now() < expiration;
  }
}
