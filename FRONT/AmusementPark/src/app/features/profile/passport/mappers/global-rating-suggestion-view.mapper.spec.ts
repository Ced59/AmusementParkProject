import { GlobalRatingSuggestion } from '@app/models/passport/global-rating-suggestion.models';
import { mapGlobalRatingSuggestionView } from './global-rating-suggestion-view.mapper';

describe('mapGlobalRatingSuggestionView', () => {
  it('formats values for the active locale and maps numeric API enums', () => {
    const view = mapGlobalRatingSuggestionView(
      createSuggestion(),
      'fr',
      '2026-09-04T07:00:00Z'
    );

    expect(view).toEqual(expect.objectContaining({
      id: 'ParkItem:item-1',
      targetType: 'ParkItem',
      presentedAtUtc: '2026-09-04T07:00:00Z',
      currentGlobalRatingLabel: '4,5 / 5',
      recentAverageLabel: '3,25 / 5',
      reasonKey: 'passport.ratingSuggestions.reasons.lower'
    }));
  });
});

function createSuggestion(): GlobalRatingSuggestion {
  return {
    targetType: 2,
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
    reason: 1,
    latestObservationAtUtc: '2026-09-01T10:00:00Z'
  };
}
