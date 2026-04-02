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

  constructor(private readonly authService: AuthService) {
  }

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    if (
      typeof window === 'undefined'
      || req.url.endsWith('/login')
      || req.url.includes('/auth/external/')
      || req.url.includes('google-response')
    ) {
      return next.handle(req);
    }

    const token: string | null = this.authService.getToken();
    if (!token) {
      return next.handle(req);
    }

    const decoded = this.authService.getTokenDecoded();
    if (!decoded || !decoded.exp || Date.now() >= decoded.exp * 1000) {
      return next.handle(req);
    }

    const authReq: HttpRequest<unknown> = req.clone({
      headers: req.headers.set('Authorization', `Bearer ${token}`)
    });

    return next.handle(authReq);
  }
}
