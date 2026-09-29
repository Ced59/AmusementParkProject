import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import {
  LiveOperationalScope,
  LiveOperationsDashboard,
  LiveQualityReplay,
  LiveWaitForecastBacktest,
  UpdateLiveOperationalControlRequest
} from '@app/models/admin/live-data/live-operations.models';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import {
  ADMIN_LIVE_OPERATIONS_DATA_PORT,
  AdminLiveOperationsDataPort
} from './admin-live-operations-data.port';
import { AdminLiveOperationsFacade } from './admin-live-operations.facade';

describe('AdminLiveOperationsFacade', () => {
  it('refreshes the dashboard after applying a control', () => {
    const dashboard: LiveOperationsDashboard = createDashboard();
    const scope: LiveOperationalScope = dashboard.scopes[0];
    const port: AdminLiveOperationsDataPort = {
      getDashboard: vi.fn().mockReturnValue(of(dashboard)),
      updateControl: vi.fn().mockReturnValue(of(scope)),
      replayQuarantine: vi.fn(),
      getForecastBacktest: vi.fn()
    };
    TestBed.configureTestingModule({
      providers: [
        provideCommonTestDependencies(),
        AdminLiveOperationsFacade,
        { provide: ADMIN_LIVE_OPERATIONS_DATA_PORT, useValue: port }
      ]
    });
    const facade: AdminLiveOperationsFacade = TestBed.inject(AdminLiveOperationsFacade);
    const request: UpdateLiveOperationalControlRequest = {
      scopeType: 'Source', sourceId: 'source', externalEntityId: null,
      internalParkId: null, targetType: null, internalTargetId: null,
      collectionEnabled: false, publicReadEnabled: true,
      expectedRevision: 0, reason: 'Provider incident'
    };

    facade.load();
    facade.update('Source:source', request);

    expect(facade.state().kind).toBe('ready');
    expect(facade.feedbackKey()).toBe('admin.liveOperations.messages.updated');
    expect(port.updateControl).toHaveBeenCalledWith(request);
    expect(port.getDashboard).toHaveBeenCalledTimes(2);
  });

  it('reports when every quarantine incident remains blocked', () => {
    const dashboard: LiveOperationsDashboard = createDashboard();
    const replay: LiveQualityReplay = {
      examinedCount: 3,
      resolvedCount: 0,
      stillBlockedCount: 3,
      persistedCount: 0,
      ignoredAsOlderCount: 0
    };
    const port: AdminLiveOperationsDataPort = {
      getDashboard: vi.fn().mockReturnValue(of(dashboard)),
      updateControl: vi.fn(),
      replayQuarantine: vi.fn().mockReturnValue(of(replay)),
      getForecastBacktest: vi.fn()
    };
    TestBed.configureTestingModule({
      providers: [
        provideCommonTestDependencies(),
        AdminLiveOperationsFacade,
        { provide: ADMIN_LIVE_OPERATIONS_DATA_PORT, useValue: port }
      ]
    });
    const facade: AdminLiveOperationsFacade = TestBed.inject(AdminLiveOperationsFacade);

    facade.replayQuarantine();

    expect(facade.feedbackKey()).toBe('admin.liveOperations.messages.replayBlocked');
    expect(port.getDashboard).toHaveBeenCalledTimes(1);
  });

  it('runs a forecast backtest without exposing a public prediction', () => {
    const dashboard: LiveOperationsDashboard = createDashboard();
    const backtest: LiveWaitForecastBacktest = createBacktest();
    const port: AdminLiveOperationsDataPort = {
      getDashboard: vi.fn().mockReturnValue(of(dashboard)),
      updateControl: vi.fn(),
      replayQuarantine: vi.fn(),
      getForecastBacktest: vi.fn().mockReturnValue(of(backtest))
    };
    TestBed.configureTestingModule({
      providers: [
        provideCommonTestDependencies(),
        AdminLiveOperationsFacade,
        { provide: ADMIN_LIVE_OPERATIONS_DATA_PORT, useValue: port }
      ]
    });
    const facade: AdminLiveOperationsFacade = TestBed.inject(AdminLiveOperationsFacade);

    facade.runForecastBacktest('item-1');

    expect(port.getForecastBacktest).toHaveBeenCalledWith('item-1');
    expect(facade.backtest()).toEqual(backtest);
    expect(facade.backtestPendingTargetId()).toBeNull();
  });
});

function createBacktest(): LiveWaitForecastBacktest {
  return {
    targetDisplayName: 'Taron', parkDisplayName: 'Phantasialand',
    studyVersion: 'live-wait-backtest-v1', verdict: 'InsufficientData',
    reasons: ['InsufficientEvaluationPoints'],
    evaluationFromUtc: '2026-06-01T00:00:00Z', evaluationToUtc: '2026-09-01T00:00:00Z',
    timeZoneId: 'Europe/Paris', sourceObservationCount: 10, hourlyPointCount: 2,
    evaluationPointCount: 0, evaluationDays: 0, baseline: null, candidate: null,
    maeImprovementPercent: null, intervalMethod: 'rolling-weekday-hour-p10-p90-v1',
    intervalCoveragePercent: null, medianIntervalWidthMinutes: null,
    olderCandidateMaeMinutes: null, recentCandidateMaeMinutes: null, driftPercent: null,
    driftDetected: false,
    policy: { trainingWindowDays: 84, minimumBaselineTrainingDays: 28,
      minimumCandidateTrainingDays: 8, minimumEvaluationDays: 14,
      minimumEvaluationPoints: 100, requiredMaeImprovementPercent: 5,
      nominalIntervalCoveragePercent: 80, minimumIntervalCoveragePercent: 70,
      maximumUsefulMedianIntervalWidthMinutes: 60, driftThresholdPercent: 25,
      minimumDriftIncreaseMinutes: 3 },
    generatedAtUtc: '2026-09-29T00:00:00Z'
  };
}

function createDashboard(): LiveOperationsDashboard {
  const scope: LiveOperationalScope = {
    scopeType: 'Source', sourceId: 'source', externalEntityId: null,
    internalParkId: null, targetType: null, internalTargetId: null,
    displayName: 'Source', parentDisplayName: null,
    collectionEnabled: true, publicReadEnabled: true,
    effectiveCollectionEnabled: true, effectivePublicReadEnabled: true,
    revision: 0, reason: null, changedByUserId: null, recordedAtUtc: null
  };
  return {
    sourceId: 'source', sourceDisplayName: 'Source',
    configuredCollectionEnabled: true, configuredPublicReadEnabled: true,
    adapterVersion: '1', transformationVersion: '1', usagePolicyVersion: '1',
    termsUrl: 'https://example.com/terms', usagePolicyReviewedAtUtc: '2026-09-29T00:00:00Z',
    attributionText: 'Source', attributionUrl: 'https://example.com',
    polling: { externalEntityId: 'park', nextAttemptAtUtc: null, lastPolledAtUtc: null,
      lastSuccessfulPollAtUtc: null, consecutiveFailures: 0, circuitOpenUntilUtc: null,
      lastDisposition: null, leaseActive: false, leaseExpiresAtUtc: null },
    summary: { mappingCount: 1, eligibleMappingCount: 1, candidateMappingCount: 0,
      suspendedMappingCount: 0, pendingIncidentCount: 0, replayablePendingIncidentCount: 0 },
    scopes: [scope], generatedAtUtc: '2026-09-29T09:00:00Z'
  };
}
