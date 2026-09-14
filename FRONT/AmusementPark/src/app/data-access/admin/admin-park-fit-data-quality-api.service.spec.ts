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

  it('loads only pending source reports', () => {
    service.getPendingReports(1, 12).subscribe((result) => {
      expect(result.items[0].parkName).toBe('Parc témoin');
      expect(result.pagination.totalItems).toBe(1);
    });

    const request = httpTestingController.expectOne(
      (candidate) => candidate.url === `${environment.apiBaseUrl}admin/park-fit/reports`
    );
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('page')).toBe('1');
    expect(request.request.params.get('size')).toBe('12');
    expect(request.request.params.get('status')).toBe('Pending');
    request.flush({
      data: [{
        reportId: 'report-1',
        parkId: 'park-1',
        parkName: 'Parc témoin',
        evidenceKind: 'OpeningCalendar',
        reason: 'Outdated',
        status: 'Pending',
        submittedAtUtc: '2026-09-14T08:00:00Z',
        revision: 0
      }],
      pagination: { totalItems: 1, totalPages: 1, currentPage: 1, itemsPerPage: 12 }
    });
  });

  it('reviews a report with its expected revision', () => {
    service.reviewReport('report/1', {
      decision: 'Resolved',
      decisionNote: 'Source corrigée',
      expectedRevision: 3
    }).subscribe();

    const request = httpTestingController.expectOne(
      `${environment.apiBaseUrl}admin/park-fit/reports/report%2F1`
    );
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({
      decision: 'Resolved',
      decisionNote: 'Source corrigée',
      expectedRevision: 3
    });
    request.flush(null);
  });

  it('changes only the Park Fit operational state of a park', () => {
    service.changeOperationalStatus('park/1', {
      targetState: 'Suspended',
      reason: 'Calendrier à vérifier',
      expectedRevision: 2
    }).subscribe();

    const request = httpTestingController.expectOne(
      `${environment.apiBaseUrl}admin/park-fit/parks/park%2F1/operational-status`
    );
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({
      targetState: 'Suspended',
      reason: 'Calendrier à vérifier',
      expectedRevision: 2
    });
    request.flush(null);
  });
});
