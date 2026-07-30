import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandler,
  HttpHeaders,
  HttpRequest,
  HttpResponse
} from '@angular/common/http';
import { defer, firstValueFrom, Observable, of, throwError } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { TransientApiReadRetryInterceptor } from './transient-api-read-retry.interceptor';

class HttpHandlerFake implements HttpHandler {
  public subscriptionCount: number = 0;

  constructor(
    private readonly responseFactory: (
      attempt: number,
      request: HttpRequest<unknown>
    ) => Observable<HttpEvent<unknown>>
  ) {
  }

  handle(request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> {
    return defer(() => {
      this.subscriptionCount += 1;
      return this.responseFactory(this.subscriptionCount, request);
    });
  }
}

describe('TransientApiReadRetryInterceptor', () => {
  const apiUrl: string = `${environment.apiBaseUrl}parks/park-1/detail-summary`;

  it.each([0, 408, 429, 500, 502, 503, 504])(
    'retries transient browser API read failures with status %i',
    async (status: number) => {
      const transientError: HttpErrorResponse = new HttpErrorResponse({
        status,
        headers: new HttpHeaders({ 'Retry-After': '0' })
      });
      const handler: HttpHandlerFake = new HttpHandlerFake((attempt: number) => {
        if (attempt === 1) {
          return throwError(() => transientError);
        }

        return of(new HttpResponse({ status: 200 }));
      });
      const interceptor: TransientApiReadRetryInterceptor =
        new TransientApiReadRetryInterceptor('browser' as unknown as object);

      const event: HttpEvent<unknown> = await firstValueFrom(
        interceptor.intercept(new HttpRequest('GET', apiUrl), handler)
      );

      expect(handler.subscriptionCount).toBe(2);
      expect(event).toBeInstanceOf(HttpResponse);
      expect((event as HttpResponse<unknown>).status).toBe(200);
    }
  );

  it('stops after the configured retry limit', async () => {
    const transientError: HttpErrorResponse = new HttpErrorResponse({
      status: 503,
      headers: new HttpHeaders({ 'Retry-After': '0' })
    });
    const handler: HttpHandlerFake = new HttpHandlerFake(() => throwError(() => transientError));
    const interceptor: TransientApiReadRetryInterceptor =
      new TransientApiReadRetryInterceptor('browser' as unknown as object);

    await expect(
      firstValueFrom(interceptor.intercept(new HttpRequest('GET', apiUrl), handler))
    ).rejects.toBe(transientError);

    expect(handler.subscriptionCount).toBe(3);
  });

  it('does not retry permanent browser API failures', async () => {
    const notFoundError: HttpErrorResponse = new HttpErrorResponse({ status: 404 });
    const handler: HttpHandlerFake = new HttpHandlerFake(() => throwError(() => notFoundError));
    const interceptor: TransientApiReadRetryInterceptor =
      new TransientApiReadRetryInterceptor('browser' as unknown as object);

    await expect(
      firstValueFrom(interceptor.intercept(new HttpRequest('GET', apiUrl), handler))
    ).rejects.toBe(notFoundError);

    expect(handler.subscriptionCount).toBe(1);
  });

  it('does not retry unsafe browser API requests', async () => {
    const transientError: HttpErrorResponse = new HttpErrorResponse({ status: 503 });
    const handler: HttpHandlerFake = new HttpHandlerFake(() => throwError(() => transientError));
    const interceptor: TransientApiReadRetryInterceptor =
      new TransientApiReadRetryInterceptor('browser' as unknown as object);

    await expect(
      firstValueFrom(interceptor.intercept(new HttpRequest('POST', apiUrl, {}), handler))
    ).rejects.toBe(transientError);

    expect(handler.subscriptionCount).toBe(1);
  });

  it('does not retry non-API browser requests', async () => {
    const transientError: HttpErrorResponse = new HttpErrorResponse({ status: 503 });
    const handler: HttpHandlerFake = new HttpHandlerFake(() => throwError(() => transientError));
    const interceptor: TransientApiReadRetryInterceptor =
      new TransientApiReadRetryInterceptor('browser' as unknown as object);

    await expect(
      firstValueFrom(interceptor.intercept(new HttpRequest('GET', '/assets/i18n/fr.json'), handler))
    ).rejects.toBe(transientError);

    expect(handler.subscriptionCount).toBe(1);
  });

  it('leaves retries to the dedicated backend during SSR', async () => {
    const transientError: HttpErrorResponse = new HttpErrorResponse({ status: 503 });
    const handler: HttpHandlerFake = new HttpHandlerFake(() => throwError(() => transientError));
    const interceptor: TransientApiReadRetryInterceptor =
      new TransientApiReadRetryInterceptor('server' as unknown as object);

    await expect(
      firstValueFrom(interceptor.intercept(new HttpRequest('GET', apiUrl), handler))
    ).rejects.toBe(transientError);

    expect(handler.subscriptionCount).toBe(1);
  });
});
