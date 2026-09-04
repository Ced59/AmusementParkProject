import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { GlobalRatingSuggestions } from '@app/models/passport/global-rating-suggestion.models';
import { GLOBAL_RATING_SUGGESTIONS_API_PORT } from './global-rating-suggestions-state-data.ports';
import { GlobalRatingSuggestionsStateFacade } from './global-rating-suggestions-state.facade';

describe('GlobalRatingSuggestionsStateFacade', () => {
  let api: {
    getSuggestions: ReturnType<typeof vi.fn>;
    setEnabled: ReturnType<typeof vi.fn>;
    recordInteraction: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    api = {
      getSuggestions: vi.fn().mockReturnValue(of(createResponse())),
      setEnabled: vi.fn().mockReturnValue(of({ isAvailable: true, isEnabled: false })),
      recordInteraction: vi.fn().mockReturnValue(of({ isAvailable: true, isEnabled: true }))
    };
    TestBed.configureTestingModule({
      providers: [
        GlobalRatingSuggestionsStateFacade,
        { provide: GLOBAL_RATING_SUGGESTIONS_API_PORT, useValue: api }
      ]
    });
  });

  it('loads, maps and records presentation without changing any rating', () => {
    const facade = TestBed.inject(GlobalRatingSuggestionsStateFacade);

    facade.load('fr');

    expect(facade.suggestions()[0].recentAverageLabel).toBe('3,25 / 5');
    expect(api.recordInteraction).toHaveBeenCalledWith({
      targetType: 'ParkItem', targetId: 'item-1', interactionType: 'Presented'
    });
    expect(api).not.toHaveProperty('upsertRating');
  });

  it('records acceptance before handing control back to the rating editor', () => {
    const facade = TestBed.inject(GlobalRatingSuggestionsStateFacade);
    const accepted = vi.fn();
    facade.load('en');

    facade.accept(facade.suggestions()[0], accepted);

    expect(api.recordInteraction).toHaveBeenLastCalledWith({
      targetType: 'ParkItem', targetId: 'item-1', interactionType: 'Accepted'
    });
    expect(accepted).toHaveBeenCalledTimes(1);
    expect(facade.suggestions()).toEqual([]);
  });

  it('removes a dismissed suggestion and supports an explicit opt-out', () => {
    const facade = TestBed.inject(GlobalRatingSuggestionsStateFacade);
    facade.load('en');
    facade.dismiss(facade.suggestions()[0]);

    expect(facade.suggestions()).toEqual([]);
    facade.load('en');
    facade.setEnabled(false);
    expect(api.setEnabled).toHaveBeenCalledWith(false);
    expect(facade.enabled()).toBe(false);
  });

  it('exposes a recoverable error without inventing a suggestion', () => {
    api.getSuggestions.mockReturnValue(throwError(() => new Error('offline')));
    const facade = TestBed.inject(GlobalRatingSuggestionsStateFacade);

    facade.load('en');

    expect(facade.error()).toBe(true);
    expect(facade.suggestions()).toEqual([]);
  });
});

function createResponse(): GlobalRatingSuggestions {
  return {
    isAvailable: true,
    isEnabled: true,
    minimumNewObservationCount: 2,
    cooldownDays: 30,
    suggestions: [{
      targetType: 'ParkItem',
      targetId: 'item-1',
      targetName: 'Taron',
      parkId: 'park-1',
      parkName: 'Phantasialand',
      parkItemCategory: 'Attraction',
      currentGlobalRating: 4.5,
      latestObservationRating: 3,
      recentAverage: 3.25,
      historicalMedian: 4,
      newObservationCount: 2,
      recentObservationCount: 2,
      reason: 'RecentExperiencesLower',
      latestObservationAtUtc: '2026-09-01T10:00:00Z'
    }]
  };
}
