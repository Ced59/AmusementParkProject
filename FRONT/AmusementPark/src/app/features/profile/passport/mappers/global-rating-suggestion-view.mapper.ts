import { GlobalRatingSuggestion } from '@app/models/passport/global-rating-suggestion.models';
import { GlobalRatingSuggestionViewModel } from '../models/global-rating-suggestion-view.models';

export function mapGlobalRatingSuggestionView(
  suggestion: GlobalRatingSuggestion,
  language: string
): GlobalRatingSuggestionViewModel {
  const targetType: 'Park' | 'ParkItem' = suggestion.targetType === 'Park' || suggestion.targetType === 1
    ? 'Park'
    : 'ParkItem';
  const formatter = new Intl.NumberFormat(language, {
    minimumFractionDigits: 1,
    maximumFractionDigits: 2
  });
  const reasonIsLower: boolean = suggestion.reason === 'RecentExperiencesLower' || suggestion.reason === 1;
  return {
    id: `${targetType}:${suggestion.targetId}`,
    targetType,
    targetId: suggestion.targetId,
    targetName: suggestion.targetName,
    parkName: suggestion.parkName,
    parkItemCategory: suggestion.parkItemCategory,
    currentGlobalRatingLabel: `${formatter.format(suggestion.currentGlobalRating)} / 5`,
    latestObservationRatingLabel: `${formatter.format(suggestion.latestObservationRating)} / 5`,
    recentAverageLabel: `${formatter.format(suggestion.recentAverage)} / 5`,
    historicalMedianLabel: `${formatter.format(suggestion.historicalMedian)} / 5`,
    newObservationCount: suggestion.newObservationCount,
    recentObservationCount: suggestion.recentObservationCount,
    reasonKey: reasonIsLower
      ? 'passport.ratingSuggestions.reasons.lower'
      : 'passport.ratingSuggestions.reasons.higher'
  };
}
