import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { PublicLiveTarget } from '@app/models/live-data/public-live.models';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { environment } from '../../../environments/environment';
import { PublicLiveApiService } from './public-live-api.service';

describe('PublicLiveApiService', () => {
  let service: PublicLiveApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    service = TestBed.inject(PublicLiveApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('revalidates with the stored ETag and reuses the cached body on 304', () => {
    const target: PublicLiveTarget = createTarget();
    const received: PublicLiveTarget[] = [];

    service.getParkItem('item/one').subscribe((result: PublicLiveTarget) => received.push(result));
    const firstRequest = http.expectOne(`${environment.apiBaseUrl}public/live/items/item%2Fone`);
    expect(firstRequest.request.headers.has('If-None-Match')).toBe(false);
    firstRequest.flush(target, { headers: { ETag: '"live-v1"' } });

    service.getParkItem('item/one').subscribe((result: PublicLiveTarget) => received.push(result));
    const conditionalRequest = http.expectOne(`${environment.apiBaseUrl}public/live/items/item%2Fone`);
    expect(conditionalRequest.request.headers.get('If-None-Match')).toBe('"live-v1"');
    conditionalRequest.flush(null, { status: 304, statusText: 'Not Modified' });

    expect(received).toEqual([target, target]);
  });
});

function createTarget(): PublicLiveTarget {
  return {
    targetId: 'item/one',
    targetType: 'ParkItem',
    displayName: 'Example attraction',
    parkId: 'park-1',
    parkDisplayName: 'Example park',
    availability: 'Current',
    status: 'Open',
    queues: [],
    asOfUtc: '2026-09-29T10:00:00Z',
    observedAtUtc: '2026-09-29T09:59:00Z',
    receivedAtUtc: '2026-09-29T09:59:01Z',
    ageSeconds: 60,
    freshness: 'Fresh',
    expiresAtUtc: '2026-09-29T10:04:00Z',
    source: null,
    confidence: 'High'
  };
}
