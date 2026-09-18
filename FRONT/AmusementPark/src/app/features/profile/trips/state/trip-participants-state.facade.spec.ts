import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { TripParticipantList } from '@app/models/trips/trip-participant.models';
import {
  TRIP_PARTICIPANTS_DATA_PORT,
  TripParticipantsDataPort
} from './trip-state-data.ports';
import { TripParticipantsStateFacade } from './trip-participants-state.facade';

describe('TripParticipantsStateFacade', () => {
  let facade: TripParticipantsStateFacade;
  let data: TripParticipantsDataPort;

  beforeEach(() => {
    data = {
      list: vi.fn().mockReturnValue(of(createList())),
      changeRole: vi.fn().mockReturnValue(of(createList(5))),
      transferOwnership: vi.fn().mockReturnValue(of(createList(6))),
      leave: vi.fn().mockReturnValue(of(undefined))
    };
    TestBed.configureTestingModule({
      providers: [
        TripParticipantsStateFacade,
        { provide: TRIP_PARTICIPANTS_DATA_PORT, useValue: data }
      ]
    });
    facade = TestBed.inject(TripParticipantsStateFacade);
    facade.load('trip-1');
  });

  it('loads the participant aliases and server-calculated capabilities', () => {
    expect(facade.participants().map(participant => participant.displayName)).toEqual(['Camille', 'Alex']);
    expect(facade.canManageRoles()).toBe(true);
    expect(facade.canTransferOwnership()).toBe(true);
    expect(facade.canLeave()).toBe(false);
  });

  it('uses the latest trip version when changing a role', () => {
    facade.changeRole('member-2', 'Viewer');

    expect(data.changeRole).toHaveBeenCalledWith('trip-1', 'member-2', 'Viewer', 4);
  });

  it('signals the overview to refresh its permissions after ownership transfer', () => {
    facade.transferOwnership('member-2', 'Viewer');

    expect(data.transferOwnership).toHaveBeenCalledWith('trip-1', 'member-2', 'Viewer', 4);
    expect(facade.ownershipTransferRevision()).toBe(1);
  });

  it('marks the collaboration as left only after the server confirms departure', () => {
    vi.mocked(data.list).mockReturnValue(of({ ...createList(), canLeave: true }));
    facade.load('trip-1');

    facade.leave();

    expect(data.leave).toHaveBeenCalledWith('trip-1', 4);
    expect(facade.left()).toBe(true);
  });
});

function createList(tripVersion: number = 4): TripParticipantList {
  return {
    participants: [
      {
        memberId: 'member-1',
        displayName: 'Camille',
        role: 'Owner',
        isCurrentUser: true,
        joinedAtUtc: '2027-06-01T08:00:00Z'
      },
      {
        memberId: 'member-2',
        displayName: 'Alex',
        role: 'Participant',
        isCurrentUser: false,
        joinedAtUtc: '2027-06-01T09:00:00Z'
      }
    ],
    canManageRoles: true,
    canTransferOwnership: true,
    canLeave: false,
    tripVersion
  };
}
