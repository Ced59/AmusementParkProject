import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';

import { TripDayPlan, TripParkCandidate, TripPlan, TripProgram } from '@app/models/trips/trip.models';
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
      putDay: vi.fn(),
      deleteDay: vi.fn().mockReturnValue(of(undefined))
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
      createEntry({ entryId: 'planned', targetId: 'park-2', kind: 'Planned' }),
      createEntry({ entryId: 'already-added', targetId: 'park-1' }),
      createEntry({ entryId: 'closed', targetId: 'park-3', targetStatus: 'PermanentlyClosed' }),
      createEntry({ entryId: 'favourite', targetId: 'park-4', kind: 'Favorite' }),
      createEntry({ entryId: 'attraction', targetId: 'item-1', targetType: 'ParkItem' })
    ]));

    facade.load('trip-1');

    expect(collections.listMine).toHaveBeenCalledWith('Park');
    expect(facade.wishlistParks().map((entry: UserCollectionEntry): string => entry.entryId)).toEqual(['planned']);
  });

  it('keeps the planner available when the auxiliary wishlist cannot load', () => {
    (collections.listMine as ReturnType<typeof vi.fn>).mockReturnValue(
      throwError(() => ({ status: 503 }))
    );

    facade.load('trip-1');

    expect(facade.status()).toBe('ready');
    expect(facade.trip()?.tripPlanId).toBe('trip-1');
    expect(facade.wishlistParks()).toEqual([]);
    expect(facade.wishlistUnavailable()).toBe(true);
  });

  it('imports several wishes sequentially with the refreshed plan version', () => {
    const first: UserCollectionEntry = createEntry({
      entryId: 'entry-1', targetId: 'park-2', preferredStartsOn: '2026-10-03', preferredEndsOn: '2026-10-03'
    });
    const second: UserCollectionEntry = createEntry({
      entryId: 'entry-2',
      targetId: 'park-3',
      privateNote: 'x'.repeat(2001),
      preferredStartsOn: '2026-11-01',
      preferredEndsOn: '2026-11-01'
    });
    const plannedDuplicate: UserCollectionEntry = createEntry({
      entryId: 'entry-1-planned',
      targetId: 'park-2',
      kind: 'Planned',
      privateNote: 'Planifié'
    });
    (plans.getMine as ReturnType<typeof vi.fn>)
      .mockReturnValueOnce(of(createTrip({ version: 1 })))
      .mockReturnValueOnce(of(createTrip({ version: 2 })))
      .mockReturnValueOnce(of(createTrip({ version: 3 })));
    (collections.listMine as ReturnType<typeof vi.fn>).mockReturnValue(of([first, second]));

    facade.load('trip-1');
    facade.importWishlist([first, plannedDuplicate, second]);

    expect(programData.addPark).toHaveBeenCalledTimes(2);
    expect((programData.addPark as ReturnType<typeof vi.fn>).mock.calls[0]).toEqual([
      'trip-1',
      {
        expectedPlanVersion: 1,
        parkId: 'park-2',
        candidateDates: [],
        source: 'Wishlist',
        collectiveNote: null
      },
      'operation-1'
    ]);
    expect((programData.addPark as ReturnType<typeof vi.fn>).mock.calls[1][1]).toMatchObject({
      expectedPlanVersion: 2,
      parkId: 'park-3',
      candidateDates: [],
      collectiveNote: null
    });
    expect(operationIds.create).toHaveBeenCalledTimes(2);
    expect(programData.get).toHaveBeenCalledTimes(2);
    expect(facade.trip()?.version).toBe(3);
    expect(facade.busy()).toBe(false);
  });

  it('keeps successfully imported parks visible when a later wishlist import fails', () => {
    const first: UserCollectionEntry = createEntry({ entryId: 'entry-1', targetId: 'park-2' });
    const second: UserCollectionEntry = createEntry({ entryId: 'entry-2', targetId: 'park-3' });
    (plans.getMine as ReturnType<typeof vi.fn>)
      .mockReturnValueOnce(of(createTrip({ version: 1 })))
      .mockReturnValueOnce(of(createTrip({ version: 2 })));
    (programData.addPark as ReturnType<typeof vi.fn>)
      .mockReturnValueOnce(of(createCandidate({ candidateId: 'candidate-2', parkId: 'park-2' })))
      .mockReturnValueOnce(throwError(() => ({ status: 400 })));

    facade.load('trip-1');
    facade.importWishlist([first, second]);

    expect(programData.addPark).toHaveBeenCalledTimes(2);
    expect(programData.get).toHaveBeenCalledTimes(1);
    expect(facade.program().candidates.map((candidate: TripParkCandidate): string => candidate.parkId))
      .toEqual(['park-2']);
    expect(facade.wishlistParks().map((entry: UserCollectionEntry): string => entry.targetId))
      .not.toContain('park-2');
    expect(facade.trip()?.version).toBe(2);
    expect(facade.actionError()).toBe('failed');
    expect(facade.busy()).toBe(false);
  });

  it('clears a saved day and refreshes the authoritative day drafts', () => {
    const day: TripDayPlan = createDay({ version: 4 });
    (programData.get as ReturnType<typeof vi.fn>)
      .mockReturnValueOnce(of(createProgram([], [day])))
      .mockReturnValueOnce(of(createProgram()));
    (plans.getMine as ReturnType<typeof vi.fn>)
      .mockReturnValueOnce(of(createTrip({ version: 7 })))
      .mockReturnValueOnce(of(createTrip({ version: 8 })));

    facade.load('trip-1');
    facade.clearDay('2026-10-03');

    expect(programData.deleteDay).toHaveBeenCalledWith('trip-1', '2026-10-03', 7, 4);
    expect(facade.program().days).toEqual([]);
    expect(facade.trip()?.version).toBe(8);
    expect(facade.clearedDay()).toEqual({ localDate: '2026-10-03', revision: 1 });
  });

  it('saves the explicit destination timezone instead of inferring it from the browser', () => {
    (plans.setDates as ReturnType<typeof vi.fn>).mockReturnValue(of(createTrip({
      version: 2,
      destinationTimeZoneId: 'Europe/Berlin'
    })));

    facade.load('trip-1');
    facade.setDates('2026-10-03', '2026-10-05', ' Europe/Berlin ');

    expect(plans.setDates).toHaveBeenCalledWith('trip-1', {
      expectedVersion: 1,
      dateProposal: {
        kind: 'Fixed',
        startDate: '2026-10-03',
        endDate: '2026-10-05',
        candidateDates: []
      },
      destinationTimeZoneId: 'Europe/Berlin'
    });
    expect(facade.dateDraftRevision()).toBe(1);
  });

  it('preserves local drafts after a deterministic validation failure', () => {
    (plans.setDates as ReturnType<typeof vi.fn>).mockReturnValue(
      throwError(() => ({ status: 400 }))
    );

    facade.load('trip-1');
    facade.setDates('2026-10-03', '2026-10-05', 'Invalid/Zone');

    expect(facade.actionError()).toBe('failed');
    expect(programData.get).toHaveBeenCalledTimes(1);
    expect(plans.getMine).toHaveBeenCalledTimes(1);
    expect(facade.recoveryRevision()).toBe(0);
    expect(facade.dateDraftRevision()).toBe(0);
  });

  it('reloads the latest plan and reports an optimistic conflict', () => {
    const candidate: TripParkCandidate = createCandidate();
    const recoveryProgram: Subject<TripProgram> = new Subject<TripProgram>();
    (programData.get as ReturnType<typeof vi.fn>)
      .mockReturnValueOnce(of(createProgram([candidate])))
      .mockReturnValueOnce(recoveryProgram);
    (plans.getMine as ReturnType<typeof vi.fn>)
      .mockReturnValueOnce(of(createTrip({ version: 1 })))
      .mockReturnValueOnce(of(createTrip({ version: 2 })));
    (programData.changeParkState as ReturnType<typeof vi.fn>).mockReturnValue(
      throwError(() => ({ status: 409 }))
    );

    facade.load('trip-1');
    facade.changeCandidateState(candidate, 'Selected');

    expect(facade.actionError()).toBe('conflict');
    expect(facade.busy()).toBe(true);

    recoveryProgram.next(createProgram([{ ...candidate, state: 'Selected', version: 2 }]));
    recoveryProgram.complete();

    expect(facade.trip()?.version).toBe(2);
    expect(facade.program().candidates[0].state).toBe('Selected');
    expect(facade.recoveryRevision()).toBe(1);
    expect(facade.dateDraftRevision()).toBe(1);
    expect(facade.busy()).toBe(false);
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

function createProgram(candidates: TripParkCandidate[] = [], days: TripDayPlan[] = []): TripProgram {
  return { candidates, days };
}

function createDay(overrides: Partial<TripDayPlan> = {}): TripDayPlan {
  return {
    dayPlanId: 'day-1', localDate: '2026-10-03', parkCandidateId: 'candidate-1', parkId: 'park-1',
    parkName: 'Parc A', isParkAvailable: true, desiredArrivalTime: '09:00', groupNote: null, blocks: [],
    version: 1, createdAtUtc: '2026-09-18T10:00:00Z', updatedAtUtc: '2026-09-18T10:00:00Z',
    ...overrides
  };
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
