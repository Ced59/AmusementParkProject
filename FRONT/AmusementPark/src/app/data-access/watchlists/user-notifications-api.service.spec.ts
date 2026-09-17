import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { environment } from '../../../environments/environment';
import { USER_NOTIFICATIONS_API_ENDPOINTS } from './user-notifications-api-endpoints';
import { UserNotificationsApiService } from './user-notifications-api.service';

describe('UserNotificationsApiService', (): void => {
  let service: UserNotificationsApiService;
  let http: HttpTestingController;

  beforeEach((): void => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    service = TestBed.inject(UserNotificationsApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach((): void => http.verify());

  it('captures a misleading report against its owned notification', (): void => {
    service.capturePilotInteraction('MisleadingAlertReported', 'notification-1').subscribe();

    const request = http.expectOne(
      `${environment.apiBaseUrl}${USER_NOTIFICATIONS_API_ENDPOINTS.pilotInteractions}`
    );
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      interactionKind: 'MisleadingAlertReported',
      notificationId: 'notification-1'
    });
    request.flush(null);
  });
});
