import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { GlobalRatingSuggestions } from '@app/models/passport/global-rating-suggestion.models';
import { GLOBAL_RATING_SUGGESTIONS_API_PORT } from './global-rating-suggestions-state-data.ports';
import { GlobalRatingSuggestionsStateFacade } from './global-rating-suggestions-state.facade';

describe('GlobalRatingSuggestionsStateFacade', () => {
  let api: {
    getSuggestions: ReturnType<typeof vi.fn>;
    presentSuggestions: ReturnType<typeof vi.fn>;
    setEnabled: ReturnType<typeof vi.fn>;
    recordInteraction: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    api = {
      getSuggestions: vi.fn().mockReturnValue(of(createResponse())),
      presentSuggestions: vi.fn().mockReturnValue(of({
        isAvailable: true,
        isEnabled: true,
        presentedTargets: [{ targetType: 'ParkItem', targetId: 'item-1' }]
      })),
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

  it('shows only suggestions whose batched presentation was acknowledged', () => {
    const facade = TestBed.inject(GlobalRatingSuggestionsStateFacade);

    facade.load('fr');

    expect(facade.suggestions()[0].recentAverageLabel).toBe('3,25 / 5');
    expect(api.presentSuggestions).toHaveBeenCalledWith({
      targets: [{ targetType: 'ParkItem', targetId: 'item-1' }]
    });
    expect(api).not.toHaveProperty('upsertRating');
  });

  it('does not expose actions when presentation acknowledgement fails', () => {
    api.presentSuggestions.mockReturnValue(throwError(() => new Error('offline')));
    const facade = TestBed.inject(GlobalRatingSuggestionsStateFacade);

    facade.load('en');

    expect(facade.error()).toBe(true);
    expect(facade.loading()).toBe(false);
    expect(facade.suggestions()).toEqual([]);
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

  it('clears a previous acceptance error when a retry succeeds', () => {
    api.recordInteraction
      .mockReturnValueOnce(throwError(() => new Error('offline')))
      .mockReturnValueOnce(of({ isAvailable: true, isEnabled: true }));
    const facade = TestBed.inject(GlobalRatingSuggestionsStateFacade);
    const accepted = vi.fn();
    facade.load('en');
    const suggestion = facade.suggestions()[0];

    facade.accept(suggestion, accepted);
    expect(facade.error()).toBe(true);

    facade.accept(suggestion, accepted);

    expect(facade.error()).toBe(false);
    expect(accepted).toHaveBeenCalledTimes(1);
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

  it('clears a previous dismissal error when a retry succeeds', () => {
    api.recordInteraction
      .mockReturnValueOnce(throwError(() => new Error('offline')))
      .mockReturnValueOnce(of({ isAvailable: true, isEnabled: true }));
    const facade = TestBed.inject(GlobalRatingSuggestionsStateFacade);
    facade.load('en');
    const suggestion = facade.suggestions()[0];

    facade.dismiss(suggestion);
    expect(facade.error()).toBe(true);

    facade.dismiss(suggestion);

    expect(facade.error()).toBe(false);
    expect(facade.suggestions()).toEqual([]);
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
