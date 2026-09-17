import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { TripParkCandidate, TripPlan, TripProgram } from '@app/models/trips/trip.models';
import { UserCollectionEntry } from '@app/models/watchlists/user-collection-entry.model';
import {
  TRIP_COLLECTIONS_DATA_PORT,
  TRIP_OPERATION_ID_PORT,
  TRIP_PLANS_DATA_PORT,
  TRIP_PROGRAM_DATA_PORT,
  TripCollectionsDataPort,
  TripOperationIdPort,
  TripPlansDataPort,
  TripProgramDataPort
} from './trip-state-data.ports';
import { TripOverviewStateFacade } from './trip-overview-state.facade';

describe('TripOverviewStateFacade', () => {
  let facade: TripOverviewStateFacade;
  let plans: TripPlansDataPort;
  let programData: TripProgramDataPort;
  let collections: TripCollectionsDataPort;
  let operationIds: TripOperationIdPort;

  beforeEach(() => {
    plans = {
      listMine: vi.fn().mockReturnValue(of([])),
      getMine: vi.fn().mockReturnValue(of(createTrip())),
      create: vi.fn(),
      setDates: vi.fn(),
      delete: vi.fn()
    };
    programData = {
      get: vi.fn().mockReturnValue(of(createProgram())),
      addPark: vi.fn().mockReturnValue(of(createCandidate())),
      changeParkState: vi.fn().mockReturnValue(of(createCandidate())),
      movePark: vi.fn().mockReturnValue(of(createProgram())),
      putDay: vi.fn()
    };
    collections = { listMine: vi.fn().mockReturnValue(of([])) };
    operationIds = { create: vi.fn().mockReturnValueOnce('operation-1').mockReturnValueOnce('operation-2') };
    TestBed.configureTestingModule({
      providers: [
        TripOverviewStateFacade,
        { provide: TRIP_PLANS_DATA_PORT, useValue: plans },
        { provide: TRIP_PROGRAM_DATA_PORT, useValue: programData },
        { provide: TRIP_COLLECTIONS_DATA_PORT, useValue: collections },
        { provide: TRIP_OPERATION_ID_PORT, useValue: operationIds }
      ]
    });
    facade = TestBed.inject(TripOverviewStateFacade);
  });

  it('offers only available park wishes that are not already in the trip', () => {
    (programData.get as ReturnType<typeof vi.fn>).mockReturnValue(of(createProgram([createCandidate()])));
    (collections.listMine as ReturnType<typeof vi.fn>).mockReturnValue(of([
      createEntry({ entryId: 'eligible', targetId: 'park-2' }),
      createEntry({ entryId: 'already-added', targetId: 'park-1' }),
      createEntry({ entryId: 'closed', targetId: 'park-3', targetStatus: 'PermanentlyClosed' }),
      createEntry({ entryId: 'favourite', targetId: 'park-4', kind: 'Favorite' }),
      createEntry({ entryId: 'attraction', targetId: 'item-1', targetType: 'ParkItem' })
    ]));

    facade.load('trip-1');

    expect(collections.listMine).toHaveBeenCalledWith('Park');
    expect(facade.wishlistParks().map((entry: UserCollectionEntry): string => entry.entryId)).toEqual(['eligible']);
  });

  it('imports several wishes sequentially with the refreshed plan version', () => {
    const first: UserCollectionEntry = createEntry({
      entryId: 'entry-1', targetId: 'park-2', preferredStartsOn: '2026-10-03', preferredEndsOn: '2026-10-03'
    });
    const second: UserCollectionEntry = createEntry({ entryId: 'entry-2', targetId: 'park-3', privateNote: 'Immanquable' });
    (plans.getMine as ReturnType<typeof vi.fn>)
      .mockReturnValueOnce(of(createTrip({ version: 1 })))
      .mockReturnValueOnce(of(createTrip({ version: 2 })))
      .mockReturnValueOnce(of(createTrip({ version: 3 })));
    (collections.listMine as ReturnType<typeof vi.fn>).mockReturnValue(of([first, second]));

    facade.load('trip-1');
    facade.importWishlist([first, second]);

    expect(programData.addPark).toHaveBeenCalledTimes(2);
    expect((programData.addPark as ReturnType<typeof vi.fn>).mock.calls[0]).toEqual([
      'trip-1',
      {
        expectedPlanVersion: 1,
        parkId: 'park-2',
        candidateDates: ['2026-10-03'],
        source: 'Wishlist',
        collectiveNote: null
      },
      'operation-1'
    ]);
    expect((programData.addPark as ReturnType<typeof vi.fn>).mock.calls[1][1]).toMatchObject({
      expectedPlanVersion: 2,
      parkId: 'park-3',
      candidateDates: [],
      collectiveNote: 'Immanquable'
    });
    expect(operationIds.create).toHaveBeenCalledTimes(2);
    expect(programData.get).toHaveBeenCalledTimes(2);
    expect(facade.trip()?.version).toBe(3);
    expect(facade.busy()).toBe(false);
  });

  it('reloads the latest plan and reports an optimistic conflict', () => {
    const candidate: TripParkCandidate = createCandidate();
    (programData.get as ReturnType<typeof vi.fn>)
      .mockReturnValueOnce(of(createProgram([candidate])))
      .mockReturnValueOnce(of(createProgram([{ ...candidate, state: 'Selected', version: 2 }])));
    (plans.getMine as ReturnType<typeof vi.fn>)
      .mockReturnValueOnce(of(createTrip({ version: 1 })))
      .mockReturnValueOnce(of(createTrip({ version: 2 })));
    (programData.changeParkState as ReturnType<typeof vi.fn>).mockReturnValue(
      throwError(() => ({ status: 409 }))
    );

    facade.load('trip-1');
    facade.changeCandidateState(candidate, 'Selected');

    expect(facade.actionError()).toBe('conflict');
    expect(facade.trip()?.version).toBe(2);
    expect(facade.program().candidates[0].state).toBe('Selected');
  });
});

function createTrip(overrides: Partial<TripPlan> = {}): TripPlan {
  return {
    tripPlanId: 'trip-1', title: 'Voyage',
    dateProposal: { kind: 'Fixed', startDate: '2026-10-03', endDate: '2026-10-04', candidateDates: [] },
    destinationTimeZoneId: 'Europe/Paris', status: 'Draft', accessScope: 'Private', memberCount: 1,
    isOwner: true, createdAtUtc: '2026-09-18T10:00:00Z', updatedAtUtc: '2026-09-18T10:00:00Z', version: 1,
    ...overrides
  };
}

function createCandidate(overrides: Partial<TripParkCandidate> = {}): TripParkCandidate {
  return {
    candidateId: 'candidate-1', parkId: 'park-1', parkName: 'Parc A', isParkAvailable: true,
    candidateDates: [], source: 'Manual', state: 'Proposed', collectiveNote: null, fitSnapshot: null,
    sortPosition: 0, version: 1, createdAtUtc: '2026-09-18T10:00:00Z', updatedAtUtc: '2026-09-18T10:00:00Z',
    ...overrides
  };
}

function createProgram(candidates: TripParkCandidate[] = []): TripProgram {
  return { candidates, days: [] };
}

function createEntry(overrides: Partial<UserCollectionEntry> = {}): UserCollectionEntry {
  return {
    entryId: 'entry-1', targetType: 'Park', targetId: 'park-2', kind: 'WantToVisit', targetStatus: 'Available',
    targetName: 'Parc B', parentParkId: null, parentParkName: null, mainImageId: null, privateNote: null,
    priority: null, preferredStartsOn: null, preferredEndsOn: null, createdAtUtc: '2026-09-18T10:00:00Z',
    updatedAtUtc: '2026-09-18T10:00:00Z', version: 1,
    ...overrides
  };
}
