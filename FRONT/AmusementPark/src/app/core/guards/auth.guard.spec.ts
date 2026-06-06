import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, UrlTree, provideRouter } from '@angular/router';
import { firstValueFrom, Observable, of } from 'rxjs';

import { AuthService } from '@app/services/auth/auth.service';
import { ModalService } from '@app/services/modal/modal.service';
import { authGuard } from './auth.guard';

describe('authGuard', () => {
  let authService: jasmine.SpyObj<AuthService>;
  let modalService: jasmine.SpyObj<ModalService>;
  let router: Router;

  beforeEach(() => {
    authService = jasmine.createSpyObj<AuthService>('AuthService', ['ensureValidAccessToken']);
    modalService = jasmine.createSpyObj<ModalService>('ModalService', ['openModal']);

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authService },
        { provide: ModalService, useValue: modalService },
        { provide: PLATFORM_ID, useValue: 'browser' }
      ]
    });
    router = TestBed.inject(Router);
  });

  async function runGuard(url: string): Promise<boolean | UrlTree> {
    const result: unknown = TestBed.runInInjectionContext(() => authGuard({} as never, { url } as never));
    return typeof result === 'boolean' || result instanceof UrlTree
      ? result
      : await firstValueFrom(result as Observable<boolean | UrlTree>);
  }

  it('allows activation when a valid token exists', async () => {
    authService.ensureValidAccessToken.and.returnValue(of('token'));

    await expectAsync(runGuard('/fr/account')).toBeResolvedTo(true);
    expect(modalService.openModal).not.toHaveBeenCalled();
  });

  it('opens the login modal and redirects anonymous browser users to the localized home', async () => {
    authService.ensureValidAccessToken.and.returnValue(of(null));

    const result: boolean | UrlTree = await runGuard('/fr/account');

    expect(result instanceof UrlTree).toBeTrue();
    expect(router.serializeUrl(result as UrlTree)).toBe('/fr/home');
    expect(modalService.openModal).toHaveBeenCalledOnceWith('loginModal');
  });

  it('falls back to English when the current URL has no supported language', async () => {
    authService.ensureValidAccessToken.and.returnValue(of(null));

    const result: boolean | UrlTree = await runGuard('/unknown/account');

    expect(router.serializeUrl(result as UrlTree)).toBe('/en/home');
  });
});
