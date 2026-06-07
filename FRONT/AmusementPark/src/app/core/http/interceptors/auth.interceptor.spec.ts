import { HttpHandler, HttpRequest, HttpResponse } from '@angular/common/http';
import { of } from 'rxjs';

import { AuthService } from '@app/services/auth/auth.service';
import { AuthInterceptor } from './auth.interceptor';

describe('AuthInterceptor', () => {
  function createAuthService(token: string | null): jasmine.SpyObj<AuthService> {
    const authService = jasmine.createSpyObj<AuthService>('AuthService', ['ensureValidAccessToken']);
    authService.ensureValidAccessToken.and.returnValue(of(token));
    return authService;
  }

  function captureHeaders(interceptor: AuthInterceptor, url: string, method: string = 'GET'): Promise<string | null> {
    return new Promise((resolve) => {
      const handler: HttpHandler = {
        handle: (request: HttpRequest<unknown>) => {
          resolve(request.headers.get('Authorization'));
          return of(new HttpResponse({ status: 200 }));
        }
      };

      interceptor.intercept(new HttpRequest(method, url), handler).subscribe();
    });
  }

  it('adds the bearer token for protected browser requests', async () => {
    const authService: jasmine.SpyObj<AuthService> = createAuthService('access-token');
    const interceptor = new AuthInterceptor(authService, 'browser' as unknown as object);

    const authorizationHeader: string | null = await captureHeaders(interceptor, '/api/admin/parks');

    expect(authorizationHeader).toBe('Bearer access-token');
    expect(authService.ensureValidAccessToken).toHaveBeenCalledOnceWith();
  });

  it('adds the bearer token for the admin users list request', async () => {
    const authService: jasmine.SpyObj<AuthService> = createAuthService('access-token');
    const interceptor = new AuthInterceptor(authService, 'browser' as unknown as object);

    const authorizationHeader: string | null = await captureHeaders(interceptor, '/api/users?page=1&size=10');

    expect(authorizationHeader).toBe('Bearer access-token');
    expect(authService.ensureValidAccessToken).toHaveBeenCalledOnceWith();
  });

  it('does not add an Authorization header when no token is available', async () => {
    const authService: jasmine.SpyObj<AuthService> = createAuthService(null);
    const interceptor = new AuthInterceptor(authService, 'browser' as unknown as object);

    const authorizationHeader: string | null = await captureHeaders(interceptor, '/api/admin/parks');

    expect(authorizationHeader).toBeNull();
  });

  it('skips public auth endpoints to avoid sending credentials to login and refresh routes', async () => {
    const authService: jasmine.SpyObj<AuthService> = createAuthService('access-token');
    const interceptor = new AuthInterceptor(authService, 'browser' as unknown as object);

    const authorizationHeader: string | null = await captureHeaders(interceptor, '/api/auth/login', 'POST');

    expect(authorizationHeader).toBeNull();
    expect(authService.ensureValidAccessToken).not.toHaveBeenCalled();
  });

  it('does not try to resolve tokens during SSR', async () => {
    const authService: jasmine.SpyObj<AuthService> = createAuthService('access-token');
    const interceptor = new AuthInterceptor(authService, 'server' as unknown as object);

    const authorizationHeader: string | null = await captureHeaders(interceptor, '/api/admin/parks');

    expect(authorizationHeader).toBeNull();
    expect(authService.ensureValidAccessToken).not.toHaveBeenCalled();
  });
});
