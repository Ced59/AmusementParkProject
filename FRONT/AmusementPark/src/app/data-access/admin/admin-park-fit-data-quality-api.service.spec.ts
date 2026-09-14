import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { environment } from '../../../environments/environment';
import { AdminParkFitDataQualityApiService } from './admin-park-fit-data-quality-api.service';

describe('AdminParkFitDataQualityApiService', () => {
  let service: AdminParkFitDataQualityApiService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: provideCommonTestDependencies()
    });
    service = TestBed.inject(AdminParkFitDataQualityApiService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('loads a bounded audit page', () => {
    service.getPage(2, 12).subscribe((result) => {
      expect(result.items[0].parkName).toBe('Parc témoin');
      expect(result.pagination.currentPage).toBe(2);
    });

    const request = httpTestingController.expectOne(
      (candidate) => candidate.url ===
        `${environment.apiBaseUrl}admin/park-fit/data-quality`
    );
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('page')).toBe('2');
    expect(request.request.params.get('size')).toBe('12');
    request.flush({
      data: [{
        parkId: 'park-1',
        parkName: 'Parc témoin',
        status: 'Insufficient',
        coveragePercent: 0,
        visibleAttractionCount: 1,
        attractionWithConditionsCount: 0,
        decisionEligibleAttractionCount: 0,
        conditionCount: 0,
        decisionEligibleConditionCount: 0,
        issueItemCount: 1,
        missingSourceItemCount: 0,
        missingTimestampItemCount: 0,
        staleEvidenceItemCount: 0,
        ambiguousItemCount: 0,
        issues: ['MissingAccessConditions'],
        issueSamples: []
      }],
      pagination: { totalItems: 13, totalPages: 2, currentPage: 2, itemsPerPage: 12 }
    });
  });
});
