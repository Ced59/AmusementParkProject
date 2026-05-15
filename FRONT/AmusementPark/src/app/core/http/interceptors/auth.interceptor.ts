import {
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest
} from '@angular/common/http';
import { isPlatformBrowser } from '@angular/common';
import { Inject, Injectable, PLATFORM_ID } from '@angular/core';
import { Observable, switchMap } from 'rxjs';

import { AuthService } from '@app/services/auth/auth.service';
import { shouldSkipAuthorizationHeader } from '../auth/auth-request-policy';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(
    private readonly authService: AuthService,
    @Inject(PLATFORM_ID) private readonly platformId: object
  ) {
  }

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    if (!isPlatformBrowser(this.platformId) || shouldSkipAuthorizationHeader(req.url)) {
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
