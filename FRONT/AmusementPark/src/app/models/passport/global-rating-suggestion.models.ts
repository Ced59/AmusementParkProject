export type GlobalRatingSuggestionTargetType = 'Park' | 'ParkItem' | 1 | 2;
export type GlobalRatingSuggestionReason =
  | 'RecentExperiencesLower'
  | 'RecentExperiencesHigher'
  | 1
  | 2;
export type GlobalRatingSuggestionInteractionType = 'Accepted' | 'Dismissed';

export interface GlobalRatingSuggestion {
  targetType: GlobalRatingSuggestionTargetType;
  targetId: string;
  targetName: string;
  parkId: string;
  parkName: string | null;
  parkItemCategory: string | null;
  currentGlobalRating: number;
  latestObservationRating: number;
  recentAverage: number;
  historicalMedian: number;
  newObservationCount: number;
  recentObservationCount: number;
  reason: GlobalRatingSuggestionReason;
  latestObservationAtUtc: string;
}

export interface GlobalRatingSuggestions {
  isAvailable: boolean;
  isEnabled: boolean;
  minimumNewObservationCount: number;
  cooldownDays: number;
  suggestions: GlobalRatingSuggestion[];
}

export interface GlobalRatingSuggestionPreference {
  isAvailable: boolean;
  isEnabled: boolean;
}

export interface GlobalRatingSuggestionPresentationTarget {
  targetType: 'Park' | 'ParkItem';
  targetId: string;
}

export interface GlobalRatingSuggestionPresentedTarget extends GlobalRatingSuggestionPresentationTarget {
  presentedAtUtc: string;
}

export interface GlobalRatingSuggestionPresentation {
  isAvailable: boolean;
  isEnabled: boolean;
  presentedTargets: GlobalRatingSuggestionPresentedTarget[];
}

export interface PresentGlobalRatingSuggestionsRequest {
  targets: GlobalRatingSuggestionPresentationTarget[];
}

export interface RecordGlobalRatingSuggestionInteractionRequest {
  targetType: 'Park' | 'ParkItem';
  targetId: string;
  interactionType: GlobalRatingSuggestionInteractionType;
  presentedAtUtc: string;
}
