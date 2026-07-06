import { FetchBackend, HttpBackend, HttpErrorResponse, HttpEvent, HttpRequest } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, throwError, timer } from 'rxjs';
import { retry } from 'rxjs/operators';

import { environment } from '../../../../environments/environment';

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
  private static readonly TransientRetryCount: number = 2;
  private static readonly TransientRetryBaseDelayMilliseconds: number = 250;
  private static readonly TransientRetryMaxDelayMilliseconds: number = 2_000;

  constructor(private readonly fetchBackend: FetchBackend) {
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
        count: ServerApiBaseUrlBackend.TransientRetryCount,
        delay: (error: unknown, retryCount: number) => this.resolveTransientRetryDelay(error, rewrittenRequest, retryCount)
      })
    );
  }

  private rewriteApiUrl(url: string): string {
    const browserApiBaseUrl: string = ServerApiBaseUrlBackend.ensureTrailingSlash(environment.apiBaseUrl);
    const serverApiBaseUrl: string = ServerApiBaseUrlBackend.ensureTrailingSlash(environment.ssrApiBaseUrl);

    if (browserApiBaseUrl === serverApiBaseUrl || !url.startsWith(browserApiBaseUrl)) {
      return url;
    }

    return `${serverApiBaseUrl}${url.substring(browserApiBaseUrl.length)}`;
  }

  private static ensureTrailingSlash(value: string): string {
    return value.endsWith('/') ? value : `${value}/`;
  }

  private resolveTransientRetryDelay(
    error: unknown,
    request: HttpRequest<unknown>,
    retryCount: number
  ): Observable<number> {
    if (!this.shouldRetryTransientError(error, request)) {
      return throwError(() => error);
    }

    return timer(this.getRetryDelayMilliseconds(error, retryCount));
  }

  private shouldRetryTransientError(error: unknown, request: HttpRequest<unknown>): boolean {
    if (!this.isSafeMethod(request.method)) {
      return false;
    }

    if (!(error instanceof HttpErrorResponse)) {
      return true;
    }

    return error.status === 0
      || error.status === 408
      || error.status === 429
      || error.status === 500
      || error.status === 502
      || error.status === 503
      || error.status === 504;
  }

  private isSafeMethod(method: string): boolean {
    const normalizedMethod: string = method.toUpperCase();
    return normalizedMethod === 'GET' || normalizedMethod === 'HEAD';
  }

  private getRetryDelayMilliseconds(error: unknown, retryCount: number): number {
    const retryAfterDelayMilliseconds: number | null = this.tryReadRetryAfterDelayMilliseconds(error);
    if (retryAfterDelayMilliseconds !== null) {
      return retryAfterDelayMilliseconds;
    }

    return Math.min(
      ServerApiBaseUrlBackend.TransientRetryBaseDelayMilliseconds * retryCount,
      ServerApiBaseUrlBackend.TransientRetryMaxDelayMilliseconds
    );
  }

  private tryReadRetryAfterDelayMilliseconds(error: unknown): number | null {
    if (!(error instanceof HttpErrorResponse)) {
      return null;
    }

    const retryAfterHeader: string | null = error.headers.get('Retry-After');
    if (retryAfterHeader === null) {
      return null;
    }

    const retryAfterSeconds: number = Number.parseInt(retryAfterHeader, 10);
    if (!Number.isFinite(retryAfterSeconds) || retryAfterSeconds < 0) {
      return null;
    }

    return Math.min(
      retryAfterSeconds * 1000,
      ServerApiBaseUrlBackend.TransientRetryMaxDelayMilliseconds
    );
  }
}
