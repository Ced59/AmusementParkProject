export interface GlobalRatingSuggestionViewModel {
  id: string;
  targetType: 'Park' | 'ParkItem';
  targetId: string;
  targetName: string;
  parkName: string | null;
  parkItemCategory: string | null;
  currentGlobalRatingLabel: string;
  latestObservationRatingLabel: string;
  recentAverageLabel: string;
  historicalMedianLabel: string;
  newObservationCount: number;
  recentObservationCount: number;
  reasonKey: string;
}
