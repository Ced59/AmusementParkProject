import { DestroyRef } from '@angular/core';
import { of, throwError } from 'rxjs';

import { TripExport } from '@app/models/trips/trip-export.models';
import { TripExportDataPort } from './trip-export-data.port';
import { TripExportFacade } from './trip-export.facade';
import { TripOperationIdPort } from './trip-state-data.ports';

describe('TripExportFacade', () => {
  it('loads a fresh audited export request and exposes the portable plan', () => {
    const plan: TripExport = createPlan();
    const data: TripExportDataPort = { get: vi.fn().mockReturnValue(of(plan)) };
    const operationIds: TripOperationIdPort = { create: vi.fn().mockReturnValue('request-1') };
    const facade: TripExportFacade = createFacade(data, operationIds);

    facade.load(' trip-1 ');

    expect(data.get).toHaveBeenCalledWith('trip-1', 'request-1');
    expect(facade.plan()).toEqual(plan);
    expect(facade.error()).toBe(false);
  });

  it('clears a stale plan when loading fails', () => {
    const data: TripExportDataPort = {
      get: vi.fn()
        .mockReturnValueOnce(of(createPlan()))
        .mockReturnValueOnce(throwError(() => new Error('offline')))
    };
    const operationIds: TripOperationIdPort = {
      create: vi.fn()
        .mockReturnValueOnce('request-1')
        .mockReturnValueOnce('request-2')
    };
    const facade: TripExportFacade = createFacade(data, operationIds);

    facade.load('trip-1');
    facade.retry();

    expect(facade.plan()).toBeNull();
    expect(facade.error()).toBe(true);
  });
});

function createFacade(
  data: TripExportDataPort,
  operationIds: TripOperationIdPort
): TripExportFacade {
  const destroyRef: DestroyRef = {
    onDestroy: (): (() => void) => (): void => undefined,
    destroyed: false
  } as unknown as DestroyRef;
  return new TripExportFacade(data, operationIds, destroyRef);
}

function createPlan(): TripExport {
  return {
    schemaVersion: 'trip-plan-export-v1',
    title: 'Voyage test',
    dateProposal: { kind: 'None', startDate: null, endDate: null, candidateDates: [] },
    destinationTimeZoneId: null,
    status: 'Planning',
    generatedAtUtc: '2027-08-12T09:30:00Z',
    candidateParks: [],
    days: [],
    collectiveDecisions: []
  };
}
