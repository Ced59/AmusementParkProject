import { DestroyRef } from '@angular/core';
import { of } from 'rxjs';

import {
  TripProgramCoherence,
  TripProgramCoherenceCode,
  TripProgramCoherenceIssue,
  TripProgramCoherenceSeverity
} from '@app/models/trips/trip.models';
import { TripProgramCoherenceDataPort } from './trip-program-coherence-data.port';
import { TripProgramCoherenceFacade } from './trip-program-coherence.facade';

describe('TripProgramCoherenceFacade', () => {
  it('orders critical facts before notices without changing the server result', () => {
    const coherence: TripProgramCoherence = createCoherence();
    const data: TripProgramCoherenceDataPort = { get: vi.fn().mockReturnValue(of(coherence)) };
    const facade: TripProgramCoherenceFacade = createFacade(data);

    facade.load('trip-1');

    expect(facade.orderedIssues().map((issue) => issue.severity)).toEqual([
      'Critical',
      'Attention',
      'Information'
    ]);
    expect(coherence.issues[0].severity).toBe('Information');
  });

  it('does not call the API for an empty identifier', () => {
    const data: TripProgramCoherenceDataPort = { get: vi.fn().mockReturnValue(of(createCoherence())) };
    const facade: TripProgramCoherenceFacade = createFacade(data);

    facade.load('   ');

    expect(data.get).not.toHaveBeenCalled();
  });
});

function createFacade(data: TripProgramCoherenceDataPort): TripProgramCoherenceFacade {
  const destroyRef: DestroyRef = {
    onDestroy: (): (() => void) => (): void => undefined,
    destroyed: false
  } as unknown as DestroyRef;
  return new TripProgramCoherenceFacade(data, destroyRef);
}

function createCoherence(): TripProgramCoherence {
  return {
    tripPlanId: 'trip-1',
    tripTitle: 'Voyage test',
    planVersion: 4,
    evaluatedAtUtc: '2026-09-18T12:00:00Z',
    criticalCount: 1,
    attentionCount: 1,
    informationCount: 1,
    days: [],
    travelSegments: [],
    issues: [
      createIssue('OpeningHoursVerifiedAfterPlanning', 'Information'),
      createIssue('OpeningHoursClosed', 'Critical'),
      createIssue('OpeningHoursStale', 'Attention')
    ]
  };
}

function createIssue(
  code: TripProgramCoherenceCode,
  severity: TripProgramCoherenceSeverity
): TripProgramCoherenceIssue {
  return {
    code,
    severity,
    localDate: '2026-10-01',
    parkId: 'park-1',
    parkName: 'Parc test',
    parkItemId: null,
    parkItemName: null,
    officialStatus: null,
    officialSourceUrl: null,
    officialVerifiedAtUtc: null
  };
}
