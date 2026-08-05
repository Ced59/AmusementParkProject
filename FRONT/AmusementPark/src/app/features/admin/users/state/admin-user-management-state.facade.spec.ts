import type { MockedObject } from 'vitest';
import { DestroyRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { of, throwError } from 'rxjs';

import { ParkDataEditorToken } from '@app/models/users/user-admin-responses';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import {
  ADMIN_USER_MANAGEMENT_STATE_USER_ADMIN_API_SERVICE_PORT,
  ADMIN_USER_MANAGEMENT_STATE_USERS_API_SERVICE_PORT,
  AdminUserManagementStateUserAdminApiServicePort,
  AdminUserManagementStateUsersApiServicePort,
} from './admin-user-management-state-data.ports';
import { AdminUserManagementStateFacade } from './admin-user-management-state.facade';

describe('AdminUserManagementStateFacade', () => {
  let facade: AdminUserManagementStateFacade;
  let userAdminPort: MockedObject<AdminUserManagementStateUserAdminApiServicePort>;
  let toastMessageService: MockedObject<ToastMessageService>;

  beforeEach(() => {
    const usersPort: MockedObject<AdminUserManagementStateUsersApiServicePort> = {
      getUserById: vi.fn(),
    } as unknown as MockedObject<AdminUserManagementStateUsersApiServicePort>;
    userAdminPort = {
      getParkDataEditorTokens: vi.fn(),
      revokeParkDataEditorToken: vi.fn(),
      revokeAllParkDataEditorTokens: vi.fn(),
    } as unknown as MockedObject<AdminUserManagementStateUserAdminApiServicePort>;
    toastMessageService = {
      add: vi.fn(),
    } as unknown as MockedObject<ToastMessageService>;
    const translateService: MockedObject<TranslateService> = {
      instant: vi.fn((key: string | string[]) => key),
    } as unknown as MockedObject<TranslateService>;

    TestBed.configureTestingModule({
      providers: [
        AdminUserManagementStateFacade,
        { provide: ADMIN_USER_MANAGEMENT_STATE_USERS_API_SERVICE_PORT, useValue: usersPort },
        { provide: ADMIN_USER_MANAGEMENT_STATE_USER_ADMIN_API_SERVICE_PORT, useValue: userAdminPort },
        { provide: ToastMessageService, useValue: toastMessageService },
        { provide: TranslateService, useValue: translateService },
        { provide: DestroyRef, useValue: { onDestroy: vi.fn() } },
      ],
    });

    facade = TestBed.inject(AdminUserManagementStateFacade);
  });

  it('loads park data editor tokens through the facade port', () => {
    const token: ParkDataEditorToken = createToken();
    userAdminPort.getParkDataEditorTokens.mockReturnValue(of([token]));

    facade.loadParkDataEditorTokens('user-1');

    expect(userAdminPort.getParkDataEditorTokens).toHaveBeenCalledWith('user-1');
    expect(facade.parkDataEditorTokens()).toEqual([token]);
    expect(facade.hasActiveParkDataEditorTokens()).toBe(true);
    expect(facade.loadingParkDataEditorTokens()).toBe(false);
  });

  it('revokes one token and refreshes the list', () => {
    userAdminPort.revokeParkDataEditorToken.mockReturnValue(of({ revokedCount: 1 }));
    userAdminPort.getParkDataEditorTokens.mockReturnValue(of([]));

    facade.revokeParkDataEditorToken('user-1', 'token-1');

    expect(userAdminPort.revokeParkDataEditorToken).toHaveBeenCalledWith('user-1', 'token-1');
    expect(userAdminPort.getParkDataEditorTokens).toHaveBeenCalledWith('user-1');
    expect(facade.revokingParkDataEditorTokenId()).toBeNull();
    expect(toastMessageService.add).toHaveBeenCalledWith(
      'success',
      'admin.users.parkDataEditorTokens.successTitle',
      'admin.users.parkDataEditorTokens.revoked');
  });

  it('clears the loading state and reports token loading failures', () => {
    userAdminPort.getParkDataEditorTokens.mockReturnValue(throwError(() => new Error('network')));

    facade.loadParkDataEditorTokens('user-1');

    expect(facade.parkDataEditorTokens()).toEqual([]);
    expect(facade.loadingParkDataEditorTokens()).toBe(false);
    expect(toastMessageService.add).toHaveBeenCalledWith(
      'error',
      'admin.users.parkDataEditorTokens.errorTitle',
      'admin.users.parkDataEditorTokens.loadError');
  });
});

function createToken(): ParkDataEditorToken {
  return {
    id: 'token-1',
    label: 'Codex',
    displayPrefix: 'apf_pde_12345678',
    createdAtUtc: '2026-08-05T00:00:00Z',
    expiresAtUtc: '2026-09-04T00:00:00Z',
    lastUsedAtUtc: null,
    revokedAtUtc: null,
    revokedByUserId: null,
    revocationReason: null,
    isActive: true,
  };
}
