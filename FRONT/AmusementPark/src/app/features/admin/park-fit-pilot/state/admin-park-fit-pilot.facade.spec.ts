import type { MockedObject } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { ParkFitPilotMetricsResult } from '@app/models/admin/park-fit/park-fit-pilot-metrics.models';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import {
  ADMIN_PARK_FIT_PILOT_STATE_DATA_PORT,
  AdminParkFitPilotStateDataPort
} from './admin-park-fit-pilot-state-data.port';
import { AdminParkFitPilotFacade } from './admin-park-fit-pilot.facade';

describe('AdminParkFitPilotFacade', () => {
  it('exposes the aggregate pilot signal without visitor identifiers', () => {
    const metrics: ParkFitPilotMetricsResult = buildMetrics();
    const port: MockedObject<AdminParkFitPilotStateDataPort> = {
      getMetrics: vi.fn().mockReturnValue(of(metrics))
    } as unknown as MockedObject<AdminParkFitPilotStateDataPort>;
    TestBed.configureTestingModule({
      providers: [
        provideCommonTestDependencies(),
        AdminParkFitPilotFacade,
        { provide: ADMIN_PARK_FIT_PILOT_STATE_DATA_PORT, useValue: port }
      ]
    });
    const facade: AdminParkFitPilotFacade = TestBed.inject(AdminParkFitPilotFacade);

    facade.load({ fromUtc: '2026-09-01T00:00:00Z' });

    expect(facade.state().kind).toBe('ready');
    expect(facade.signal()).toBe('Encouraging');
    expect(facade.daily()).toHaveLength(1);
    expect(facade.metrics()).not.toHaveProperty('userId');
    expect(facade.metrics()).not.toHaveProperty('parkId');
  });
});

function buildMetrics(): ParkFitPilotMetricsResult {
  return {
    generatedAtUtc: '2026-09-14T20:00:00Z',
    fromUtc: '2026-09-01T00:00:00Z',
    toUtc: '2026-09-14T20:00:00Z',
    searchesStarted: 10,
    searchesCompleted: 8,
    searchesFailed: 1,
    searchesAbandoned: 1,
    explanationsViewed: 3,
    comparisonsOpened: 2,
    sourceReports: 1,
    outdatedSourceReports: 1,
    health: {
      completionRatePercent: 80,
      noResultRatePercent: 12.5,
      significantUnknownRatePercent: 12.5,
      explanationOpenRatePercent: 37.5,
      comparisonOpenRatePercent: 25,
      signal: 'Encouraging',
      requiresQualitativeReview: true
    },
    resultBandCounts: { One: 1, FiveOrMore: 7 },
    unknownLevelCounts: { None: 7, Significant: 1 },
    durationBandCounts: { UnderHalfSecond: 8 },
    failureKindCounts: { Technical: 1 },
    comparisonSizeCounts: { Two: 2 },
    qualityIssueCounts: { MissingOpeningCalendar: 1 },
    zeroResultQualityIssueCounts: { MissingOpeningCalendar: 1 },
    methodVersionCounts: { 'park-fit-2026-02': 8 },
    daily: [{
      date: '2026-09-14',
      eventCounts: { SearchCompleted: 8 },
      sourceReports: 1,
      outdatedSourceReports: 1
    }]
  };
}
