import { TestBed } from '@angular/core/testing';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
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
      imports: [TranslateModule.forRoot()],
      providers: [
        ConfirmAccountPageStateFacade,
        {
          provide: CONFIRM_ACCOUNT_PAGE_STATE_AUTH_API_SERVICE_PORT,
          useValue: port,
        },
      ],
    });

    const translateService: TranslateService = TestBed.inject(TranslateService);
    translateService.setTranslation('fr', { auth: { confirm: {
      invalidLinkMessage: 'Le lien de confirmation est invalide.', fallbackError: 'La confirmation du compte a échoué.'
    } } });
    translateService.setTranslation('en', { auth: { confirm: {
      invalidLinkMessage: 'The confirmation link is invalid.', fallbackError: 'Your account could not be confirmed.'
    } } });
    translateService.use('fr');
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

  it('localizes an invalid confirmation link without making an auth request', () => {
    TestBed.inject(TranslateService).use('en');
    facade.confirmEmail('', 'en');
    expect(port.calls).toEqual([]);
    expect(facade.currentLanguage()).toBe('en');
    expect(facade.message()).toBe('The confirmation link is invalid.');
  });

  it.each([
    ['fr', 'La confirmation du compte a échoué.'],
    ['en', 'Your account could not be confirmed.']
  ])('uses the localized %s fallback only when the server supplies no message', (language: string, message: string) => {
    TestBed.inject(TranslateService).use(language);
    port.response$ = throwError(() => new Error('offline'));
    facade.confirmEmail('token-1', language);
    expect(port.calls).toEqual(['token-1']);
    expect(facade.isSuccess()).toBe(false);
    expect(facade.message()).toBe(message);
  });

  it('uses the backend error message when confirmation fails', () => {
    port.response$ = throwError(() => ({ error: { message: 'Lien expiré.' } }));

    facade.confirmEmail('token-1', 'fr');

    expect(facade.isSuccess()).toBe(false);
    expect(facade.message()).toBe('Lien expiré.');
  });
});
