import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { NotificationEmailPreference } from '@app/models/watchlists/notification-email-preference.model';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { environment } from '../../../environments/environment';
import { NOTIFICATION_EMAIL_PREFERENCES_API_ENDPOINTS } from './notification-email-preferences-api-endpoints';
import { NotificationEmailPreferencesApiService } from './notification-email-preferences-api.service';

describe('NotificationEmailPreferencesApiService', () => {
  let service: NotificationEmailPreferencesApiService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    service = TestBed.inject(NotificationEmailPreferencesApiService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTestingController.verify());

  it('uses the authenticated owner preference endpoint', () => {
    const preference: NotificationEmailPreference = buildPreference();

    service.get().subscribe();
    const getRequest = httpTestingController.expectOne(
      `${environment.apiBaseUrl}${NOTIFICATION_EMAIL_PREFERENCES_API_ENDPOINTS.preference}`
    );
    expect(getRequest.request.method).toBe('GET');
    getRequest.flush(preference);

    service.update({
      emailDigestEnabled: true,
      consentAccepted: true,
      consentLocale: 'fr',
      expectedVersion: null
    }).subscribe();
    const updateRequest = httpTestingController.expectOne(
      `${environment.apiBaseUrl}${NOTIFICATION_EMAIL_PREFERENCES_API_ENDPOINTS.preference}`
    );
    expect(updateRequest.request.method).toBe('PUT');
    expect(updateRequest.request.body).toEqual({
      emailDigestEnabled: true,
      consentAccepted: true,
      consentLocale: 'fr',
      expectedVersion: null
    });
    updateRequest.flush(preference);
  });
});

function buildPreference(): NotificationEmailPreference {
  return {
    emailDigestEnabled: false,
    emailAvailable: true,
    maskedEmail: 'u***@example.com',
    consentTextVersion: 'watch-email-consent-v1',
    consentGrantedAtUtc: null,
    revokedAtUtc: null,
    version: null
  };
}
