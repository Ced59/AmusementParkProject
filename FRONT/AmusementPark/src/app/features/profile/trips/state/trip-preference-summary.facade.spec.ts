import { DestroyRef } from '@angular/core';
import { of } from 'rxjs';

import { TripPreferenceSummary } from '@app/models/trips/trip.models';
import { TripPreferenceSummaryDataPort } from './trip-preference-summary-data.port';
import { TripPreferenceSummaryFacade } from './trip-preference-summary.facade';

describe('TripPreferenceSummaryFacade', () => {
  it('shows conflicts before consensus and never exposes individual preferences', () => {
    const data: TripPreferenceSummaryDataPort = {
      get: vi.fn().mockReturnValue(of(createSummary())),
      setDecision: vi.fn().mockReturnValue(of(createSummary()))
    };
    const facade: TripPreferenceSummaryFacade = createFacade(data);

    facade.load('trip-1');

    expect(facade.visibleItems().map((item) => item.compatibility)).toEqual([
      'Conflict',
      'Consensus'
    ]);
    expect(facade.count('Conflict')).toBe(1);
    expect(facade.summary()?.items[0]).not.toHaveProperty('preferences');
  });

  it('requires a reason and keeps the write versioned', () => {
    const summary: TripPreferenceSummary = createSummary();
    const data: TripPreferenceSummaryDataPort = {
      get: vi.fn().mockReturnValue(of(summary)),
      setDecision: vi.fn().mockReturnValue(of(summary))
    };
    const facade: TripPreferenceSummaryFacade = createFacade(data);
    const conflict = summary.items[1];

    facade.load('trip-1');
    facade.setDecisionStatus(conflict, 'SplitGroup');
    facade.setDecisionReason(conflict, '');
    expect(facade.canSave(conflict)).toBe(false);
    facade.setDecisionReason(conflict, 'Le groupe se retrouve ensuite.');
    expect(facade.canSave(conflict)).toBe(true);

    facade.saveDecision(conflict);

    expect(data.setDecision).toHaveBeenCalledWith('trip-1', 'item-conflict', {
      expectedPlanVersion: 7,
      expectedDecisionVersion: 2,
      status: 'SplitGroup',
      reason: 'Le groupe se retrouve ensuite.'
    });
  });

  it('does not let a participant record the official group decision', () => {
    const summary: TripPreferenceSummary = { ...createSummary(), canDecide: false };
    const data: TripPreferenceSummaryDataPort = {
      get: vi.fn().mockReturnValue(of(summary)),
      setDecision: vi.fn().mockReturnValue(of(summary))
    };
    const facade: TripPreferenceSummaryFacade = createFacade(data);
    const item = summary.items[0];

    facade.load('trip-1');
    facade.setDecisionReason(item, 'Une raison assez longue.');
    facade.saveDecision(item);

    expect(data.setDecision).not.toHaveBeenCalled();
  });
});

function createFacade(data: TripPreferenceSummaryDataPort): TripPreferenceSummaryFacade {
  const destroyRef: DestroyRef = {
    onDestroy: (): (() => void) => (): void => undefined,
    destroyed: false
  } as unknown as DestroyRef;
  return new TripPreferenceSummaryFacade(data, destroyRef);
}

function createSummary(): TripPreferenceSummary {
  return {
    tripPlanId: 'trip-1',
    tripTitle: 'Voyage test',
    planVersion: 7,
    participantCount: 4,
    canDecide: true,
    items: [
      {
        parkId: 'park-1',
        parkName: 'Parc test',
        parkItemId: 'item-consensus',
        parkItemName: 'Tour familiale',
        mainImageId: null,
        mustDoCount: 2,
        wantToDoCount: 2,
        optionalCount: 0,
        notForMeCount: 0,
        unansweredCount: 0,
        compatibility: 'Consensus',
        isCompatibilityKnown: true,
        hasIndividualConstraint: false,
        isGroupPriority: true,
        officialStatus: 'Operating',
        officialSourceUrl: null,
        officialStatusVerifiedAtUtc: null,
        decision: null
      },
      {
        parkId: 'park-1',
        parkName: 'Parc test',
        parkItemId: 'item-conflict',
        parkItemName: 'Grand huit',
        mainImageId: 'image-1',
        mustDoCount: 2,
        wantToDoCount: 1,
        optionalCount: 0,
        notForMeCount: 1,
        unansweredCount: 0,
        compatibility: 'Conflict',
        isCompatibilityKnown: true,
        hasIndividualConstraint: true,
        isGroupPriority: false,
        officialStatus: 'Operating',
        officialSourceUrl: 'https://example.com/ride',
        officialStatusVerifiedAtUtc: null,
        decision: {
          status: 'Review',
          reason: 'Le groupe doit encore en parler.',
          decidedByDisplayName: 'Organisateur1',
          decidedAtUtc: '2027-03-04T10:00:00Z',
          version: 2
        }
      }
    ]
  };
}
