import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, FormGroup } from '@angular/forms';
import { Observable, Subject, of } from 'rxjs';

import {
  RatingRankingAdministration,
  RatingRankingPolicyCandidateRequest,
  RatingRankingPolicyImpact,
  RatingRankingRebuildRequestResult
} from '@app/models/admin/ratings/rating-ranking-administration.models';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import {
  ADMIN_RATING_RANKING_STATE_PORT,
  AdminRatingRankingStatePort
} from '../../state/admin-rating-ranking-state-data.ports';
import { AdminRatingRankingComponent } from './admin-rating-ranking.component';

interface AdminRatingRankingComponentHarness {
  policyForm: FormGroup;
  rebuildConfirmed: FormControl<boolean>;
  rebuild(): void;
  impactUnavailableKey(scope: { isSourceTruncated: boolean }): string;
}

class FakeAdminRatingRankingPort implements AdminRatingRankingStatePort {
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

describe('AdminRatingRankingComponent', () => {
  let fixture: ComponentFixture<AdminRatingRankingComponent>;
  let port: FakeAdminRatingRankingPort;

  beforeEach(async () => {
    port = new FakeAdminRatingRankingPort();
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, AdminRatingRankingComponent],
      providers: [
        ...provideCommonTestDependencies(),
        { provide: ADMIN_RATING_RANKING_STATE_PORT, useValue: port }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AdminRatingRankingComponent);
    fixture.detectChanges();
  });

  it('prepares the next methodology version from the published policy', () => {
    const versionInput: HTMLInputElement = fixture.nativeElement.querySelector(
      'input[formControlName="version"]'
    );

    expect(versionInput.value).toBe('ratings-2026-02');
  });

  it('submits every policy field to the simulation facade', () => {
    const form: HTMLFormElement = fixture.nativeElement.querySelector('form');

    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(port.previewRequests).toEqual([createCandidate()]);
  });

  it('does not request a rebuild until the explicit confirmation is checked', () => {
    const component = fixture.componentInstance as unknown as AdminRatingRankingComponentHarness;

    component.rebuild();
    expect(port.rebuildCallCount).toBe(0);

    component.rebuildConfirmed.setValue(true);
    component.rebuild();

    expect(port.rebuildCallCount).toBe(1);
    expect(component.rebuildConfirmed.value).toBe(false);
  });

  it('distinguishes a safety limit from a concurrent source change', () => {
    const component = fixture.componentInstance as unknown as AdminRatingRankingComponentHarness;

    expect(component.impactUnavailableKey({ isSourceTruncated: true }))
      .toBe('admin.ratingRanking.impact.unavailable');
    expect(component.impactUnavailableKey({ isSourceTruncated: false }))
      .toBe('admin.ratingRanking.impact.sourceChanged');
  });

  it('disables and blocks rebuild submission while the request is pending', () => {
    const component = fixture.componentInstance as unknown as AdminRatingRankingComponentHarness;
    const response: Subject<RatingRankingRebuildRequestResult> =
      new Subject<RatingRankingRebuildRequestResult>();
    port.rebuildResult = response;
    component.rebuildConfirmed.setValue(true);

    component.rebuild();
    component.rebuildConfirmed.setValue(true);
    component.rebuild();
    fixture.detectChanges();

    const rebuildButton: HTMLButtonElement = fixture.nativeElement.querySelector(
      'button.p-button-danger'
    );
    const refreshButton: HTMLButtonElement = fixture.nativeElement.querySelector(
      'button.p-button-secondary'
    );
    const previewButton: HTMLButtonElement = fixture.nativeElement.querySelector(
      'form button[type="submit"]'
    );
    expect(port.rebuildCallCount).toBe(1);
    expect(rebuildButton.disabled).toBe(true);
    expect(refreshButton.disabled).toBe(true);
    expect(previewButton.disabled).toBe(true);
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
