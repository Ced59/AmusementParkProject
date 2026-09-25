import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { TripPilotMetricsResult } from '@app/models/admin/trip-pilot/trip-pilot-metrics.models';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { ADMIN_TRIP_PILOT_DATA_PORT, AdminTripPilotDataPort } from './admin-trip-pilot-data.port';
import { AdminTripPilotFacade } from './admin-trip-pilot.facade';

describe('AdminTripPilotFacade', () => {
  it('exposes aggregate metrics without member identity or private text', () => {
    const metrics: TripPilotMetricsResult = {
      generatedAtUtc: '2027-04-05T10:00:00Z',
      totalPlans: 12,
      collaborativePlans: 5,
      plansWithPreferences: 8,
      plansWithDecisions: 4,
      expiredInvitations: 2,
      enabledNotificationSubscriptions: 3,
      auditEvents: 42,
      pendingAuditMarkers: 0,
      activityCounts: { CandidateAdded: 9 }
    };
    const port: AdminTripPilotDataPort = { getMetrics: vi.fn().mockReturnValue(of(metrics)) };
    TestBed.configureTestingModule({
      providers: [
        provideCommonTestDependencies(),
        AdminTripPilotFacade,
        { provide: ADMIN_TRIP_PILOT_DATA_PORT, useValue: port }
      ]
    });
    const facade: AdminTripPilotFacade = TestBed.inject(AdminTripPilotFacade);

    facade.load();

    expect(facade.state().kind).toBe('ready');
    expect(facade.metrics()?.collaborativePlans).toBe(5);
    expect(facade.metrics()).not.toHaveProperty('userId');
    expect(facade.metrics()).not.toHaveProperty('tripTitle');
    expect(facade.metrics()).not.toHaveProperty('note');
  });
});
