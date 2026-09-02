import { TestBed } from '@angular/core/testing';
import { Observable, Subject, of, throwError } from 'rxjs';

import {
  RatingRankingAdministration,
  RatingRankingPolicyCandidateRequest,
  RatingRankingPolicyImpact,
  RatingRankingRebuildRequestResult
} from '@app/models/admin/ratings/rating-ranking-administration.models';
import {
  ADMIN_RATING_RANKING_STATE_PORT,
  AdminRatingRankingStatePort
} from './admin-rating-ranking-state-data.ports';
import { AdminRatingRankingStateFacade } from './admin-rating-ranking-state.facade';

class FakeAdminRatingRankingPort implements AdminRatingRankingStatePort {
  public readonly previewRequests: RatingRankingPolicyCandidateRequest[] = [];
  public dashboardCallCount: number = 0;
  public rebuildCallCount: number = 0;
  public previewResult: Observable<RatingRankingPolicyImpact> | null = null;
  public rebuildResult: Observable<RatingRankingRebuildRequestResult> | null = null;

  getDashboard(): Observable<RatingRankingAdministration> {
    this.dashboardCallCount++;
    return of(createDashboard());
  }

  previewImpact(request: RatingRankingPolicyCandidateRequest): Observable<RatingRankingPolicyImpact> {
    this.previewRequests.push(request);
    return this.previewResult ?? of(createImpact(request));
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

describe('AdminRatingRankingStateFacade', () => {
  let facade: AdminRatingRankingStateFacade;
  let port: FakeAdminRatingRankingPort;

  beforeEach(() => {
    port = new FakeAdminRatingRankingPort();
    TestBed.configureTestingModule({
      providers: [
        AdminRatingRankingStateFacade,
        { provide: ADMIN_RATING_RANKING_STATE_PORT, useValue: port }
      ]
    });
    facade = TestBed.inject(AdminRatingRankingStateFacade);
  });

  it('loads the dashboard into the shared screen state', () => {
    facade.load();

    expect(port.dashboardCallCount).toBe(1);
    expect(facade.dashboard()?.currentMethodology.version).toBe('ratings-2026-01');
    expect(facade.loading()).toBe(false);
  });

  it('keeps the candidate preview separate from the published dashboard', () => {
    const candidate: RatingRankingPolicyCandidateRequest = createCandidate();

    facade.preview(candidate);

    expect(port.previewRequests).toEqual([candidate]);
    expect(facade.impact()?.candidate.version).toBe('ratings-2026-02');
    expect(facade.previewing()).toBe(false);
    expect(facade.actionMessageKey()).toBe('admin.ratingRanking.preview.success');
  });

  it('clears the previous impact when a new preview fails', () => {
    const candidate: RatingRankingPolicyCandidateRequest = createCandidate();
    facade.preview(candidate);
    expect(facade.impact()).not.toBeNull();
    port.previewResult = throwError(() => new Error('preview failed'));

    facade.preview({ ...candidate, eligibleMinUniqueContributors: 12 });

    expect(facade.impact()).toBeNull();
    expect(facade.previewing()).toBe(false);
    expect(facade.actionMessageKey()).toBe('admin.ratingRanking.preview.error');
  });

  it('keeps at most one expensive preview request in flight', () => {
    const response: Subject<RatingRankingPolicyImpact> = new Subject<RatingRankingPolicyImpact>();
    const candidate: RatingRankingPolicyCandidateRequest = createCandidate();
    port.previewResult = response;

    facade.preview(candidate);
    facade.preview({ ...candidate, eligibleMinUniqueContributors: 12 });

    expect(port.previewRequests).toEqual([candidate]);
    expect(facade.previewing()).toBe(true);
    response.next(createImpact(candidate));
    response.complete();
    expect(facade.previewing()).toBe(false);
  });

  it('reloads diagnostics after scheduling a rebuild', () => {
    facade.rebuild();

    expect(port.rebuildCallCount).toBe(1);
    expect(port.dashboardCallCount).toBe(1);
    expect(facade.rebuildResult()?.scheduledScopeCount).toBe(1);
    expect(facade.actionMessageKey()).toBe('admin.ratingRanking.rebuild.success');
  });

  it('clears the previous rebuild result when a new rebuild fails', () => {
    facade.rebuild();
    expect(facade.rebuildResult()).not.toBeNull();
    port.rebuildResult = throwError(() => new Error('rebuild failed'));

    facade.rebuild();

    expect(facade.rebuildResult()).toBeNull();
    expect(facade.rebuilding()).toBe(false);
    expect(facade.actionMessageKey()).toBe('admin.ratingRanking.rebuild.error');
  });

  it('keeps at most one expensive rebuild request in flight', () => {
    const response: Subject<RatingRankingRebuildRequestResult> =
      new Subject<RatingRankingRebuildRequestResult>();
    port.rebuildResult = response;

    facade.rebuild();
    facade.rebuild();

    expect(port.rebuildCallCount).toBe(1);
    expect(facade.rebuilding()).toBe(true);
    response.next({
      requestedAtUtc: '2026-09-02T12:00:00Z',
      scheduledScopeCount: 1,
      scopes: [{ scopeKey: 'parks:global', requestedSourceRevision: 8 }]
    });
    response.complete();
    expect(facade.rebuilding()).toBe(false);
  });
});

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

function createCandidate(): RatingRankingPolicyCandidateRequest {
  return {
    version: 'ratings-2026-02',
    provisionalMinUniqueContributors: 3,
    eligibleMinUniqueContributors: 10,
    establishedMinUniqueContributors: 30,
    strongEvidenceMinUniqueContributors: 100,
    minimumEligibleEntriesPerRanking: 3,
    minimumEligibleItemsForParkItemComponent: 5,
    minimumEligibleItemsPerCategory: 2,
    minimumEligibleCategories: 2,
    scoreTieEpsilon: 0.0001
  };
}

function createImpact(candidate: RatingRankingPolicyCandidateRequest): RatingRankingPolicyImpact {
  return {
    generatedAtUtc: '2026-09-02T12:00:00Z',
    candidate,
    gainedEligibilityCount: 1,
    lostEligibilityCount: 0,
    comparedRankCount: 2,
    totalAbsoluteRankChange: 1,
    averageRankChange: 0.5,
    maximumRankChange: 1,
    scopeCountBelowMinimum: 0,
    incompleteParkCompositionCount: 0,
    estimatedTargetCount: 3,
    estimatedChunkCount: 1,
    scopes: []
  };
}
