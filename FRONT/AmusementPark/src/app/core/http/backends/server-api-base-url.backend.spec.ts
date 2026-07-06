import { FetchBackend, HttpErrorResponse, HttpEvent, HttpHeaders, HttpRequest, HttpResponse } from '@angular/common/http';
import { defer, Observable, of, throwError } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ServerApiBaseUrlBackend } from './server-api-base-url.backend';

class FetchBackendFake {
  public capturedUrl: string = '';
  public capturedInternalSsrHeader: string | null = null;
  public subscriptionCount: number = 0;

  constructor(
    private readonly responseFactory: (attempt: number, request: HttpRequest<unknown>) => Observable<HttpEvent<unknown>> =
      () => of(new HttpResponse({ status: 200 }))
  ) {
  }

  handle(request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> {
    this.capturedUrl = request.url;
    this.capturedInternalSsrHeader = request.headers.get('X-AmusementPark-Internal-SSR');
    return defer(() => {
      this.subscriptionCount += 1;
      return this.responseFactory(this.subscriptionCount, request);
    });
  }
}

describe('ServerApiBaseUrlBackend', () => {
  function handleUrl(url: string): FetchBackendFake {
    const fetchBackend: FetchBackendFake = new FetchBackendFake();
    const backend: ServerApiBaseUrlBackend = new ServerApiBaseUrlBackend(fetchBackend as unknown as FetchBackend);

    backend.handle(new HttpRequest('GET', url)).subscribe();

    return fetchBackend;
  }

  it('rewrites browser API urls to the SSR API base url and flags the internal call', () => {
    const result: FetchBackendFake = handleUrl(`${environment.apiBaseUrl}api/parks?page=1`);

    expect(result.capturedUrl).toBe(`${environment.ssrApiBaseUrl}api/parks?page=1`);
    expect(result.capturedInternalSsrHeader).toBe('1');
  });

  it('leaves already external or relative urls untouched', () => {
    expect(handleUrl('https://cdn.example.test/image.png').capturedUrl).toBe('https://cdn.example.test/image.png');
    expect(handleUrl('/assets/logo.png').capturedUrl).toBe('/assets/logo.png');
  });

  it('does not rewrite a similar prefix from another host', () => {
    const similarUrl: string = `${environment.apiBaseUrl.replace(/\/$/, '')}.evil.test/api/parks`;

    expect(handleUrl(similarUrl).capturedUrl).toBe(similarUrl);
  });

  it('retries transient safe internal API failures during SSR', (done: DoneFn) => {
    const retryAfterHeaders: HttpHeaders = new HttpHeaders({ 'Retry-After': '0' });
    const transientError: HttpErrorResponse = new HttpErrorResponse({ status: 429, headers: retryAfterHeaders });
    const fetchBackend: FetchBackendFake = new FetchBackendFake((attempt: number) => {
      if (attempt === 1) {
        return throwError(() => transientError);
      }

      return of(new HttpResponse({ status: 200 }));
    });
    const backend: ServerApiBaseUrlBackend = new ServerApiBaseUrlBackend(fetchBackend as unknown as FetchBackend);

    backend.handle(new HttpRequest('GET', `${environment.apiBaseUrl}api/videos/video-1`)).subscribe({
      next: (event: HttpEvent<unknown>) => {
        if (event instanceof HttpResponse) {
          expect(fetchBackend.subscriptionCount).toBe(2);
          expect(event.status).toBe(200);
          done();
        }
      },
      error: done.fail
    });
  });

  it('does not retry permanent not found responses during SSR', (done: DoneFn) => {
    const notFoundError: HttpErrorResponse = new HttpErrorResponse({ status: 404 });
    const fetchBackend: FetchBackendFake = new FetchBackendFake(() => throwError(() => notFoundError));
    const backend: ServerApiBaseUrlBackend = new ServerApiBaseUrlBackend(fetchBackend as unknown as FetchBackend);

    backend.handle(new HttpRequest('GET', `${environment.apiBaseUrl}api/videos/missing`)).subscribe({
      next: () => {
        done.fail('Expected the 404 response to stay an error.');
      },
      error: (error: HttpErrorResponse) => {
        expect(fetchBackend.subscriptionCount).toBe(1);
        expect(error.status).toBe(404);
        done();
      }
    });
  });

  it('does not retry unsafe internal API methods during SSR', (done: DoneFn) => {
    const serviceUnavailableError: HttpErrorResponse = new HttpErrorResponse({ status: 503 });
    const fetchBackend: FetchBackendFake = new FetchBackendFake(() => throwError(() => serviceUnavailableError));
    const backend: ServerApiBaseUrlBackend = new ServerApiBaseUrlBackend(fetchBackend as unknown as FetchBackend);

    backend.handle(new HttpRequest('POST', `${environment.apiBaseUrl}api/social-share/events`, {})).subscribe({
      next: () => {
        done.fail('Expected the unsafe request to stay an error.');
      },
      error: (error: HttpErrorResponse) => {
        expect(fetchBackend.subscriptionCount).toBe(1);
        expect(error.status).toBe(503);
        done();
      }
    });
  });
});
