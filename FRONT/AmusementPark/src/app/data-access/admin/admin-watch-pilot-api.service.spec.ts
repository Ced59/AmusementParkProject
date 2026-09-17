import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { environment } from '../../../environments/environment';
import { AdminWatchPilotApiService } from './admin-watch-pilot-api.service';

describe('AdminWatchPilotApiService', (): void => {
  let service: AdminWatchPilotApiService;
  let http: HttpTestingController;

  beforeEach((): void => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    service = TestBed.inject(AdminWatchPilotApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach((): void => http.verify());

  it('loads a bounded aggregate window from the admin endpoint', (): void => {
    service.getMetrics({
      fromUtc: '2026-09-01T00:00:00Z',
      toUtc: '2026-09-17T23:59:59Z'
    }).subscribe();

    const request = http.expectOne(
      (candidate): boolean => candidate.url === `${environment.apiBaseUrl}admin/watch-pilot/metrics`
    );
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('fromUtc')).toBe('2026-09-01T00:00:00Z');
    expect(request.request.params.get('toUtc')).toBe('2026-09-17T23:59:59Z');
    request.flush({});
  });
});
