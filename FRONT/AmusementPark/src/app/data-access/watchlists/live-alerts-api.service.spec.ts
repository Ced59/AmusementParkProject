import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { environment } from '../../../environments/environment';
import { LIVE_ALERTS_API_ENDPOINTS } from './live-alerts-api-endpoints';
import { LiveAlertsApiService } from './live-alerts-api.service';

describe('LiveAlertsApiService', (): void => {
  let service: LiveAlertsApiService;
  let http: HttpTestingController;

  beforeEach((): void => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    service = TestBed.inject(LiveAlertsApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach((): void => http.verify());

  it('loads the dashboard scoped to an attraction', (): void => {
    service.getDashboard('item / 1').subscribe();

    const request = http.expectOne(
      (candidate): boolean => candidate.url === `${environment.apiBaseUrl}${LIVE_ALERTS_API_ENDPOINTS.collection}`
        && candidate.params.get('targetId') === 'item / 1'
    );
    expect(request.request.method).toBe('GET');
    request.flush({ subscriptions: [], notifications: [], unreadCount: 0, retentionDays: 30 });
  });

  it('creates a temporary threshold alert', (): void => {
    const payload = {
      targetId: 'item-1',
      type: 'WaitBelow' as const,
      thresholdMinutes: 30,
      durationMinutes: 180
    };

    service.create(payload).subscribe();

    const request = http.expectOne(
      `${environment.apiBaseUrl}${LIVE_ALERTS_API_ENDPOINTS.collection}`
    );
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(payload);
    request.flush({});
  });

  it('encodes identifiers and carries optimistic versions on mutations', (): void => {
    service.delete('subscription / 1', 4).subscribe();
    service.markRead('notification / 1', 2).subscribe();
    service.dismiss('notification / 2', 3).subscribe();

    const deletion = http.expectOne(
      (candidate): boolean => candidate.url === `${environment.apiBaseUrl}me/live-alerts/subscription%20%2F%201`
        && candidate.params.get('expectedVersion') === '4'
    );
    expect(deletion.request.method).toBe('DELETE');
    deletion.flush(null);

    const read = http.expectOne(`${environment.apiBaseUrl}me/live-alerts/notifications/notification%20%2F%201/read`);
    expect(read.request.method).toBe('POST');
    expect(read.request.body).toEqual({ expectedVersion: 2 });
    read.flush(null);

    const dismiss = http.expectOne(`${environment.apiBaseUrl}me/live-alerts/notifications/notification%20%2F%202/dismiss`);
    expect(dismiss.request.method).toBe('POST');
    expect(dismiss.request.body).toEqual({ expectedVersion: 3 });
    dismiss.flush(null);
  });
});
