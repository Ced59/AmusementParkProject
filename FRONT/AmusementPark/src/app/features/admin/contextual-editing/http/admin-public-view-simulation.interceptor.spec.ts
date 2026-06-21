import { HttpContext, HttpHandler, HttpRequest, HttpResponse } from '@angular/common/http';
import { of } from 'rxjs';

import { SKIP_AUTHORIZATION_HEADER } from '@core/http/auth/auth-request-policy';
import { AdminPublicViewModeFacade } from '../state/admin-public-view-mode.facade';
import { AdminPublicViewSimulationInterceptor } from './admin-public-view-simulation.interceptor';

describe('AdminPublicViewSimulationInterceptor', () => {
  function captureRequest(interceptor: AdminPublicViewSimulationInterceptor, request: HttpRequest<unknown>): Promise<HttpRequest<unknown>> {
    return new Promise((resolve) => {
      const handler: HttpHandler = {
        handle: (nextRequest: HttpRequest<unknown>) => {
          resolve(nextRequest);
          return of(new HttpResponse({ status: 200 }));
        }
      };

      interceptor.intercept(request, handler).subscribe();
    });
  }

  function createAnonymousGetRequest(): HttpRequest<unknown> {
    return new HttpRequest('GET', '/api/parks/park-1', {
      context: new HttpContext().set(SKIP_AUTHORIZATION_HEADER, true)
    });
  }

  it('keeps anonymous public requests unchanged by default', async () => {
    const facade = new AdminPublicViewModeFacade();
    const interceptor = new AdminPublicViewSimulationInterceptor(facade);
    const request: HttpRequest<unknown> = createAnonymousGetRequest();

    const capturedRequest: HttpRequest<unknown> = await captureRequest(interceptor, request);

    expect(capturedRequest.headers.has('X-AmusementPark-Public-View-Mode')).toBeFalse();
    expect(capturedRequest.context.get(SKIP_AUTHORIZATION_HEADER)).toBeTrue();
  });

  it('adds the simulated role header and allows auth for user visitor mode', async () => {
    const facade = new AdminPublicViewModeFacade();
    facade.setViewMode('userVisitor');
    const interceptor = new AdminPublicViewSimulationInterceptor(facade);

    const capturedRequest: HttpRequest<unknown> = await captureRequest(interceptor, createAnonymousGetRequest());

    expect(capturedRequest.headers.get('X-AmusementPark-Public-View-Mode')).toBe('userVisitor');
    expect(capturedRequest.headers.get('Cache-Control')).toBe('no-store');
    expect(capturedRequest.headers.get('Pragma')).toBe('no-cache');
    expect(capturedRequest.context.get(SKIP_AUTHORIZATION_HEADER)).toBeFalse();
  });

  it('adds the simulated role header and allows auth for admin preview mode', async () => {
    const facade = new AdminPublicViewModeFacade();
    facade.setViewMode('adminPreview');
    const interceptor = new AdminPublicViewSimulationInterceptor(facade);

    const capturedRequest: HttpRequest<unknown> = await captureRequest(interceptor, createAnonymousGetRequest());

    expect(capturedRequest.headers.get('X-AmusementPark-Public-View-Mode')).toBe('adminPreview');
    expect(capturedRequest.context.get(SKIP_AUTHORIZATION_HEADER)).toBeFalse();
  });

  it('does not modify anonymous non-read requests', async () => {
    const facade = new AdminPublicViewModeFacade();
    facade.setViewMode('moderatorVisitor');
    const interceptor = new AdminPublicViewSimulationInterceptor(facade);
    const request = new HttpRequest('POST', '/api/contact', {}, {
      context: new HttpContext().set(SKIP_AUTHORIZATION_HEADER, true)
    });

    const capturedRequest: HttpRequest<unknown> = await captureRequest(interceptor, request);

    expect(capturedRequest.headers.has('X-AmusementPark-Public-View-Mode')).toBeFalse();
    expect(capturedRequest.context.get(SKIP_AUTHORIZATION_HEADER)).toBeTrue();
  });
});
