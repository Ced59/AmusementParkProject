import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import {
  CONFIRM_ACCOUNT_PAGE_STATE_AUTH_API_SERVICE_PORT,
  ConfirmAccountPageStateAuthApiServicePort,
} from './confirm-account-page-state-data.ports';
import { ConfirmAccountPageStateFacade } from './confirm-account-page-state.facade';
import { FakeAuthPort } from './test-helpers/confirm-account-page-state.facade/fake-auth-port';

describe('ConfirmAccountPageStateFacade', () => {
  let facade: ConfirmAccountPageStateFacade;
  let port: FakeAuthPort;

  beforeEach(() => {
    port = new FakeAuthPort();

    TestBed.configureTestingModule({
      providers: [
        ConfirmAccountPageStateFacade,
        {
          provide: CONFIRM_ACCOUNT_PAGE_STATE_AUTH_API_SERVICE_PORT,
          useValue: port,
        },
      ],
    });

    facade = TestBed.inject(ConfirmAccountPageStateFacade);
  });

  it('does not call the API when the token is missing', () => {
    facade.confirmEmail('', 'fr');

    expect(port.calls).toEqual([]);
    expect(facade.currentLanguage()).toBe('fr');
    expect(facade.isSuccess()).toBe(false);
    expect(facade.message()).toBe('Le lien de confirmation est invalide.');
  });

  it('confirms the account through the auth port', () => {
    facade.confirmEmail('token-1', 'fr');

    expect(port.calls).toEqual(['token-1']);
    expect(facade.isSuccess()).toBe(true);
    expect(facade.message()).toBe('Compte confirmé.');
  });

  it('uses the backend error message when confirmation fails', () => {
    port.response$ = throwError(() => ({ error: { message: 'Lien expiré.' } }));

    facade.confirmEmail('token-1', 'fr');

    expect(facade.isSuccess()).toBe(false);
    expect(facade.message()).toBe('Lien expiré.');
  });
});
