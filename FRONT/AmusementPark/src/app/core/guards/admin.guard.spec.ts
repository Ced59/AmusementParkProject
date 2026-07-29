import type { MockedObject } from 'vitest';
import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, UrlTree, provideRouter } from '@angular/router';
import { firstValueFrom, Observable, of } from 'rxjs';

import { AuthService } from '@app/services/auth/auth.service';
import { adminGuard } from './admin.guard';

describe('adminGuard', () => {
  let authService: MockedObject<AuthService>;
  let router: Router;

  beforeEach(() => {
    authService = {
      ensureValidAccessToken: vi
        .fn()
        .mockName('AuthService.ensureValidAccessToken'),
      hasRole: vi.fn().mockName('AuthService.hasRole'),
    } as unknown as MockedObject<AuthService>;

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authService },
        { provide: PLATFORM_ID, useValue: 'browser' },
      ],
    });
    router = TestBed.inject(Router);
  });

  async function runGuard(url: string): Promise<boolean | UrlTree> {
    const result: unknown = TestBed.runInInjectionContext(() =>
      adminGuard({} as never, { url } as never),
    );
    return typeof result === 'boolean' || result instanceof UrlTree
      ? result
      : await firstValueFrom(result as Observable<boolean | UrlTree>);
  }

  it('allows activation for authenticated administrators', async () => {
    authService.ensureValidAccessToken.mockReturnValue(of('token'));
    authService.hasRole.mockReturnValue(true);

    await expect(runGuard('/fr/admin')).resolves.toEqual(true);
  });

  it('redirects authenticated non-admin users to localized home', async () => {
    vi.spyOn(console, 'warn');
    authService.ensureValidAccessToken.mockReturnValue(of('token'));
    authService.hasRole.mockReturnValue(false);

    const result: boolean | UrlTree = await runGuard('/de/admin');

    expect(router.serializeUrl(result as UrlTree)).toBe('/de/home');
    expect(console.warn).toHaveBeenCalled();
  });

  it('redirects anonymous users without checking roles', async () => {
    authService.ensureValidAccessToken.mockReturnValue(of(null));

    const result: boolean | UrlTree = await runGuard('/fr/admin');

    expect(router.serializeUrl(result as UrlTree)).toBe('/fr/home');
    expect(authService.hasRole).not.toHaveBeenCalled();
  });
});
