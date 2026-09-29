import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import {
  LiveOperationalScope,
  UpdateLiveOperationalControlRequest
} from '@app/models/admin/live-data/live-operations.models';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { environment } from '../../../environments/environment';
import { AdminLiveOperationsApiService } from './admin-live-operations-api.service';

describe('AdminLiveOperationsApiService', () => {
  it('updates a versioned operational control', () => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    const service: AdminLiveOperationsApiService = TestBed.inject(AdminLiveOperationsApiService);
    const http: HttpTestingController = TestBed.inject(HttpTestingController);
    const request: UpdateLiveOperationalControlRequest = {
      scopeType: 'Source', sourceId: 'source', externalEntityId: null,
      internalParkId: null, targetType: null, internalTargetId: null,
      collectionEnabled: false, publicReadEnabled: false,
      expectedRevision: 0, reason: 'Provider incident'
    };
    const response: LiveOperationalScope = createScope();

    service.updateControl(request).subscribe(
      (scope: LiveOperationalScope): void => expect(scope).toEqual(response)
    );

    const pending = http.expectOne(`${environment.apiBaseUrl}admin/live/operations/controls`);
    expect(pending.request.method).toBe('PUT');
    expect(pending.request.body).toEqual(request);
    pending.flush(response);
    http.verify();
  });

  it('replays a bounded quarantine batch', () => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    const service: AdminLiveOperationsApiService = TestBed.inject(AdminLiveOperationsApiService);
    const http: HttpTestingController = TestBed.inject(HttpTestingController);

    service.replayQuarantine(100).subscribe();

    const pending = http.expectOne(
      `${environment.apiBaseUrl}admin/live/quality/quarantine/replay`
    );
    expect(pending.request.method).toBe('POST');
    expect(pending.request.body).toEqual({ maximumCount: 100 });
    pending.flush({ examinedCount: 0, resolvedCount: 0, stillBlockedCount: 0, persistedCount: 0, ignoredAsOlderCount: 0 });
    http.verify();
  });
});

function createScope(): LiveOperationalScope {
  return {
    scopeType: 'Source', sourceId: 'source', externalEntityId: null,
    internalParkId: null, targetType: null, internalTargetId: null,
    displayName: 'Source', parentDisplayName: null,
    collectionEnabled: false, publicReadEnabled: false,
    effectiveCollectionEnabled: false, effectivePublicReadEnabled: false,
    revision: 1, reason: 'Provider incident', changedByUserId: 'admin',
    recordedAtUtc: '2026-09-29T09:00:00Z'
  };
}
