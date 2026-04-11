import {
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest
} from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, switchMap } from 'rxjs';

import { AuthService } from '../../../services/auth/auth.service';
import { shouldSkipAuthorizationHeader } from '../auth/auth-request-policy';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(private readonly authService: AuthService) {
  }

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    if (typeof window === 'undefined' || shouldSkipAuthorizationHeader(req.url)) {
      return next.handle(req);
    }

    return this.authService.ensureValidAccessToken().pipe(
      switchMap((token: string | null) => {
        if (!token) {
          return next.handle(req);
        }

        const authReq: HttpRequest<unknown> = req.clone({
          headers: req.headers.set('Authorization', `Bearer ${token}`)
        });

        return next.handle(authReq);
      })
    );
  }
}
