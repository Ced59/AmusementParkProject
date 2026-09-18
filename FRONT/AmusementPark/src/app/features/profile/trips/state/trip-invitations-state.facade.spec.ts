import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';

import {
  TripInvitationCreation,
  TripInvitationList,
  TripInvitationSummary
} from '@app/models/trips/trip-invitation.models';
import {
  TRIP_INVITATIONS_DATA_PORT,
  TripInvitationsDataPort
} from '@features/trips/state/trip-invitation-data.port';
import {
  TRIP_OPERATION_ID_PORT,
  TripOperationIdPort
} from './trip-state-data.ports';
import { TripInvitationsStateFacade } from './trip-invitations-state.facade';

describe('TripInvitationsStateFacade', () => {
  let facade: TripInvitationsStateFacade;
  let data: TripInvitationsDataPort;
  let operationIds: TripOperationIdPort;

  beforeEach(() => {
    data = {
      list: vi.fn().mockReturnValue(of(createList())),
      create: vi.fn().mockReturnValue(of(createResult())),
      revoke: vi.fn().mockReturnValue(of(undefined)),
      preview: vi.fn(),
      accept: vi.fn(),
      decline: vi.fn()
    };
    operationIds = { create: vi.fn().mockReturnValue('operation-1') };
    TestBed.configureTestingModule({
      providers: [
        TripInvitationsStateFacade,
        { provide: TRIP_INVITATIONS_DATA_PORT, useValue: data },
        { provide: TRIP_OPERATION_ID_PORT, useValue: operationIds }
      ]
    });
    facade = TestBed.inject(TripInvitationsStateFacade);
    facade.load('trip-1', 4);
  });

  it('creates an opaque link with the chosen role and optional target', () => {
    facade.create('Editor', 24, ' guest@example.com ');

    expect(data.create).toHaveBeenCalledWith('trip-1', {
      expectedPlanVersion: 4,
      proposedRole: 'Editor',
      lifetimeHours: 24,
      targetEmail: 'guest@example.com'
    }, 'operation-1');
    expect(facade.creation()?.token).toBe('opaque-token');
    expect(facade.creation()?.inviterDisplayName).toBe('CoasterCamille');
    expect(facade.inviterDisplayName()).toBe('CoasterCamille');
    expect(facade.status()).toBe('idle');
  });

  it('revokes by the hidden action identifier and removes the visible card', () => {
    const invitation: TripInvitationSummary = createSummary();
    (data.list as ReturnType<typeof vi.fn>).mockReturnValue(of(createList([invitation])));
    facade.load('trip-1', 4);

    facade.revoke(invitation);

    expect(data.revoke).toHaveBeenCalledWith('trip-1', 'invitation-1', 2, 'operation-1');
    expect(facade.invitations()).toEqual([]);
  });

  it('hides the newly created link when that invitation is revoked', () => {
    const invitation: TripInvitationSummary = createSummary();
    facade.create('Participant', 24, '');

    facade.revoke(invitation);

    expect(facade.creation()).toBeNull();
  });

  it('reuses the idempotency key when the same creation is retried after an ambiguous failure', () => {
    (data.create as ReturnType<typeof vi.fn>)
      .mockReturnValueOnce(throwError((): Error => new Error('network')))
      .mockReturnValueOnce(of(createResult()));

    facade.create('Participant', 48, 'guest@example.com');
    facade.load('trip-1', 5);
    facade.create('Participant', 48, 'guest@example.com');

    expect(operationIds.create).toHaveBeenCalledTimes(1);
    expect(data.create).toHaveBeenNthCalledWith(1, 'trip-1', expect.any(Object), 'operation-1');
    expect(data.create).toHaveBeenNthCalledWith(2, 'trip-1', {
      expectedPlanVersion: 5,
      proposedRole: 'Participant',
      lifetimeHours: 48,
      targetEmail: 'guest@example.com'
    }, 'operation-1');
    expect(facade.creation()?.token).toBe('opaque-token');
  });

  it('uses a new idempotency key when the creation choices change', () => {
    (operationIds.create as ReturnType<typeof vi.fn>)
      .mockReturnValueOnce('operation-1')
      .mockReturnValueOnce('operation-2');
    (data.create as ReturnType<typeof vi.fn>)
      .mockReturnValueOnce(throwError((): Error => new Error('network')))
      .mockReturnValueOnce(of(createResult()));

    facade.create('Participant', 48, 'guest@example.com');
    facade.create('Viewer', 48, 'guest@example.com');

    expect(operationIds.create).toHaveBeenCalledTimes(2);
    expect(data.create).toHaveBeenNthCalledWith(2, 'trip-1', expect.any(Object), 'operation-2');
  });

  it('retains a newer plan version received while the invitation list is loading', () => {
    const pendingList = new Subject<TripInvitationList>();
    (data.list as ReturnType<typeof vi.fn>).mockReturnValue(pendingList);

    facade.load('trip-1', 5);
    facade.load('trip-1', 6);
    pendingList.next(createList());
    pendingList.complete();
    facade.create('Participant', 24, '');

    expect(data.create).toHaveBeenCalledWith('trip-1', {
      expectedPlanVersion: 6,
      proposedRole: 'Participant',
      lifetimeHours: 24,
      targetEmail: null
    }, 'operation-1');
  });

  it('ignores an older refresh that completes after an invitation was revoked', () => {
    const staleRefresh = new Subject<TripInvitationList>();
    const invitation: TripInvitationSummary = createSummary();
    (data.list as ReturnType<typeof vi.fn>).mockReturnValueOnce(staleRefresh);

    facade.create('Participant', 24, '');
    facade.revoke(invitation);
    staleRefresh.next(createList([invitation]));
    staleRefresh.complete();

    expect(facade.invitations()).toEqual([]);
  });
});

function createResult(): TripInvitationCreation {
  return {
    invitationId: 'invitation-1',
    token: 'opaque-token',
    inviterDisplayName: 'CoasterCamille',
    proposedRole: 'Participant',
    expiresAtUtc: '2027-06-08T08:00:00Z',
    isTargeted: false,
    wasReplayed: false
  };
}

function createList(invitations: TripInvitationSummary[] = []): TripInvitationList {
  return {
    inviterDisplayName: 'CoasterCamille',
    invitations
  };
}

function createSummary(): TripInvitationSummary {
  return {
    invitationId: 'invitation-1',
    tokenHint: 'hidden',
    proposedRole: 'Participant',
    expiresAtUtc: '2027-06-08T08:00:00Z',
    isTargeted: false,
    version: 2,
    createdAtUtc: '2027-06-01T08:00:00Z'
  };
}
