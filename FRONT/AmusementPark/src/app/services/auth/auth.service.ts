// src/app/services/auth.service.ts

import {Injectable} from '@angular/core';
import {jwtDecode} from 'jwt-decode';
import {JwtPayload} from "../../models/users/jwt_payload";

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  constructor() { }

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
    const token = this.getToken();
    const decoded = this.getTokenDecoded();
    return !!(decoded && Date.now() < decoded.exp * 1000);

  }

  logout(): void {
    localStorage.removeItem('auth_token');
  }
}
