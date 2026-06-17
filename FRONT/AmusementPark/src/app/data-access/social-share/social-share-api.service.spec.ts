import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../environments/environment';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { AdminSocialShareStatsApiService } from './admin-social-share-stats-api.service';
import { SocialShareEventsApiService } from './social-share-events-api.service';

describe('SocialShare API services', () => {
  let eventsApiService: SocialShareEventsApiService;
  let statsApiService: AdminSocialShareStatsApiService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    eventsApiService = TestBed.inject(SocialShareEventsApiService);
    statsApiService = TestBed.inject(AdminSocialShareStatsApiService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('captures public share events through the public endpoint', () => {
    const body = {
      targetType: 'Park' as const,
      targetId: 'park-1',
      targetTitle: 'Test Park',
      url: 'https://example.test/fr/park/park-1/test-park',
      languageCode: 'fr',
      channel: 'Copy' as const
    };

    eventsApiService.captureEvent(body).subscribe((response) => {
      expect(response.accepted).toBeTrue();
    });

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}social-share/events`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(body);
    request.flush({ accepted: true, occurredAtUtc: '2026-06-17T10:00:00Z' });
  });

  it('loads admin social share stats with optional UTC bounds', () => {
    statsApiService.getStats({
      fromUtc: '2026-06-01T00:00:00Z',
      toUtc: '2026-06-17T23:59:59Z'
    }).subscribe((response) => {
      expect(response.totalEvents).toBe(3);
    });

    const request = httpTestingController.expectOne((candidate) => candidate.url === `${environment.apiBaseUrl}admin/social-share/stats`);
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('fromUtc')).toBe('2026-06-01T00:00:00Z');
    expect(request.request.params.get('toUtc')).toBe('2026-06-17T23:59:59Z');
    request.flush({
      fromUtc: '2026-06-01T00:00:00Z',
      toUtc: '2026-06-17T23:59:59Z',
      totalEvents: 3,
      anonymousEvents: 2,
      authenticatedEvents: 1,
      daily: [],
      channels: [],
      targetTypes: [],
      visitorKinds: [],
      topTargets: []
    });
  });
});
