import { Observable, of } from 'rxjs';

import { RatingRankingAdministration, RatingRankingPolicyCandidateRequest, RatingRankingPolicyImpact, RatingRankingRebuildRequestResult } from '@app/models/admin/ratings/rating-ranking-administration.models';

import { AdminRatingRankingStatePort } from '../../../../state/admin-rating-ranking-state-data.ports';

function createDashboard(): RatingRankingAdministration {
  return {
    generatedAtUtc: '2026-09-02T12:00:00Z',
    currentMethodology: {
      version: 'ratings-2026-01',
      effectiveDate: '2026-08-31',
      isCurrent: true,
      previousVersion: null,
      ratingScale: { minimum: 0.5, maximum: 5, step: 0.5 },
      bayesian: { priorMean: 3.5, priorWeight: 5 },
      parkComposition: {
        directRatingWeight: 0.5,
        itemRatingWeight: 0.5,
        balancesItemCategoriesEqually: true,
        minimumEligibleItems: 5,
        minimumItemsPerCategory: 2,
        minimumCategories: 2
      },
      evidenceThresholds: { provisional: 3, eligible: 10, established: 30, strong: 100 },
      publicationRules: { minimumEligibleEntries: 3, scoreTieEpsilon: 0.0001, rankingConvention: 'competition' }
    },
    preparingMethodology: null,
    dataDiagnostics: {
      generatedAtUtc: '2026-09-02T12:00:00Z',
      executionDurationMilliseconds: 10,
      totalRatings: 42,
      anomalies: {
        nonNumericValueCount: 0,
        unexpectedValueStorageTypeCount: 0,
        outOfRangeValueCount: 0,
        nonHalfStepValueCount: 0,
        nearHalfStepValueCount: 0,
        missingUserIdCount: 0,
        missingTargetCount: 0,
        duplicateVoteKeyCount: 0,
        extraDuplicateDocumentCount: 0
      },
      aggregateIntegrity: {
        isSourceComparisonEvaluated: true,
        isOrphanCheckEvaluated: true,
        sourceTargetCount: 3,
        missingAggregateCount: 0,
        divergentAggregateCount: 0,
        contributorCountMismatchCount: 0,
        derivedScoreMismatchCount: 0,
        orphanAggregateCount: 0
      },
      targetDistribution: []
    },
    scopes: [],
    evidenceDistribution: [],
    nearThresholdTargets: [],
    exclusions: [],
    categoryCoverage: []
  };
}

export class FakeAdminRatingRankingPort implements AdminRatingRankingStatePort {
  public readonly previewRequests: RatingRankingPolicyCandidateRequest[] = [];
  public rebuildCallCount: number = 0;
  public rebuildResult: Observable<RatingRankingRebuildRequestResult> | null = null;

  getDashboard(): Observable<RatingRankingAdministration> {
    return of(createDashboard());
  }

  previewImpact(request: RatingRankingPolicyCandidateRequest): Observable<RatingRankingPolicyImpact> {
    this.previewRequests.push(request);
    return of({
      generatedAtUtc: '2026-09-02T12:00:00Z',
      candidate: request,
      gainedEligibilityCount: 0,
      lostEligibilityCount: 0,
      comparedRankCount: 0,
      totalAbsoluteRankChange: 0,
      averageRankChange: null,
      maximumRankChange: null,
      scopeCountBelowMinimum: 0,
      incompleteParkCompositionCount: 0,
      estimatedTargetCount: 0,
      estimatedChunkCount: 0,
      scopes: []
    });
  }

  rebuild(): Observable<RatingRankingRebuildRequestResult> {
    this.rebuildCallCount++;
    return this.rebuildResult ?? of({
      requestedAtUtc: '2026-09-02T12:00:00Z',
      scheduledScopeCount: 1,
      scopes: [{ scopeKey: 'parks:global', requestedSourceRevision: 8 }]
    });
  }
}
