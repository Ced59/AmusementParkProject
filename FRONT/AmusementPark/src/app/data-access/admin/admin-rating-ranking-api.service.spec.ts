import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { RatingRankingPolicyCandidateRequest } from '@app/models/admin/ratings/rating-ranking-administration.models';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { environment } from '../../../environments/environment';
import { AdminRatingRankingApiService } from './admin-rating-ranking-api.service';

describe('AdminRatingRankingApiService', () => {
  let service: AdminRatingRankingApiService;
  let httpTestingController: HttpTestingController;
  const baseUrl: string = `${environment.apiBaseUrl}admin/ratings/ranking-management`;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    service = TestBed.inject(AdminRatingRankingApiService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('loads the protected ranking dashboard', () => {
    service.getDashboard().subscribe();

    const request = httpTestingController.expectOne(baseUrl);
    expect(request.request.method).toBe('GET');
    request.flush({});
  });

  it('submits the complete candidate policy for an impact preview', () => {
    const candidate: RatingRankingPolicyCandidateRequest = createCandidate();

    service.previewImpact(candidate).subscribe();

    const request = httpTestingController.expectOne(`${baseUrl}/preview`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(candidate);
    request.flush({});
  });

  it('always sends the explicit rebuild confirmation', () => {
    service.rebuild().subscribe();

    const request = httpTestingController.expectOne(`${baseUrl}/rebuild`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ confirmed: true });
    request.flush({});
  });
});

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
