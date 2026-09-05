import { HttpErrorResponse } from '@angular/common/http';
import { DestroyRef } from '@angular/core';
import { of, Subject, throwError } from 'rxjs';

import { PassportRideOccurrence } from '@app/models/passport/passport-ride-occurrence.models';
import { PassportVisit } from '@app/models/passport/passport-visit.models';
import { AuthService } from '@app/services/auth/auth.service';
import { ParkItemPassportRideDraft } from '../models/park-item-passport-ride.models';
import {
  ParkItemPassportRideOccurrencesPort,
  ParkItemPassportRideOperationIdPort,
  ParkItemPassportRideVisitsPort
} from './park-item-passport-ride-state-data.ports';
import { ParkItemPassportRideStateFacade } from './park-item-passport-ride-state.facade';

describe('ParkItemPassportRideStateFacade', () => {
  it('loads only draft visits for the current park and requires an explicit selection', () => {
    const dependencies = createDependencies();
    dependencies.visits.listVisits = vi.fn().mockReturnValue(of({
      items: [
        createVisit(),
        createVisit({ id: 'completed', status: 'Completed' }),
        createVisit({ id: 'other-park', parkId: 'park-2' })
      ],
      nextCursor: null
    }));
    const facade: ParkItemPassportRideStateFacade = createFacade(dependencies);

    configureAndLoad(facade);

    expect(dependencies.visits.listVisits).toHaveBeenCalledWith(20, null, {
      parkId: 'park-1',
      status: 'Draft'
    });
    expect(facade.visits().map((visit) => visit.id)).toEqual(['visit-1']);
    expect(facade.selectedVisitId()).toBeNull();
  });

  it('evaluates the chosen visit before adding an idempotent ride batch', () => {
    const dependencies = createDependencies();
    const facade: ParkItemPassportRideStateFacade = createFacade(dependencies);
    configureAndLoad(facade);

    facade.selectVisit('visit-1');
    facade.addRide(createDraft());

    expect(dependencies.occurrences.evaluateVisitTargets).toHaveBeenCalledWith('visit-1', ['ride-1']);
    expect(dependencies.occurrences.addBatch).toHaveBeenCalledWith('visit-1', {
      items: [expect.objectContaining({
        parkItemId: 'ride-1',
        count: 2,
        status: 'Completed'
      })]
    }, 'operation-1');
    expect(facade.outcome()).toBe('rideSaved');
    expect(facade.createdVisitId()).toBe('visit-1');
  });

  it('applies an optional private rating to every newly added occurrence', () => {
    const dependencies = createDependencies();
    dependencies.occurrences.addBatch = vi.fn().mockReturnValue(of({
      occurrences: [createOccurrence('occurrence-1')],
      wasReplayed: false,
      wasOrderNormalized: false
    }));
    const facade: ParkItemPassportRideStateFacade = createFacade(dependencies);
    configureAndLoad(facade);
    facade.selectVisit('visit-1');

    facade.addRide({ ...createDraft(), count: 1, rating: 4.5 });

    expect(dependencies.occurrences.upsertAssessment).toHaveBeenCalledTimes(1);
    expect(dependencies.occurrences.upsertAssessment).toHaveBeenCalledWith('occurrence-1', {
      value: 4.5,
      privateComment: null,
      expectedVersion: 1
    });
    expect(facade.outcome()).toBe('rideAndRatingSaved');
  });

  it('keeps grouped ride creation to one batch without multiplying rating requests', () => {
    const dependencies = createDependencies();
    const facade: ParkItemPassportRideStateFacade = createFacade(dependencies);
    configureAndLoad(facade);
    facade.selectVisit('visit-1');

    facade.addRide({ ...createDraft(), rating: 4.5 });

    expect(dependencies.occurrences.addBatch).not.toHaveBeenCalled();
    expect(dependencies.occurrences.upsertAssessment).not.toHaveBeenCalled();
    expect(facade.errorKey()).toBe('parkItems.passportRide.errors.multipleRating');
  });

  it('requires explicit confirmation when the attraction dates conflict with the visit', () => {
    const dependencies = createDependencies();
    dependencies.occurrences.evaluateVisitTargets = vi.fn().mockReturnValue(of([{
      parkItemId: 'ride-1',
      historicalConsistency: 'ConfirmedConflict',
      openingDate: '2025-01-01',
      closingDate: null
    }]));
    const facade: ParkItemPassportRideStateFacade = createFacade(dependencies);
    configureAndLoad(facade);
    facade.selectVisit('visit-1');

    facade.addRide(createDraft());

    expect(dependencies.occurrences.addBatch).not.toHaveBeenCalled();
    expect(facade.errorKey()).toBe('parkItems.passportRide.errors.confirmConflict');
  });

  it('reuses the operation key after an ambiguous network failure', () => {
    const dependencies = createDependencies();
    dependencies.occurrences.addBatch = vi.fn()
      .mockReturnValueOnce(throwError(() => new HttpErrorResponse({ status: 0 })))
      .mockReturnValueOnce(of({
        occurrences: [createOccurrence('occurrence-1')],
        wasReplayed: true,
        wasOrderNormalized: false
      }));
    const facade: ParkItemPassportRideStateFacade = createFacade(dependencies);
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
    configureAndLoad(facade);
    facade.selectVisit('visit-1');

    facade.addRide(createDraft());
    facade.addRide(createDraft());

    const calls = vi.mocked(dependencies.occurrences.addBatch).mock.calls;
    expect(calls[0][2]).toBe('operation-1');
    expect(calls[1][2]).toBe('operation-1');
    expect(dependencies.operationIds.create).toHaveBeenCalledTimes(1);
    expect(facade.outcome()).toBe('rideSaved');
  });

  it('ignores a stale target evaluation after another visit is selected', () => {
    const firstEvaluation = new Subject<Array<{
      parkItemId: string;
      historicalConsistency: 'Verified';
      openingDate: null;
      closingDate: null;
    }>>();
    const dependencies = createDependencies();
    dependencies.visits.listVisits = vi.fn().mockReturnValue(of({
      items: [createVisit(), createVisit({ id: 'visit-2' })],
      nextCursor: null
    }));
    dependencies.occurrences.evaluateVisitTargets = vi.fn()
      .mockReturnValueOnce(firstEvaluation)
      .mockReturnValueOnce(of([{
        parkItemId: 'ride-1',
        historicalConsistency: 'Unverified',
        openingDate: null,
        closingDate: null
      }]));
    const facade: ParkItemPassportRideStateFacade = createFacade(dependencies);
    configureAndLoad(facade);

    facade.selectVisit('visit-1');
    facade.selectVisit('visit-2');
    firstEvaluation.next([{
      parkItemId: 'ride-1',
      historicalConsistency: 'Verified',
      openingDate: null,
      closingDate: null
    }]);

    expect(facade.selectedVisitId()).toBe('visit-2');
    expect(facade.evaluation()?.consistency).toBe('Unverified');
  });
});

interface Dependencies {
  visits: ParkItemPassportRideVisitsPort;
  occurrences: ParkItemPassportRideOccurrencesPort;
  operationIds: ParkItemPassportRideOperationIdPort;
  auth: AuthService;
}

function createDependencies(): Dependencies {
  return {
    visits: {
      listVisits: vi.fn().mockReturnValue(of({ items: [createVisit()], nextCursor: null }))
    },
    occurrences: {
      evaluateVisitTargets: vi.fn().mockReturnValue(of([{
        parkItemId: 'ride-1',
        historicalConsistency: 'Verified',
        openingDate: null,
        closingDate: null
      }])),
      addBatch: vi.fn().mockReturnValue(of({
        occurrences: [createOccurrence('occurrence-1'), createOccurrence('occurrence-2')],
        wasReplayed: false,
        wasOrderNormalized: false
      })),
      upsertAssessment: vi.fn().mockImplementation((_occurrenceId: string) => of(createOccurrence('rated')))
    },
    operationIds: {
      create: vi.fn().mockReturnValue('operation-1')
    },
    auth: {
      isLoggedIn: vi.fn().mockReturnValue(true)
    } as unknown as AuthService
  };
}

function createFacade(dependencies: Dependencies): ParkItemPassportRideStateFacade {
  const destroyRef = {
    onDestroy: (): (() => void) => (): void => undefined
  } as unknown as DestroyRef;
  return new ParkItemPassportRideStateFacade(
    dependencies.visits,
    dependencies.occurrences,
    dependencies.operationIds,
    dependencies.auth,
    destroyRef);
}

function configureAndLoad(facade: ParkItemPassportRideStateFacade): void {
  facade.configure({
    parkItemId: 'ride-1',
    parkItemName: 'Le Grand Huit',
    parkId: 'park-1',
    parkName: 'Parc test',
    language: 'fr'
  });
  facade.load();
}

function createDraft(): ParkItemPassportRideDraft {
  return {
    visitId: 'visit-1',
    count: 2,
    status: 'Completed',
    localTime: '',
    isApproximate: false,
    rating: null,
    confirmHistoricalConflict: false
  };
}

function createVisit(overrides: Partial<PassportVisit> = {}): PassportVisit {
  return {
    id: 'visit-1',
    parkId: 'park-1',
    parkName: 'Parc test',
    date: { year: 2026, month: 9, day: 5, precision: 'Day', isApproximate: false },
    timeZoneId: 'Europe/Paris',
    serviceDayConvention: 'VisitStartLocalDate',
    status: 'Draft',
    privacy: 'Private',
    title: null,
    privateNote: null,
    version: 1,
    createdAtUtc: '2026-09-05T10:00:00Z',
    updatedAtUtc: '2026-09-05T10:00:00Z',
    completedAtUtc: null,
    ...overrides
  };
}

function createOccurrence(id: string): PassportRideOccurrence {
  return {
    id,
    visitId: 'visit-1',
    parkId: 'park-1',
    parkItemId: 'ride-1',
    sortPosition: 1024,
    moment: { localTime: null, isApproximate: false },
    status: 'Completed',
    source: 'Manual',
    historicalConsistency: 'Verified',
    privateNote: null,
    countsAsRide: true,
    version: 1,
    createdAtUtc: '2026-09-05T10:00:00Z',
    updatedAtUtc: '2026-09-05T10:00:00Z'
  };
}
