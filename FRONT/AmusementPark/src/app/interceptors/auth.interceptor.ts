import { Injectable } from '@angular/core';
import {
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest
} from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from '../services/auth/auth.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {

  constructor(private authService: AuthService) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    if (
      typeof window === 'undefined' ||
      req.url.endsWith('/login') ||
      req.url.includes('google-response')
    ) {
      return next.handle(req);
    }

    const token = this.authService.getToken();
    if (!token) {
      return next.handle(req);
    }

    const decoded = this.authService.getTokenDecoded();
    if (!decoded || !decoded.exp || Date.now() >= decoded.exp * 1000) {
      // éventuellement : this.authService.logout();
      return next.handle(req);
    }

    const authReq = req.clone({
      headers: req.headers.set('Authorization', `Bearer ${token}`)
    });

    return next.handle(authReq);
  }
}
