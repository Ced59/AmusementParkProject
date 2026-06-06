import { HttpHandler, HttpRequest, HttpResponse } from '@angular/common/http';
import { of } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ServerApiBaseUrlInterceptor } from './server-api-base-url.interceptor';

describe('ServerApiBaseUrlInterceptor', () => {
  function captureUrlFor(url: string): string {
    let capturedUrl: string = '';
    const handler: HttpHandler = {
      handle: (request: HttpRequest<unknown>) => {
        capturedUrl = request.url;
        return of(new HttpResponse({ status: 200 }));
      }
    };

    new ServerApiBaseUrlInterceptor()
      .intercept(new HttpRequest('GET', url), handler)
      .subscribe();

    return capturedUrl;
  }

  it('rewrites browser API urls to the SSR API base url', () => {
    const rewrittenUrl: string = captureUrlFor(`${environment.apiBaseUrl}api/parks?page=1`);

    expect(rewrittenUrl).toBe(`${environment.ssrApiBaseUrl}api/parks?page=1`);
  });

  it('leaves already external or relative urls untouched', () => {
    expect(captureUrlFor('https://cdn.example.test/image.png')).toBe('https://cdn.example.test/image.png');
    expect(captureUrlFor('/assets/logo.png')).toBe('/assets/logo.png');
  });

  it('does not rewrite a similar prefix from another host', () => {
    const similarUrl: string = `${environment.apiBaseUrl.replace(/\/$/, '')}.evil.test/api/parks`;

    expect(captureUrlFor(similarUrl)).toBe(similarUrl);
  });
});
