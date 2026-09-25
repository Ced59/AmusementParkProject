import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { TripPilotMetricsResult } from '@app/models/admin/trip-pilot/trip-pilot-metrics.models';
import {
  COMMON_TEST_IMPORTS,
  provideCommonTestDependencies
} from '@app/testing/common-test-providers';
import {
  ADMIN_TRIP_PILOT_DATA_PORT,
  AdminTripPilotDataPort
} from '../../state/admin-trip-pilot-data.port';
import { AdminTripPilotComponent } from './admin-trip-pilot.component';

describe('AdminTripPilotComponent', () => {
  it('uses actor-free aggregate labels for activity bars', async () => {
    const metrics: TripPilotMetricsResult = {
      generatedAtUtc: '2027-04-05T10:00:00Z',
      totalPlans: 1,
      collaborativePlans: 1,
      plansWithPreferences: 0,
      plansWithDecisions: 0,
      expiredInvitations: 0,
      enabledNotificationSubscriptions: 1,
      auditEvents: 2,
      pendingAuditMarkers: 0,
      activityCounts: { CandidateAdded: 2 }
    };
    const dataPort: AdminTripPilotDataPort = {
      getMetrics: vi.fn().mockReturnValue(of(metrics))
    };
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, AdminTripPilotComponent],
      providers: [
        ...provideCommonTestDependencies(),
        { provide: ADMIN_TRIP_PILOT_DATA_PORT, useValue: dataPort }
      ]
    }).compileComponents();
    const fixture: ComponentFixture<AdminTripPilotComponent> =
      TestBed.createComponent(AdminTripPilotComponent);

    fixture.detectChanges();

    const text: string = fixture.nativeElement.textContent;
    expect(text).toContain('admin.tripPilot.activity.labels.candidateAdded');
    expect(text).not.toContain('trips.activity.kinds.candidateAdded');
  });
});
