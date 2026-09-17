import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { WatchPilotMetricsResult } from '@app/models/admin/watch-pilot/watch-pilot-metrics.models';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import {
  ADMIN_WATCH_PILOT_DATA_PORT,
  AdminWatchPilotDataPort
} from './admin-watch-pilot-data.port';
import { AdminWatchPilotFacade } from './admin-watch-pilot.facade';

describe('AdminWatchPilotFacade', (): void => {
  it('exposes only aggregate pilot metrics', (): void => {
    const port: AdminWatchPilotDataPort = {
      getMetrics: vi.fn().mockReturnValue(of(buildMetrics()))
    };
    TestBed.configureTestingModule({
      providers: [
        provideCommonTestDependencies(),
        AdminWatchPilotFacade,
        { provide: ADMIN_WATCH_PILOT_DATA_PORT, useValue: port }
      ]
    });
    const facade: AdminWatchPilotFacade = TestBed.inject(AdminWatchPilotFacade);

    facade.load();

    expect(facade.state().kind).toBe('ready');
    expect(facade.metrics()?.health.signal).toBe('Monitor');
    expect(facade.metrics()).not.toHaveProperty('userId');
    expect(facade.metrics()).not.toHaveProperty('notificationId');
  });
});

function buildMetrics(): WatchPilotMetricsResult {
  return {
    generatedAtUtc: '2026-09-17T12:00:00Z', fromUtc: '2026-09-01T00:00:00Z',
    toUtc: '2026-09-17T12:00:00Z', activeSubscriptions: 2,
    activeSubscriptionsByEventType: { OpeningDateConfirmed: 2 },
    eventsVerified: 3, eventsPublished: 2, eventsCorrected: 0, eventsRetracted: 0,
    notificationsDelivered: 20, duplicateNotifications: 0, notificationCenterOpens: 5,
    sourceOpens: 3, misleadingAlertReports: 0, subscriptionsRemoved: 1, digestsGenerated: 2,
    emailPending: 0, emailSucceeded: 2, emailFailed: 0, emailCancelled: 0,
    bounceCount: null, complaintCount: null, pendingOutboxEntries: 0, queueCountsByStatus: {},
    health: {
      duplicateRatePercent: 0, misleadingReportRatePercent: 0, emailFailureRatePercent: 0,
      averageDeliveryLatencyMinutes: 1, signal: 'Monitor', canExtendEventTypes: false,
      providerFeedbackAvailable: false
    },
    daily: [{
      date: '2026-09-17', notificationsDelivered: 20, digestsGenerated: 2,
      interactionCounts: { NotificationCenterOpened: 5 }
    }]
  };
}
