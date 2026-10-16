import { TestBed } from '@angular/core/testing';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { Observable, of, throwError } from 'rxjs';

import {
  RESET_PASSWORD_PAGE_STATE_AUTH_API_SERVICE_PORT,
  ResetPasswordPageStateAuthApiServicePort,
} from './reset-password-page-state-data.ports';
import { ResetPasswordPageStateFacade } from './reset-password-page-state.facade';
import { FakeAuthPort } from './test-helpers/reset-password-page-state.facade/fake-auth-port';

describe('ResetPasswordPageStateFacade', () => {
  let facade: ResetPasswordPageStateFacade;
  let port: FakeAuthPort;

  beforeEach(() => {
    port = new FakeAuthPort();

    TestBed.configureTestingModule({
      imports: [TranslateModule.forRoot()],
      providers: [
        ResetPasswordPageStateFacade,
        {
          provide: RESET_PASSWORD_PAGE_STATE_AUTH_API_SERVICE_PORT,
          useValue: port,
        },
      ],
    });

    const translateService: TranslateService = TestBed.inject(TranslateService);
    translateService.setTranslation('fr', { auth: { reset_password: { fallbackError: 'La réinitialisation du mot de passe a échoué.' } } });
    translateService.setTranslation('en', { auth: { reset_password: { fallbackError: 'Your password could not be reset.' } } });
    translateService.use('fr');
    facade = TestBed.inject(ResetPasswordPageStateFacade);
  });

  it('initializes the token and clears password fields', () => {
    facade.initialize('token-1');

    expect(facade.token()).toBe('token-1');
    expect(facade.newPassword()).toBe('');
    expect(facade.confirmPassword()).toBe('');
  });

  it('does not submit while required fields are missing', () => {
    facade.initialize('token-1');
    facade.setNewPassword('Secret123!');

    facade.submit();

    expect(port.calls).toEqual([]);
    expect(facade.isSubmitted()).toBe(false);
  });

  it('submits the reset request through the auth port', () => {
    facade.initialize('token-1');
    facade.setNewPassword('Secret123!');
    facade.setConfirmPassword('Secret123!');

    facade.submit();

    expect(port.calls).toEqual([
      {
        token: 'token-1',
        newPassword: 'Secret123!',
        confirmPassword: 'Secret123!',
      },
    ]);
    expect(facade.isSubmitted()).toBe(true);
    expect(facade.message()).toBe('Mot de passe réinitialisé.');
  });

  it('exposes a friendly error message when reset fails', () => {
    port.response$ = throwError(() => ({
      error: { message: 'Token invalide.' },
    }));
    facade.initialize('token-1');
    facade.setNewPassword('Secret123!');
    facade.setConfirmPassword('Secret123!');

    facade.submit();

    expect(facade.isSubmitted()).toBe(true);
    expect(facade.message()).toBe('Token invalide.');
  });

  it.each([
    ['fr', 'La réinitialisation du mot de passe a échoué.'],
    ['en', 'Your password could not be reset.']
  ])('uses the localized %s fallback while preserving the reset payload', (language: string, message: string) => {
    TestBed.inject(TranslateService).use(language);
    port.response$ = throwError(() => new Error('offline'));
    facade.initialize('token-1');
    facade.setNewPassword('Secret123!');
    facade.setConfirmPassword('Secret123!');
    facade.submit();
    expect(port.calls).toEqual([{ token: 'token-1', newPassword: 'Secret123!', confirmPassword: 'Secret123!' }]);
    expect(facade.isSubmitted()).toBe(true);
    expect(facade.message()).toBe(message);
  });
});
