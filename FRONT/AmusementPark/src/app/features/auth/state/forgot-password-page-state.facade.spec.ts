import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import {
  FORGOT_PASSWORD_PAGE_STATE_AUTH_API_SERVICE_PORT,
  ForgotPasswordPageStateAuthApiServicePort
} from './forgot-password-page-state-data.ports';
import { ForgotPasswordPageStateFacade } from './forgot-password-page-state.facade';

class FakeAuthPort implements ForgotPasswordPageStateAuthApiServicePort {
  public response$: Observable<{ message: string }> = of({ message: 'Email envoyé.' });
  public readonly calls: string[] = [];

  forgotPassword(email: string): Observable<{ message: string }> {
    this.calls.push(email);
    return this.response$;
  }
}

describe('ForgotPasswordPageStateFacade', () => {
  let facade: ForgotPasswordPageStateFacade;
  let port: FakeAuthPort;

  beforeEach(() => {
    port = new FakeAuthPort();

    TestBed.configureTestingModule({
      providers: [
        ForgotPasswordPageStateFacade,
        { provide: FORGOT_PASSWORD_PAGE_STATE_AUTH_API_SERVICE_PORT, useValue: port }
      ]
    });

    facade = TestBed.inject(ForgotPasswordPageStateFacade);
  });

  it('updates the email locally without calling the API', () => {
    facade.setEmail(' ced@example.test ');

    expect(facade.email()).toBe(' ced@example.test ');
    expect(port.calls).toEqual([]);
  });

  it('does not submit when the email is blank', () => {
    facade.setEmail('   ');

    facade.submit();

    expect(port.calls).toEqual([]);
    expect(facade.isSubmitted()).toBeFalse();
  });

  it('submits the trimmed email and stores the success message', () => {
    facade.setEmail(' ced@example.test ');

    facade.submit();

    expect(port.calls).toEqual(['ced@example.test']);
    expect(facade.isSubmitted()).toBeTrue();
    expect(facade.message()).toBe('Email envoyé.');
  });

  it('keeps the submitted state with the backend error message on failure', () => {
    port.response$ = throwError(() => ({ error: { Message: 'Adresse inconnue.' } }));
    facade.setEmail('ced@example.test');

    facade.submit();

    expect(facade.isSubmitted()).toBeTrue();
    expect(facade.message()).toBe('Adresse inconnue.');
  });
});
