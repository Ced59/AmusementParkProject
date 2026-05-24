import { HttpBackend, HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { Inject, Injectable, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { Observable, catchError, finalize, firstValueFrom, map, of, shareReplay } from 'rxjs';
import { jwtDecode } from 'jwt-decode';

import { environment } from '../../../environments/environment';
import { AUTH_API_ENDPOINTS } from '@data-access/auth/auth-api-endpoints';
import { RefreshTokenResponse } from '@data-access/auth/models/api/refresh-token-response.model';
import { JwtPayload } from '@app/models/users/jwt_payload';
import { UserToken } from '@app/models/users/user_token';
import { GoogleIdentityService } from './google-identity.service';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private static readonly RefreshSessionMarkerKey = 'amusementpark.hasRefreshSession';

  private readonly rawHttpClient: HttpClient;
  private refreshAccessTokenRequest$: Observable<string | null> | null = null;
  private accessToken: string | null = null;
  private hasServerRefreshSession: boolean = false;
  private hasInitializedSession: boolean = false;

  constructor(
    private readonly googleIdentityService: GoogleIdentityService,
    @Inject(PLATFORM_ID) private readonly platformId: object,
    httpBackend: HttpBackend
  ) {
    this.rawHttpClient = new HttpClient(httpBackend);
  }

  async initializeSession(): Promise<void> {
    if (!isPlatformBrowser(this.platformId) || this.hasInitializedSession) {
      this.hasInitializedSession = true;
      return;
    }

    if (!this.hasPersistedRefreshSessionMarker()) {
      this.clearSession(false);
      this.hasInitializedSession = true;
      return;
    }

    try {
      await firstValueFrom(this.ensureValidAccessToken(true));
    } catch (_error) {
      this.clearSession(false);
    } finally {
      this.hasInitializedSession = true;
    }
  }

  getToken(): string | null {
    return this.accessToken;
  }

  setToken(token: string | null): void {
    this.accessToken = token;
  }

  setAuthenticatedSession(result: UserToken | RefreshTokenResponse): void {
    const accessToken: string | undefined = 'accessToken' in result ? result.accessToken : result.token;
    if (!accessToken) {
      return;
    }

    this.setToken(accessToken);
    this.hasServerRefreshSession = true;
    this.hasInitializedSession = true;
    this.persistRefreshSessionMarker();
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
    return this.hasValidAccessToken();
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

  ensureValidAccessToken(forceRefreshAttempt: boolean = false): Observable<string | null> {
    const token: string | null = this.getToken();
    if (this.hasValidAccessToken() && token) {
      return of(token);
    }

    if (!isPlatformBrowser(this.platformId)) {
      return of(null);
    }

    if (!forceRefreshAttempt && !this.hasServerRefreshSession) {
      return of(null);
    }

    if (this.refreshAccessTokenRequest$) {
      return this.refreshAccessTokenRequest$;
    }

    const url: string = `${environment.apiBaseUrl}${AUTH_API_ENDPOINTS.refreshToken}`;
    const options = {
      headers: new HttpHeaders({
        'Content-Type': 'application/json'
      }),
      withCredentials: true
    };

    this.refreshAccessTokenRequest$ = this.rawHttpClient
      .post<RefreshTokenResponse>(url, {}, options)
      .pipe(
        map((response: RefreshTokenResponse) => {
          this.setAuthenticatedSession(response);
          return response.accessToken;
        }),
        catchError((error: unknown) => {
          if (!this.isExpectedRefreshTokenFailure(error)) {
            console.error('Error refreshing access token:', error);
          }

          this.clearSession(false);
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
    const shouldNotifyServer: boolean = isPlatformBrowser(this.platformId) && this.hasServerRefreshSession;

    this.clearSession(true);

    if (shouldNotifyServer) {
      const url: string = `${environment.apiBaseUrl}${AUTH_API_ENDPOINTS.logout}`;
      const options = {
        headers: new HttpHeaders({
          'Content-Type': 'application/json'
        }),
        withCredentials: true
      };

      void firstValueFrom(this.rawHttpClient.post<void>(url, {}, options))
        .catch((error: unknown): void => {
          console.error('Error logging out from server session:', error);
        });
    }
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

  private clearSession(disableAutoSelect: boolean): void {
    this.accessToken = null;
    this.hasServerRefreshSession = false;
    this.clearRefreshSessionMarker();

    if (disableAutoSelect) {
      this.googleIdentityService.disableAutoSelect();
    }
  }

  private hasPersistedRefreshSessionMarker(): boolean {
    if (!isPlatformBrowser(this.platformId)) {
      return false;
    }

    try {
      return localStorage.getItem(AuthService.RefreshSessionMarkerKey) === 'true';
    } catch (_error) {
      return false;
    }
  }

  private persistRefreshSessionMarker(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    try {
      localStorage.setItem(AuthService.RefreshSessionMarkerKey, 'true');
    } catch (_error) {
      // Non-critical: the HttpOnly refresh cookie remains the source of truth.
    }
  }

  private clearRefreshSessionMarker(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    try {
      localStorage.removeItem(AuthService.RefreshSessionMarkerKey);
    } catch (_error) {
      // Non-critical: browser storage may be unavailable in privacy modes.
    }
  }

  private isExpectedRefreshTokenFailure(error: unknown): boolean {
    return error instanceof HttpErrorResponse
      && (error.status === 400 || error.status === 401 || error.status === 403);
  }
}
