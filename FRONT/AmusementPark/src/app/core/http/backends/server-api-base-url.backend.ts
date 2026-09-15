import { FetchBackend, HttpBackend, HttpEvent, HttpRequest } from '@angular/common/http';
import { Inject, Injectable, Optional } from '@angular/core';
import { Observable } from 'rxjs';
import { retry } from 'rxjs/operators';

import { environment } from '../../../../environments/environment';
import { SSR_API_ORIGIN } from '../../ssr/ssr-api-origin.token';
import {
  resolveTransientHttpReadRetryDelay,
  TRANSIENT_HTTP_READ_RETRY_COUNT
} from '../transient-http-read-retry.policy';

/**
 * Backend HTTP utilisé uniquement pendant le rendu SSR.
 *
 * Il résout les appels API publics vers l'origine interne (Node -> API conteneur)
 * sans passer par un intercepteur. Contrairement à un intercepteur, un backend
 * s'exécute APRÈS le transfer-cache d'Angular : la clé de cache reste donc l'URL
 * relative (`/api/...`), strictement identique à celle émise par le navigateur.
 * La réponse récupérée pendant le SSR est ainsi réutilisée à l'hydratation, sans
 * second appel réseau (suppression du « flicker » frais -> périmé).
 *
 * En développement, le navigateur continue d'appeler l'API en HTTPS afin de
 * conserver le comportement des cookies sécurisés, tandis que Node.js utilise
 * l'endpoint HTTP local exposé par le backend.
 */
@Injectable()
export class ServerApiBaseUrlBackend implements HttpBackend {
  private static readonly InternalSsrHeaderName: string = 'X-AmusementPark-Internal-SSR';

  constructor(
    private readonly fetchBackend: FetchBackend,
    @Optional() @Inject(SSR_API_ORIGIN) private readonly serverApiOrigin: string | null = null,
  ) {
  }

  handle(request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> {
    const rewrittenUrl: string = this.rewriteApiUrl(request.url);

    if (rewrittenUrl === request.url) {
      return this.fetchBackend.handle(request);
    }

    const rewrittenRequest: HttpRequest<unknown> = request.clone({
      url: rewrittenUrl,
      headers: request.headers.set(ServerApiBaseUrlBackend.InternalSsrHeaderName, '1')
    });

    return this.fetchBackend.handle(rewrittenRequest).pipe(
      retry({
        count: TRANSIENT_HTTP_READ_RETRY_COUNT,
        delay: (error: unknown, retryCount: number) => {
          return resolveTransientHttpReadRetryDelay(error, rewrittenRequest, retryCount);
        }
      })
    );
  }

  private rewriteApiUrl(url: string): string {
    const browserApiBaseUrl: string = ServerApiBaseUrlBackend.ensureTrailingSlash(environment.apiBaseUrl);
    const serverApiBaseUrl: string = ServerApiBaseUrlBackend.ensureTrailingSlash(
      this.serverApiOrigin ?? environment.ssrApiBaseUrl,
    );

    if (browserApiBaseUrl === serverApiBaseUrl || !url.startsWith(browserApiBaseUrl)) {
      return url;
    }

    return `${serverApiBaseUrl}${url.substring(browserApiBaseUrl.length)}`;
  }

  private static ensureTrailingSlash(value: string): string {
    return value.endsWith('/') ? value : `${value}/`;
  }
}
