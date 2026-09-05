import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { environment } from '../../../environments/environment';
import { AdminPassportBetaApiService } from './admin-passport-beta-api.service';

describe('AdminPassportBetaApiService', () => {
  let service: AdminPassportBetaApiService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: provideCommonTestDependencies()
    });
    service = TestBed.inject(AdminPassportBetaApiService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('loads only aggregate beta metrics with optional UTC bounds', () => {
    service.getMetrics({
      fromUtc: '2026-09-01T00:00:00.000Z',
      toUtc: '2026-09-05T23:59:59.999Z'
    }).subscribe((result) => {
      expect(result.usersWithSecondCompletedVisit).toBe(2);
      expect(result).not.toHaveProperty('userId');
      expect(result).not.toHaveProperty('visitId');
      expect(result).not.toHaveProperty('parkId');
    });

    const request = httpTestingController.expectOne(
      (candidate) => candidate.url === `${environment.apiBaseUrl}admin/passport-beta/metrics`
    );
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('fromUtc')).toBe('2026-09-01T00:00:00.000Z');
    expect(request.request.params.get('toUtc')).toBe('2026-09-05T23:59:59.999Z');
    request.flush({
      generatedAtUtc: '2026-09-05T12:00:00.000Z',
      fromUtc: '2026-09-01T00:00:00.000Z',
      toUtc: '2026-09-05T23:59:59.999Z',
      createdVisits: 7,
      completedVisits: 5,
      usersWithCompletedVisit: 4,
      usersWithSecondCompletedVisit: 2,
      repeatUsageRatePercent: 50,
      repeatUsageSignal: 'Emerging',
      requiresQualitativeValidation: true,
      daily: []
    });
  });
});
