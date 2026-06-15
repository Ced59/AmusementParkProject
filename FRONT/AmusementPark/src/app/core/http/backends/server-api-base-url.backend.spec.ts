import { FetchBackend, HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, of } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ServerApiBaseUrlBackend } from './server-api-base-url.backend';

class FetchBackendFake {
  public capturedUrl: string = '';
  public capturedInternalSsrHeader: string | null = null;

  handle(request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> {
    this.capturedUrl = request.url;
    this.capturedInternalSsrHeader = request.headers.get('X-AmusementPark-Internal-SSR');
    return of(new HttpResponse({ status: 200 }));
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
});
