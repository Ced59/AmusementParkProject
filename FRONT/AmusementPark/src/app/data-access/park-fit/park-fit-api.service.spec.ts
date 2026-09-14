import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import {
  ParkFitSearchRequest,
  ParkFitSearchResponse,
  ParkFitSourceReportRequest
} from '@app/models/park-fit/park-fit-search.models';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { SKIP_AUTHORIZATION_HEADER } from '@core/http/auth/auth-request-policy';
import { environment } from '../../../environments/environment';
import { ParkFitApiService } from './park-fit-api.service';

describe('ParkFitApiService', () => {
  let service: ParkFitApiService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    service = TestBed.inject(ParkFitApiService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('posts anonymous criteria without transfer cache', () => {
    const request: ParkFitSearchRequest = buildRequest();
    const response: ParkFitSearchResponse = buildResponse();
    let actual: ParkFitSearchResponse | null = null;

    service.search(request).subscribe((value: ParkFitSearchResponse): void => {
      actual = value;
    });

    const pending = httpTestingController.expectOne(
      `${environment.apiBaseUrl}public/park-fit/search`
    );
    expect(pending.request.method).toBe('POST');
    expect(pending.request.body).toEqual(request);
    expect(pending.request.context.get(SKIP_AUTHORIZATION_HEADER)).toBe(true);
    pending.flush(response);

    expect(actual).toEqual(response);
  });

  it('submits a source report anonymously without transfer cache', () => {
    const request: ParkFitSourceReportRequest = {
      parkId: 'park-1',
      evidenceKind: 'OpeningCalendar',
      sourceUrl: 'https://example.org/calendar',
      sourceReference: null,
      reason: 'Outdated',
      details: null
    };

    service.submitReport(request).subscribe();

    const pending = httpTestingController.expectOne(
      `${environment.apiBaseUrl}public/park-fit/reports`
    );
    expect(pending.request.method).toBe('POST');
    expect(pending.request.body).toEqual(request);
    expect(pending.request.context.get(SKIP_AUTHORIZATION_HEADER)).toBe(true);
    pending.flush(null);
  });
});

function buildRequest(): ParkFitSearchRequest {
  return {
    evaluationDate: '2026-10-10',
    members: [{
      heightCentimeters: 120,
      minimumAgeYears: 8,
      maximumAgeYears: 8,
      canBeAccompanied: true,
      companionMinimumAgeYears: 35,
      companionMaximumAgeYears: 35
    }],
    preferredAttractionTypes: ['FamilyRide'],
    preferIndoor: false,
    countryCode: null,
    originLatitude: null,
    originLongitude: null,
    unknownDataPolicy: 'KeepWithWarning',
    maximumResults: 10
  };
}

function buildResponse(): ParkFitSearchResponse {
  return {
    methodVersion: 'park-fit-2026-01',
    evaluationDate: '2026-10-10',
    evaluatedAtUtc: '2026-09-14T10:00:00Z',
    totalCandidateCount: 1,
    inspectedCandidateCount: 1,
    qualityEligibleCandidateCount: 1,
    qualityRejectedCandidateCount: 0,
    operationallySuspendedCandidateCount: 0,
    candidatePoolTruncated: false,
    qualityStatusCounts: { Eligible: 1 },
    qualityIssueCounts: {},
    parks: []
  };
}
