import { PassportVisitDate } from './passport-visit.models';

export type PassportRatingTrendKind = 'Stable' | 'Rising' | 'Falling' | 0 | 1 | 2;

export interface PassportRatingDistribution {
  ratingCount: number;
  average: number;
  median: number;
  minimum: number;
  maximum: number;
  populationStandardDeviation: number;
}

export interface PassportRatingCoverage {
  ratedCount: number;
  totalCount: number;
  rate: number;
}

export interface PassportItemRatingCoverage {
  ratedRideCount: number;
  totalRideCount: number;
  rate: number;
}

export interface PassportVisitExperience {
  visitId: string;
  parkId: string;
  date: PassportVisitDate;
}

export interface PassportItemExperience {
  visitId: string;
  date: PassportVisitDate;
}

export interface PassportRideOutcomeStatistics {
  recordedOutcomeCount: number;
  completedRideCount: number;
  attemptedCount: number;
  missedClosedCount: number;
  missedUnavailableCount: number;
  skippedByChoiceCount: number;
}

export interface PassportCategoryCoverage {
  category: string | null;
  completedRideCount: number;
  distinctItemCount: number;
  historicalReferenceRideCount: number;
  currentReferenceRideCount: number;
  unknownReferenceRideCount: number;
  completedRideRate: number;
}

export interface PassportStatisticsSummary {
  visitCount: number;
  approximateVisitCount: number;
  parkRatingCoverage: PassportRatingCoverage;
  historicalParkRatings: PassportRatingDistribution | null;
  firstVisit: PassportVisitExperience | null;
  lastVisit: PassportVisitExperience | null;
  rideOutcomes: PassportRideOutcomeStatistics;
  rideRatingCoverage: PassportRatingCoverage;
  historicalRideRatings: PassportRatingDistribution | null;
  distinctCompletedItemCount: number;
  repeatedCompletedItemCount: number;
  categoryCoverage: PassportCategoryCoverage[];
}

export interface PassportItemVisitStatistics {
  visitId: string;
  date: PassportVisitDate;
  rideCount: number;
  ratingCoverage: PassportItemRatingCoverage;
  historicalRatings: PassportRatingDistribution | null;
}

export interface PassportItemYearStatistics {
  year: number;
  rideCount: number;
  visitCount: number;
  ratingCoverage: PassportItemRatingCoverage;
  historicalRatings: PassportRatingDistribution | null;
}

export interface PassportItemRatingPoint {
  rideOccurrenceId: string;
  visitId: string;
  date: PassportVisitDate;
  sortPosition: number;
  rating: number;
}

export interface PassportRatingTrend {
  kind: PassportRatingTrendKind;
  firstWindowRatingCount: number;
  lastWindowRatingCount: number;
  firstWindowAverage: number;
  lastWindowAverage: number;
  delta: number;
}

export interface PassportItemStatistics {
  parkItemId: string;
  rideCount: number;
  visitCount: number;
  ratingCoverage: PassportItemRatingCoverage;
  firstExperience: PassportItemExperience | null;
  lastExperience: PassportItemExperience | null;
  historicalRatings: PassportRatingDistribution | null;
  currentGlobalRating: number | null;
  currentGlobalMinusHistoricalAverage: number | null;
  byVisit: PassportItemVisitStatistics[];
  byYear: PassportItemYearStatistics[];
  ratingTimeline: PassportItemRatingPoint[];
  trend: PassportRatingTrend | null;
}

export interface PassportParkAssessmentPoint {
  visitId: string;
  date: PassportVisitDate;
  rating: number;
}

export interface PassportCurrentItemRating {
  parkItemId: string;
  rating: number;
}

export interface PassportHistoricalItemRating {
  parkItemId: string;
  ratingCount: number;
  average: number;
}

export interface PassportYearBreakdown {
  year: number;
  summary: PassportStatisticsSummary;
}

export interface PassportParkBreakdown {
  parkId: string;
  summary: PassportStatisticsSummary;
}

export interface PassportParkStatistics {
  parkId: string;
  summary: PassportStatisticsSummary;
  currentGlobalRating: number | null;
  currentGlobalMinusHistoricalAverage: number | null;
  assessmentTimeline: PassportParkAssessmentPoint[];
  byYear: PassportYearBreakdown[];
  currentTopItems: PassportCurrentItemRating[];
  historicalTopItems: PassportHistoricalItemRating[];
}

export interface PassportYearStatistics {
  year: number;
  parkCount: number;
  summary: PassportStatisticsSummary;
  byPark: PassportParkBreakdown[];
}
