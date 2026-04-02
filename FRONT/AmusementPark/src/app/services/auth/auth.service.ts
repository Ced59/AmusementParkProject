import { Injectable } from '@angular/core';
import { jwtDecode } from 'jwt-decode';
import { JwtPayload } from '../../models/users/jwt_payload';
import { GoogleIdentityService } from './google-identity.service';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  constructor(private readonly googleIdentityService: GoogleIdentityService) {
  }

  getToken(): string | null {
    if (typeof window !== 'undefined') {
      return localStorage.getItem('auth_token');
    }

    return null;
  }

  setToken(token: string): void {
    localStorage.setItem('auth_token', token);
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
    const decoded: JwtPayload | null = this.getTokenDecoded();
    return !!(decoded && decoded.exp && Date.now() < decoded.exp * 1000);
  }

  getUserIdFromToken(): string | null {
    const decoded: JwtPayload | null = this.getTokenDecoded();
    if (decoded) {
      return decoded.sub;
    }

    return null;
  }

  logout(): void {
    localStorage.removeItem('auth_token');
    this.googleIdentityService.disableAutoSelect();
  }

  hasRole(expectedRole: string): boolean {
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
}
