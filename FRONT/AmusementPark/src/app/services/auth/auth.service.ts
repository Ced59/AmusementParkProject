import { Injectable } from '@angular/core';
import { jwtDecode } from 'jwt-decode';
import { JwtPayload } from '../../models/users/jwt_payload';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  constructor() {}

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
    const token = this.getToken();
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
    const decoded = this.getTokenDecoded();
    return !!(decoded && decoded.exp && Date.now() < decoded.exp * 1000);
  }

  getUserIdFromToken(): string | null {
    const decoded = this.getTokenDecoded();
    if (decoded) {
      return decoded.sub;
    } else {
      return null;
    }
  }

  logout(): void {
    localStorage.removeItem('auth_token');
  }

  hasRole(expectedRole: string): boolean {
    const decoded = this.getTokenDecoded();
    if (!decoded) {
      return false;
    }

    const possibleClaims = [
      'role',
      'roles',
      'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'
    ];

    let roles: any = null;

    for (const key of possibleClaims) {
      const value = (decoded as any)[key];
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
      // "ADMIN", ou "ADMIN,USER", ou "ADMIN USER"
      return roles.split(/[ ,]/).includes(expectedRole);
    }

    return false;
  }
}
