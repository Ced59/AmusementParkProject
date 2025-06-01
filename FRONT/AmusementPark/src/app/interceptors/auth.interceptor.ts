import { Injectable } from '@angular/core';
import { HttpEvent, HttpInterceptor, HttpHandler, HttpRequest } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {

  constructor() {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    if (
      typeof localStorage === 'undefined'
      || req.url.endsWith('/login')
      || req.url.includes('google-response')
    ) {
      return next.handle(req);
    }

    const authToken = localStorage.getItem('auth_token');

    if (authToken) {

      if (!this.isTokenExpired(authToken)) {
        const authReq = req.clone({
          headers: req.headers.set('Authorization', `Bearer ${authToken}`)
        });

        return next.handle(authReq);
      }
    }

    return next.handle(req);
  }

  private isTokenExpired(token: string): boolean {

    const decodedToken = JSON.parse(atob(token.split('.')[1]));
    const expirationDate = new Date(decodedToken.exp * 1000);
    return expirationDate < new Date();
  }
}
