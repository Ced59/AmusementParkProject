import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import {
  LiveTargetMapping,
  LiveTargetMappingPage,
  ReviewLiveTargetMappingRequest
} from '@app/models/admin/live-data/live-target-mapping.models';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { environment } from '../../../environments/environment';
import { AdminLiveTargetMappingsApiService } from './admin-live-target-mappings-api.service';

describe('AdminLiveTargetMappingsApiService', () => {
  it('encodes filters without sending blank optional values', () => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    const service: AdminLiveTargetMappingsApiService = TestBed.inject(
      AdminLiveTargetMappingsApiService
    );
    const http: HttpTestingController = TestBed.inject(HttpTestingController);
    const page: LiveTargetMappingPage = {
      data: [],
      pagination: { totalItems: 0, totalPages: 0, currentPage: 2, itemsPerPage: 10 }
    };

    service.search({
      page: 2,
      pageSize: 10,
      sourceId: ' ',
      status: 'Candidate',
      search: 'Black Mamba'
    }).subscribe((result: LiveTargetMappingPage): void => expect(result).toEqual(page));

    const request = http.expectOne((candidate): boolean =>
      candidate.url === `${environment.apiBaseUrl}admin/live/mappings`
      && candidate.params.get('page') === '2'
      && candidate.params.get('pageSize') === '10'
      && candidate.params.get('status') === 'Candidate'
      && candidate.params.get('search') === 'Black Mamba'
      && !candidate.params.has('sourceId')
    );
    request.flush(page);
    http.verify();
  });

  it('posts a versioned review to the selected mapping', () => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    const service: AdminLiveTargetMappingsApiService = TestBed.inject(
      AdminLiveTargetMappingsApiService
    );
    const http: HttpTestingController = TestBed.inject(HttpTestingController);
    const review: ReviewLiveTargetMappingRequest = {
      expectedRevision: 1,
      decision: 'Verify',
      internalTargetId: 'item-1',
      parkId: 'park-1',
      reviewNote: null
    };
    const response: LiveTargetMapping = createMapping();

    service.review('mapping/unsafe', review).subscribe(
      (mapping: LiveTargetMapping): void => expect(mapping).toEqual(response)
    );

    const request = http.expectOne(
      `${environment.apiBaseUrl}admin/live/mappings/mapping%2Funsafe/review`
    );
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(review);
    request.flush(response);
    http.verify();
  });
});

function createMapping(): LiveTargetMapping {
  return {
    mappingId: 'mapping-1',
    version: 'mapping-1:1',
    sourceId: 'themeparks-wiki',
    externalTarget: {
      type: 'Park',
      id: 'external-park-1',
      parentId: null,
      displayName: 'Phantasialand',
      parentDisplayName: null,
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
