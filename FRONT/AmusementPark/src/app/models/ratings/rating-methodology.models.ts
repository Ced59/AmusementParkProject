export interface RatingScale {
  minimum: number;
  maximum: number;
  step: number;
}

export interface BayesianRatingParameters {
  priorMean: number;
  priorWeight: number;
}

export interface ParkRatingComposition {
  directRatingWeight: number;
  itemRatingWeight: number;
  balancesItemCategoriesEqually: boolean;
  minimumEligibleItems: number;
  minimumItemsPerCategory: number;
  minimumCategories: number;
}

export interface RatingEvidenceThresholds {
  provisional: number;
  eligible: number;
  established: number;
  strong: number;
}

export interface RatingRankingPublicationRules {
  minimumEligibleEntries: number;
  scoreTieEpsilon: number;
  rankingConvention: 'competition';
}

export interface RatingMethodology {
  version: string;
  effectiveDate: string;
  isCurrent: boolean;
  previousVersion?: string | null;
  ratingScale: RatingScale;
  bayesian: BayesianRatingParameters;
  parkComposition: ParkRatingComposition;
  evidenceThresholds: RatingEvidenceThresholds;
  publicationRules: RatingRankingPublicationRules;
}
