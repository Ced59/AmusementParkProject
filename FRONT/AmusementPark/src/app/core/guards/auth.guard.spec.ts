import type { MockedObject } from 'vitest';
import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, UrlTree, provideRouter } from '@angular/router';
import { firstValueFrom, Observable, of } from 'rxjs';

import { AuthService } from '@app/services/auth/auth.service';
import { ModalService } from '@app/services/modal/modal.service';
import { authGuard } from './auth.guard';

describe('authGuard', () => {
  let authService: MockedObject<AuthService>;
  let modalService: MockedObject<ModalService>;
  let router: Router;

  beforeEach(() => {
    authService = {
      ensureValidAccessToken: vi
        .fn()
        .mockName('AuthService.ensureValidAccessToken'),
    } as unknown as MockedObject<AuthService>;
    modalService = {
      openModal: vi.fn().mockName('ModalService.openModal'),
    } as unknown as MockedObject<ModalService>;

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authService },
        { provide: ModalService, useValue: modalService },
        { provide: PLATFORM_ID, useValue: 'browser' },
      ],
    });
    router = TestBed.inject(Router);
  });

  async function runGuard(url: string): Promise<boolean | UrlTree> {
    const result: unknown = TestBed.runInInjectionContext(() =>
      authGuard({} as never, { url } as never),
    );
    return typeof result === 'boolean' || result instanceof UrlTree
      ? result
      : await firstValueFrom(result as Observable<boolean | UrlTree>);
  }

  it('allows activation when a valid token exists', async () => {
    authService.ensureValidAccessToken.mockReturnValue(of('token'));

    await expect(runGuard('/fr/account')).resolves.toEqual(true);
    expect(modalService.openModal).not.toHaveBeenCalled();
  });

  it('opens the login modal and preserves the protected destination on the localized home', async () => {
    authService.ensureValidAccessToken.mockReturnValue(of(null));

    const result: boolean | UrlTree = await runGuard('/fr/account');

    expect(result instanceof UrlTree).toBe(true);
    expect(router.serializeUrl(result as UrlTree))
      .toBe('/fr/home?returnUrl=%2Ffr%2Faccount');
    expect(modalService.openModal).toHaveBeenCalledTimes(1);
    expect(modalService.openModal).toHaveBeenCalledWith('loginModal');
  });

  it('falls back to English when the current URL has no supported language', async () => {
    authService.ensureValidAccessToken.mockReturnValue(of(null));

    const result: boolean | UrlTree = await runGuard('/unknown/account');

    expect(router.serializeUrl(result as UrlTree))
      .toBe('/en/home?returnUrl=%2Funknown%2Faccount');
  });
});
