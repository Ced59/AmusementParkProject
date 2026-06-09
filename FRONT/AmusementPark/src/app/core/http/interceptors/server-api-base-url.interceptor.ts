import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';

/**
 * Réécrit uniquement les appels HTTP effectués pendant le rendu SSR.
 *
 * En développement, le navigateur continue d'appeler l'API en HTTPS afin de
 * conserver le comportement des cookies sécurisés, tandis que Node.js utilise
 * l'endpoint HTTP local exposé par le backend pour éviter le rejet du certificat
 * auto-signé de développement.
 */
@Injectable()
export class ServerApiBaseUrlInterceptor implements HttpInterceptor {
  private static readonly InternalSsrHeaderName: string = 'X-AmusementPark-Internal-SSR';
  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const rewrittenUrl: string = this.rewriteApiUrl(request.url);

    if (rewrittenUrl === request.url) {
      return next.handle(request);
    }

    return next.handle(request.clone({
      url: rewrittenUrl,
      headers: request.headers.set(ServerApiBaseUrlInterceptor.InternalSsrHeaderName, '1')
    }));
  }

  private rewriteApiUrl(url: string): string {
    const browserApiBaseUrl: string = ServerApiBaseUrlInterceptor.ensureTrailingSlash(environment.apiBaseUrl);
    const serverApiBaseUrl: string = ServerApiBaseUrlInterceptor.ensureTrailingSlash(environment.ssrApiBaseUrl);

    if (browserApiBaseUrl === serverApiBaseUrl || !url.startsWith(browserApiBaseUrl)) {
      return url;
    }

    return `${serverApiBaseUrl}${url.substring(browserApiBaseUrl.length)}`;
  }

  private static ensureTrailingSlash(value: string): string {
    return value.endsWith('/') ? value : `${value}/`;
  }
}
