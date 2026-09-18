import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { TripPlan, TripPlanWriteRequest } from '@app/models/trips/trip.models';
import {
  TRIP_OPERATION_ID_PORT,
  TRIP_PLANS_DATA_PORT,
  TripOperationIdPort,
  TripPlansDataPort
} from './trip-state-data.ports';
import { TripListStateFacade } from './trip-list-state.facade';

describe('TripListStateFacade', () => {
  let facade: TripListStateFacade;
  let plans: TripPlansDataPort;
  let operationIds: TripOperationIdPort;

  beforeEach(() => {
    plans = {
      listMine: vi.fn().mockReturnValue(of([])),
      getMine: vi.fn(),
      create: vi.fn().mockReturnValue(of(createTrip())),
      setDates: vi.fn(),
      delete: vi.fn()
    };
    operationIds = { create: vi.fn().mockReturnValue('operation-1') };
    TestBed.configureTestingModule({
      providers: [
        TripListStateFacade,
        { provide: TRIP_PLANS_DATA_PORT, useValue: plans },
        { provide: TRIP_OPERATION_ID_PORT, useValue: operationIds }
      ]
    });
    facade = TestBed.inject(TripListStateFacade);
  });

  it('loads the current user private trips', () => {
    (plans.listMine as ReturnType<typeof vi.fn>).mockReturnValue(of([createTrip()]));

    facade.load();

    expect(facade.status()).toBe('ready');
    expect(facade.trips().map((trip: TripPlan): string => trip.tripPlanId)).toEqual(['trip-1']);
  });

  it('creates a fixed trip with a fresh operation key and exposes the created id', () => {
    facade.create('  Voyage Allemagne  ', '2026-10-03', '2026-10-05', 'Europe/Berlin');

    expect(operationIds.create).toHaveBeenCalledTimes(1);
    expect(plans.create).toHaveBeenCalledTimes(1);
    const call: unknown[] = (plans.create as ReturnType<typeof vi.fn>).mock.calls[0];
    const request: TripPlanWriteRequest = call[0] as TripPlanWriteRequest;
    expect(request.title).toBe('Voyage Allemagne');
    expect(request.dateProposal).toEqual({
      kind: 'Fixed',
      startDate: '2026-10-03',
      endDate: '2026-10-05',
      candidateDates: []
    });
    expect(request.destinationTimeZoneId).toBe('Europe/Berlin');
    expect(call[1]).toBe('operation-1');
    expect(facade.createdTripId()).toBe('trip-1');
    expect(facade.creating()).toBe(false);
  });

  it('does not create a trip whose title is empty', () => {
    facade.create('   ', '', '', '');

    expect(plans.create).not.toHaveBeenCalled();
  });

  it('does not discard an end date entered without a start date', () => {
    facade.create('Voyage Allemagne', '', '2026-10-05', 'Europe/Berlin');

    expect(plans.create).not.toHaveBeenCalled();
    expect(operationIds.create).not.toHaveBeenCalled();
  });

  it('reuses the creation key when the same request is retried after an ambiguous failure', () => {
    (plans.create as ReturnType<typeof vi.fn>)
      .mockReturnValueOnce(throwError(() => ({ status: 0 })))
      .mockReturnValueOnce(of(createTrip()));

    facade.create('Voyage Allemagne', '2026-10-03', '2026-10-05', 'Europe/Berlin');
    facade.create('Voyage Allemagne', '2026-10-03', '2026-10-05', 'Europe/Berlin');

    expect(operationIds.create).toHaveBeenCalledTimes(1);
    expect((plans.create as ReturnType<typeof vi.fn>).mock.calls.map((call: unknown[]): unknown => call[1]))
      .toEqual(['operation-1', 'operation-1']);
    expect(facade.createdTripId()).toBe('trip-1');
  });

  it('does not silently use the browser zone when a dated trip has no destination timezone', () => {
    facade.create('Voyage Allemagne', '2026-10-03', '2026-10-05', '   ');

    expect(plans.create).not.toHaveBeenCalled();
    expect(operationIds.create).not.toHaveBeenCalled();
  });
});

function createTrip(overrides: Partial<TripPlan> = {}): TripPlan {
  return {
    tripPlanId: 'trip-1',
    title: 'Voyage Allemagne',
    dateProposal: { kind: 'None', startDate: null, endDate: null, candidateDates: [] },
    destinationTimeZoneId: null,
    status: 'Draft',
    accessScope: 'Private',
    memberCount: 1,
    isOwner: true,
    effectiveRole: 'Owner',
    canEditPlan: true,
    canEditProgram: true,
    canInvite: true,
    canChangeRoles: true,
    createdAtUtc: '2026-09-18T10:00:00Z',
    updatedAtUtc: '2026-09-18T10:00:00Z',
    version: 1,
    ...overrides
  };
}
