import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import {
  LiveTargetMapping,
  LiveTargetMappingPage,
  ReviewLiveTargetMappingRequest
} from '@app/models/admin/live-data/live-target-mapping.models';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import {
  ADMIN_LIVE_TARGET_MAPPINGS_DATA_PORT,
  AdminLiveTargetMappingsDataPort
} from './admin-live-target-mappings-data.port';
import { AdminLiveTargetMappingsFacade } from './admin-live-target-mappings.facade';

describe('AdminLiveTargetMappingsFacade', () => {
  it('loads mappings and refreshes the page after a review', () => {
    const mapping: LiveTargetMapping = createMapping();
    const page: LiveTargetMappingPage = {
      data: [mapping],
      pagination: { totalItems: 1, totalPages: 1, currentPage: 1, itemsPerPage: 25 }
    };
    const port: AdminLiveTargetMappingsDataPort = {
      search: vi.fn().mockReturnValue(of(page)),
      createCandidate: vi.fn(),
      review: vi.fn().mockReturnValue(of(mapping))
    };
    TestBed.configureTestingModule({
      providers: [
        provideCommonTestDependencies(),
        AdminLiveTargetMappingsFacade,
        { provide: ADMIN_LIVE_TARGET_MAPPINGS_DATA_PORT, useValue: port }
      ]
    });
    const facade: AdminLiveTargetMappingsFacade = TestBed.inject(
      AdminLiveTargetMappingsFacade
    );
    const request: ReviewLiveTargetMappingRequest = {
      expectedRevision: 1,
      decision: 'Verify',
      internalTargetId: 'item-1',
      parkId: 'park-1',
      reviewNote: null
    };

    facade.load({ page: 1 });
    facade.review(mapping.mappingId, request, { page: 1 });

    expect(facade.state().kind).toBe('ready');
    expect(facade.mappings()).toEqual([mapping]);
    expect(facade.feedbackKey()).toBe('admin.liveMappings.messages.reviewed');
    expect(port.review).toHaveBeenCalledWith(mapping.mappingId, request);
    expect(port.search).toHaveBeenCalledTimes(2);
  });

  it('represents a successful empty search as an empty screen', () => {
    const port: AdminLiveTargetMappingsDataPort = {
      search: vi.fn().mockReturnValue(of({
        data: [],
        pagination: { totalItems: 0, totalPages: 0, currentPage: 1, itemsPerPage: 25 }
      })),
      createCandidate: vi.fn(),
      review: vi.fn()
    };
    TestBed.configureTestingModule({
      providers: [
        provideCommonTestDependencies(),
        AdminLiveTargetMappingsFacade,
        { provide: ADMIN_LIVE_TARGET_MAPPINGS_DATA_PORT, useValue: port }
      ]
    });
    const facade: AdminLiveTargetMappingsFacade = TestBed.inject(
      AdminLiveTargetMappingsFacade
    );

    facade.load();

    expect(facade.state().kind).toBe('empty');
    expect(facade.mappings()).toEqual([]);
  });
});

function createMapping(): LiveTargetMapping {
  return {
    mappingId: '10000000-0000-0000-0000-000000000001',
    version: '10000000000000000000000000000001:1',
    sourceId: 'themeparks-wiki',
    externalTarget: {
      type: 'ParkItem',
      id: 'external-item-1',
      parentId: 'external-park-1',
      displayName: 'Black Mamba',
      parentDisplayName: 'Phantasialand',
      countryCode: 'DE'
    },
    target: null,
    status: 'Candidate',
    confidence: 'Low',
    validFromUtc: '2026-09-28T17:00:00Z',
    validToUtc: null,
    revision: 1,
    supersedesRevision: null,
    reviewedByUserId: null,
    reviewNote: null,
    recordedAtUtc: '2026-09-28T17:00:00Z',
    isEligibleForLiveUse: false
  };
}
