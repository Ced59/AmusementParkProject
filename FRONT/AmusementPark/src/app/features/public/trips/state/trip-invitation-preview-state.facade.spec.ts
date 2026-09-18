import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';

import { TripInvitationPreview } from '@app/models/trips/trip-invitation.models';
import {
  TRIP_INVITATIONS_DATA_PORT,
  TRIP_INVITATION_OPERATION_ID_PORT,
  TripInvitationsDataPort
} from '@features/trips/state/trip-invitation-data.port';
import { AuthService } from '@app/services/auth/auth.service';
import { TripInvitationPreviewStateFacade } from './trip-invitation-preview-state.facade';
import { TripInvitationDecisionOperationStore } from './trip-invitation-decision-operation.store';

describe('TripInvitationPreviewStateFacade', () => {
  let facade: TripInvitationPreviewStateFacade;
  let data: TripInvitationsDataPort;
  let loggedIn: boolean;
  let currentUserId: string;
  let decisionOperations: {
    read: ReturnType<typeof vi.fn>;
    write: ReturnType<typeof vi.fn>;
    clear: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    loggedIn = false;
    currentUserId = 'user-1';
    decisionOperations = {
      read: vi.fn().mockReturnValue(null),
      write: vi.fn(),
      clear: vi.fn()
    };
    data = {
      list: vi.fn(),
      create: vi.fn(),
      revoke: vi.fn(),
      preview: vi.fn().mockReturnValue(of(createPreview())),
      accept: vi.fn(),
      decline: vi.fn()
    };
    TestBed.configureTestingModule({
      providers: [
        TripInvitationPreviewStateFacade,
        { provide: TRIP_INVITATIONS_DATA_PORT, useValue: data },
        { provide: TRIP_INVITATION_OPERATION_ID_PORT, useValue: { create: (): string => 'operation-1' } },
        { provide: TripInvitationDecisionOperationStore, useValue: decisionOperations },
        {
          provide: AuthService,
          useValue: {
            isLoggedIn: (): boolean => loggedIn,
            getUserIdFromToken: (): string | null => loggedIn ? currentUserId : null
          }
        }
      ]
    });
    facade = TestBed.inject(TripInvitationPreviewStateFacade);
  });

  it('loads only the deliberately minimized public preview', () => {
    facade.load('opaque-token');

    expect(data.preview).toHaveBeenCalledWith('opaque-token');
    expect(facade.preview()).toEqual(createPreview());
    expect(facade.status()).toBe('ready');
    expect(Object.keys(facade.preview() ?? {})).not.toContain('tripPlanId');
  });

  it('uses one neutral unavailable state for every rejected token', () => {
    (data.preview as ReturnType<typeof vi.fn>).mockReturnValue(throwError(() => ({ status: 404 })));

    facade.load('unknown-token');

    expect(facade.preview()).toBeNull();
    expect(facade.status()).toBe('unavailable');
  });

  it('ignores an older preview response after the route token changes', () => {
    const firstPreview = new Subject<TripInvitationPreview>();
    const secondPreview: TripInvitationPreview = {
      ...createPreview(),
      tripTitle: 'Second voyage'
    };
    (data.preview as ReturnType<typeof vi.fn>)
      .mockReturnValueOnce(firstPreview)
      .mockReturnValueOnce(of(secondPreview));

    facade.load('first-token');
    facade.load('second-token');
    firstPreview.next(createPreview());

    expect(facade.preview()).toEqual(secondPreview);
    expect(facade.status()).toBe('ready');
  });

  it('accepts the retained token with a stable client operation id', () => {
    loggedIn = true;
    (data.accept as ReturnType<typeof vi.fn>).mockReturnValue(of({ tripPlanId: 'trip-1', wasReplayed: false }));
    facade.load('opaque-token');

    facade.accept();

    expect(data.accept).toHaveBeenCalledWith('opaque-token', 'operation-1');
    expect(decisionOperations.write).toHaveBeenCalledWith(
      'opaque-token',
      'user-1',
      'accept',
      'operation-1'
    );
    expect(decisionOperations.clear).toHaveBeenCalledWith('opaque-token', 'user-1');
    expect(facade.tripPlanId()).toBe('trip-1');
    expect(facade.status()).toBe('accepted');
  });

  it('resumes a pending acceptance after a page reload before requesting the terminal preview', () => {
    loggedIn = true;
    decisionOperations.read.mockReturnValue({ decision: 'accept', operationId: 'persisted-operation' });
    (data.accept as ReturnType<typeof vi.fn>).mockReturnValue(of({ tripPlanId: 'trip-1', wasReplayed: true }));

    facade.load('opaque-token');

    expect(data.preview).not.toHaveBeenCalled();
    expect(decisionOperations.read).toHaveBeenCalledWith('opaque-token', 'user-1');
    expect(data.accept).toHaveBeenCalledWith('opaque-token', 'persisted-operation');
    expect(decisionOperations.clear).toHaveBeenCalledWith('opaque-token', 'user-1');
    expect(facade.tripPlanId()).toBe('trip-1');
    expect(facade.status()).toBe('accepted');
  });

  it('resumes the account-bound decision after an in-place login before creating another operation', () => {
    facade.load('opaque-token');
    expect(decisionOperations.read).not.toHaveBeenCalled();

    loggedIn = true;
    decisionOperations.read.mockReturnValue({ decision: 'accept', operationId: 'persisted-operation' });
    (data.accept as ReturnType<typeof vi.fn>).mockReturnValue(
      of({ tripPlanId: 'trip-1', wasReplayed: true })
    );

    facade.decline();

    expect(decisionOperations.read).toHaveBeenCalledWith('opaque-token', 'user-1');
    expect(data.accept).toHaveBeenCalledWith('opaque-token', 'persisted-operation');
    expect(data.decline).not.toHaveBeenCalled();
    expect(decisionOperations.write).toHaveBeenCalledWith(
      'opaque-token',
      'user-1',
      'accept',
      'persisted-operation'
    );
    expect(facade.status()).toBe('accepted');
  });

  it('recovers the public preview when a resumed decision fails', () => {
    loggedIn = true;
    decisionOperations.read.mockReturnValue({ decision: 'accept', operationId: 'persisted-operation' });
    (data.accept as ReturnType<typeof vi.fn>).mockReturnValue(
      throwError(() => ({ status: 503 }))
    );

    facade.load('opaque-token');

    expect(data.accept).toHaveBeenCalledWith('opaque-token', 'persisted-operation');
    expect(data.preview).toHaveBeenCalledWith('opaque-token');
    expect(decisionOperations.clear).not.toHaveBeenCalled();
    expect(facade.preview()).toEqual(createPreview());
    expect(facade.status()).toBe('decision-error');
  });

  it('retries the original decision after an ambiguous error instead of replacing it', () => {
    loggedIn = true;
    (data.accept as ReturnType<typeof vi.fn>)
      .mockReturnValueOnce(throwError(() => ({ status: 503 })))
      .mockReturnValueOnce(of({ tripPlanId: 'trip-1', wasReplayed: true }));
    facade.load('opaque-token');

    facade.accept();
    expect(facade.status()).toBe('decision-error');

    facade.decline();

    expect(data.accept).toHaveBeenNthCalledWith(1, 'opaque-token', 'operation-1');
    expect(data.accept).toHaveBeenNthCalledWith(2, 'opaque-token', 'operation-1');
    expect(data.decline).not.toHaveBeenCalled();
    expect(decisionOperations.write).toHaveBeenLastCalledWith(
      'opaque-token',
      'user-1',
      'accept',
      'operation-1'
    );
    expect(facade.status()).toBe('accepted');
  });

  it('does not reuse an in-memory decision after the authenticated account changes', () => {
    loggedIn = true;
    (data.accept as ReturnType<typeof vi.fn>).mockReturnValue(
      throwError(() => ({ status: 503 }))
    );
    (data.decline as ReturnType<typeof vi.fn>).mockReturnValue(
      of({ tripPlanId: 'trip-1', wasReplayed: false })
    );
    facade.load('opaque-token');
    facade.accept();

    currentUserId = 'user-2';
    facade.decline();

    expect(decisionOperations.read).toHaveBeenLastCalledWith('opaque-token', 'user-2');
    expect(data.accept).toHaveBeenCalledOnce();
    expect(data.decline).toHaveBeenCalledWith('opaque-token', 'operation-1');
    expect(decisionOperations.write).toHaveBeenLastCalledWith(
      'opaque-token',
      'user-2',
      'decline',
      'operation-1'
    );
    expect(facade.status()).toBe('declined');
  });

  it('ignores a decision response completed after the authenticated account changes', () => {
    loggedIn = true;
    const firstDecision = new Subject<{ tripPlanId: string; wasReplayed: boolean }>();
    (data.accept as ReturnType<typeof vi.fn>).mockReturnValue(firstDecision);
    (data.decline as ReturnType<typeof vi.fn>).mockReturnValue(
      of({ tripPlanId: 'trip-2', wasReplayed: false })
    );
    facade.load('opaque-token');
    facade.accept();

    currentUserId = 'user-2';
    firstDecision.next({ tripPlanId: 'trip-1', wasReplayed: false });

    expect(facade.tripPlanId()).toBeNull();
    expect(facade.status()).toBe('decision-error');

    facade.decline();

    expect(data.decline).toHaveBeenCalledWith('opaque-token', 'operation-1');
    expect(facade.tripPlanId()).toBe('trip-2');
    expect(facade.status()).toBe('declined');
  });

  it('does not resume another account decision in a shared tab', () => {
    loggedIn = true;
    currentUserId = 'user-2';
    decisionOperations.read.mockReturnValue(null);

    facade.load('opaque-token');

    expect(decisionOperations.read).toHaveBeenCalledWith('opaque-token', 'user-2');
    expect(data.accept).not.toHaveBeenCalled();
    expect(data.preview).toHaveBeenCalledWith('opaque-token');
    expect(facade.status()).toBe('ready');
  });

  it('ignores an acceptance response from the invitation shown before a route change', () => {
    loggedIn = true;
    const firstDecision = new Subject<{ tripPlanId: string; wasReplayed: boolean }>();
    (data.accept as ReturnType<typeof vi.fn>).mockReturnValue(firstDecision);
    facade.load('first-token');
    facade.accept();

    facade.load('second-token');
    firstDecision.next({ tripPlanId: 'first-trip', wasReplayed: false });

    expect(facade.preview()).toEqual(createPreview());
    expect(facade.tripPlanId()).toBeNull();
    expect(facade.status()).toBe('ready');
  });

  it('ignores a decision error from the invitation shown before a route change', () => {
    loggedIn = true;
    const firstDecision = new Subject<{ tripPlanId: string; wasReplayed: boolean }>();
    (data.decline as ReturnType<typeof vi.fn>).mockReturnValue(firstDecision);
    facade.load('first-token');
    facade.decline();

    facade.load('second-token');
    firstDecision.error(new Error('late failure'));

    expect(facade.status()).toBe('ready');
  });
});

function createPreview(): TripInvitationPreview {
  return {
    tripTitle: 'Voyage été',
    inviterDisplayName: 'Camille',
    proposedRole: 'Participant',
    periodKind: 'SingleMonth',
    startMonth: '2027-07',
    endMonth: '2027-07',
    memberCountBand: 'TwoToFive',
    expiresAtUtc: '2027-06-08T08:00:00Z',
    isTargeted: false
  };
}
