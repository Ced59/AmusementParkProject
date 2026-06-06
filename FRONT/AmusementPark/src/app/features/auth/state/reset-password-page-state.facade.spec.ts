import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import {
  RESET_PASSWORD_PAGE_STATE_AUTH_API_SERVICE_PORT,
  ResetPasswordPageStateAuthApiServicePort
} from './reset-password-page-state-data.ports';
import { ResetPasswordPageStateFacade } from './reset-password-page-state.facade';

class FakeAuthPort implements ResetPasswordPageStateAuthApiServicePort {
  public response$: Observable<{ message: string }> = of({ message: 'Mot de passe réinitialisé.' });
  public readonly calls: { token: string; newPassword: string; confirmPassword: string }[] = [];

  resetPassword(token: string, newPassword: string, confirmPassword: string): Observable<{ message: string }> {
    this.calls.push({ token, newPassword, confirmPassword });
    return this.response$;
  }
}

describe('ResetPasswordPageStateFacade', () => {
  let facade: ResetPasswordPageStateFacade;
  let port: FakeAuthPort;

  beforeEach(() => {
    port = new FakeAuthPort();

    TestBed.configureTestingModule({
      providers: [
        ResetPasswordPageStateFacade,
        { provide: RESET_PASSWORD_PAGE_STATE_AUTH_API_SERVICE_PORT, useValue: port }
      ]
    });

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
    expect(facade.isSubmitted()).toBeFalse();
  });

  it('submits the reset request through the auth port', () => {
    facade.initialize('token-1');
    facade.setNewPassword('Secret123!');
    facade.setConfirmPassword('Secret123!');

    facade.submit();

    expect(port.calls).toEqual([{ token: 'token-1', newPassword: 'Secret123!', confirmPassword: 'Secret123!' }]);
    expect(facade.isSubmitted()).toBeTrue();
    expect(facade.message()).toBe('Mot de passe réinitialisé.');
  });

  it('exposes a friendly error message when reset fails', () => {
    port.response$ = throwError(() => ({ error: { message: 'Token invalide.' } }));
    facade.initialize('token-1');
    facade.setNewPassword('Secret123!');
    facade.setConfirmPassword('Secret123!');

    facade.submit();

    expect(facade.isSubmitted()).toBeTrue();
    expect(facade.message()).toBe('Token invalide.');
  });
});
