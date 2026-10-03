import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import {
  ACCOUNT_DELETION_DATA_PORT,
  AccountDeletionDataPort
} from './account-deletion-data.port';
import { AccountDeletionStateFacade } from './account-deletion-state.facade';

describe('AccountDeletionStateFacade', () => {
  let facade: AccountDeletionStateFacade;
  let data: AccountDeletionDataPort;

  beforeEach(() => {
    data = {
      deleteCurrentAccount: vi.fn().mockReturnValue(of(undefined))
    };
    TestBed.configureTestingModule({
      providers: [
        AccountDeletionStateFacade,
        { provide: ACCOUNT_DELETION_DATA_PORT, useValue: data }
      ]
    });
    facade = TestBed.inject(AccountDeletionStateFacade);
  });

  it('submits the explicit account confirmation and completes', () => {
    facade.request({
      confirmationEmail: 'member@example.com',
      currentPassword: 'secret'
    });

    expect(data.deleteCurrentAccount).toHaveBeenCalledWith({
      confirmationEmail: 'member@example.com',
      currentPassword: 'secret'
    });
    expect(facade.completed()).toBe(true);
    expect(facade.submitting()).toBe(false);
    expect(facade.errorKey()).toBeNull();
  });

  it('exposes a safe localized error when the request fails', () => {
    vi.mocked(data.deleteCurrentAccount).mockReturnValue(
      throwError(() => new Error('network'))
    );

    facade.request({
      confirmationEmail: 'member@example.com',
      currentPassword: ''
    });

    expect(facade.completed()).toBe(false);
    expect(facade.submitting()).toBe(false);
    expect(facade.errorKey()).toBe('accountDeletion.errors.request');
  });
});
