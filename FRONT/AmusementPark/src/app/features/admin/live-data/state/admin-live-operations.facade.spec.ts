import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import {
  LiveOperationalScope,
  LiveOperationsDashboard,
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
      replayQuarantine: vi.fn()
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
});

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
