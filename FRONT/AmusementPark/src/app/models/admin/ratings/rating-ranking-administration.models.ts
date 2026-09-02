import { RatingMethodology } from '@app/models/ratings/rating-methodology.models';

export interface RatingRankingAdministration {
  generatedAtUtc: string;
  currentMethodology: RatingMethodology;
  preparingMethodology?: RatingMethodology | null;
  dataDiagnostics: RatingDataDiagnostics;
  scopes: RatingRankingScopeDiagnostics[];
  evidenceDistribution: RatingRankingEvidenceDistribution[];
  nearThresholdTargets: RatingRankingNearThresholdTarget[];
  exclusions: RatingRankingExclusionDistribution[];
  categoryCoverage: RatingRankingCategoryCoverage[];
}

export interface RatingDataDiagnostics {
  generatedAtUtc: string;
  executionDurationMilliseconds: number;
  totalRatings: number;
  anomalies: RatingDataAnomalies;
  aggregateIntegrity: RatingAggregateIntegrity;
  targetDistribution: RatingTargetDistribution[];
}

export interface RatingDataAnomalies {
  nonNumericValueCount: number;
  unexpectedValueStorageTypeCount: number;
  outOfRangeValueCount: number;
  nonHalfStepValueCount: number;
  nearHalfStepValueCount: number;
  missingUserIdCount: number;
  missingTargetCount: number;
  duplicateVoteKeyCount: number;
  extraDuplicateDocumentCount: number;
}

export interface RatingAggregateIntegrity {
  isSourceComparisonEvaluated: boolean;
  isOrphanCheckEvaluated: boolean;
  sourceTargetCount: number;
  missingAggregateCount: number;
  divergentAggregateCount: number;
  contributorCountMismatchCount: number;
  derivedScoreMismatchCount: number;
  orphanAggregateCount: number;
}

export interface RatingTargetDistribution {
  targetType: string;
  evidenceBand: string;
  targetCount: number;
  ratingObservationCount: number;
  uniqueContributorCount: number;
}

export interface RatingRankingScopeDiagnostics {
  scopeKey: string;
  targetFamily: 'Parks' | 'ParkItems';
  parkItemCategory?: string | null;
  methodologyVersion: string;
  currentSnapshotId?: string | null;
  generatedAtUtc?: string | null;
  publishedAtUtc?: string | null;
  rebuildDurationMilliseconds?: number | null;
  totalEntryCount: number;
  eligibleEntryCount: number;
  sourceRevision: number;
  publishedSourceRevision?: number | null;
  isRebuildOutstanding: boolean;
  isDiagnosticSourceTruncated: boolean;
  lastJobStatus?: string | null;
  lastErrorCode?: string | null;
  lastJobUpdatedAtUtc?: string | null;
}

export interface RatingRankingEvidenceDistribution {
  targetType: 'Park' | 'ParkItem';
  level: string;
  targetCount: number;
  uniqueContributorCount: number;
  ratingObservationCount: number;
}

export interface RatingRankingNearThresholdTarget {
  scopeKey: string;
  targetType: 'Park' | 'ParkItem';
  targetId: string;
  targetName: string;
  uniqueContributorCount: number;
  eligibilityThreshold: number;
  remainingContributorCount: number;
}

export interface RatingRankingExclusionDistribution {
  targetType: 'Park' | 'ParkItem';
  reason: string;
  targetCount: number;
}

export interface RatingRankingCategoryCoverage {
  scopeKey: string;
  category: string;
  candidateCount: number;
  eligibleCount: number;
  hasMinimumComparableEntries: boolean;
}

export interface RatingRankingPolicyCandidateRequest {
  version: string;
  provisionalMinUniqueContributors: number;
  eligibleMinUniqueContributors: number;
  establishedMinUniqueContributors: number;
  strongEvidenceMinUniqueContributors: number;
  minimumEligibleEntriesPerRanking: number;
  minimumEligibleItemsForParkItemComponent: number;
  minimumEligibleItemsPerCategory: number;
  minimumEligibleCategories: number;
  scoreTieEpsilon: number;
}

export interface RatingRankingPolicyImpact {
  generatedAtUtc: string;
  candidate: RatingRankingPolicyCandidateRequest;
  gainedEligibilityCount: number;
  lostEligibilityCount: number;
  comparedRankCount: number;
  totalAbsoluteRankChange: number;
  averageRankChange?: number | null;
  maximumRankChange?: number | null;
  scopeCountBelowMinimum: number;
  incompleteParkCompositionCount: number;
  estimatedTargetCount: number;
  estimatedChunkCount: number;
  scopes: RatingRankingPolicyScopeImpact[];
}

export interface RatingRankingPolicyScopeImpact {
  scopeKey: string;
  targetFamily: 'Parks' | 'ParkItems';
  parkItemCategory?: string | null;
  hasCurrentSnapshot: boolean;
  isImpactAvailable: boolean;
  isSourceTruncated: boolean;
  currentEligibleCount: number;
  candidateEligibleCount: number;
  gainedEligibilityCount: number;
  lostEligibilityCount: number;
  comparedRankCount: number;
  totalAbsoluteRankChange: number;
  averageRankChange?: number | null;
  maximumRankChange?: number | null;
  hasMinimumComparableEntries: boolean;
  incompleteParkCompositionCount: number;
  estimatedTargetCount: number;
  estimatedChunkCount: number;
  gainedTargets: RatingRankingPolicyTargetChange[];
  lostTargets: RatingRankingPolicyTargetChange[];
}

export interface RatingRankingPolicyTargetChange {
  targetType: 'Park' | 'ParkItem';
  targetId: string;
  targetName: string;
  previousRank?: number | null;
  candidateRank?: number | null;
}

export interface RatingRankingRebuildRequestResult {
  requestedAtUtc: string;
  scheduledScopeCount: number;
  scopes: RatingRankingScheduledScope[];
}

export interface RatingRankingScheduledScope {
  scopeKey: string;
  requestedSourceRevision: number;
}
