import { HttpClient, HttpHeaders, HttpResponse } from '@angular/common/http';
import { of } from 'rxjs';

import {
  CreatePassportRideOccurrencesBatchRequest,
  PassportRideOccurrence
} from '@app/models/passport/passport-ride-occurrence.models';
import { environment } from '../../../environments/environment';
import { PassportRideOccurrencesApiService } from './passport-ride-occurrences-api.service';

describe('PassportRideOccurrencesApiService', () => {
  it('loads private pages with transfer caching disabled', () => {
    const httpClient = { get: vi.fn().mockReturnValue(of({ items: [], nextCursor: null })) };
    const service: PassportRideOccurrencesApiService = new PassportRideOccurrencesApiService(
      httpClient as unknown as HttpClient
    );

    service.list('visit/one', 'next+cursor', 25).subscribe();

    expect(httpClient.get).toHaveBeenCalledWith(
      `${environment.apiBaseUrl}me/passport/visits/visit%2Fone/occurrences?limit=25&cursor=next%2Bcursor`,
      { transferCache: false }
    );
  });

  it('sends idempotency and exposes replay and normalization evidence for a batch', () => {
    const occurrence: PassportRideOccurrence = createOccurrence();
    const response: HttpResponse<PassportRideOccurrence[]> = new HttpResponse({
      body: [occurrence],
      headers: new HttpHeaders({
        'Idempotency-Replayed': 'true',
        'Ride-Order-Normalized': 'true'
      })
    });
    const httpClient = { post: vi.fn().mockReturnValue(of(response)) };
    const service: PassportRideOccurrencesApiService = new PassportRideOccurrencesApiService(
      httpClient as unknown as HttpClient
    );
    const request: CreatePassportRideOccurrencesBatchRequest = {
      items: [{
        parkItemId: 'ride-1',
        moment: { localTime: null, isApproximate: false },
        status: 'Completed',
        privateNote: null,
        confirmHistoricalConflict: false,
        count: 1
      }]
    };

    service.addBatch('visit-1', request, 'operation-1').subscribe((result) => {
      expect(result.occurrences).toEqual([occurrence]);
      expect(result.wasReplayed).toBe(true);
      expect(result.wasOrderNormalized).toBe(true);
    });

    const call: unknown[] = httpClient.post.mock.calls[0];
    expect(call[0]).toBe(`${environment.apiBaseUrl}me/passport/visits/visit-1/occurrences:batch`);
    expect(call[1]).toBe(request);
    const options = call[2] as { headers: HttpHeaders; observe: string };
    expect(options.headers.get('Idempotency-Key')).toBe('operation-1');
    expect(options.observe).toBe('response');
  });

  it('uses optimistic versioning for delete and reorder mutations', () => {
    const response: HttpResponse<PassportRideOccurrence> = new HttpResponse({ body: createOccurrence() });
    const httpClient = {
      delete: vi.fn().mockReturnValue(of(undefined)),
      post: vi.fn().mockReturnValue(of(response))
    };
    const service: PassportRideOccurrencesApiService = new PassportRideOccurrencesApiService(
      httpClient as unknown as HttpClient
    );

    service.delete('visit-1', 'ride-1', 7).subscribe();
    service.reorder('visit-1', {
      occurrenceId: 'ride-1',
      expectedVersion: 7,
      anchorOccurrenceId: null,
      placement: 'First'
    }, 'move-1').subscribe();

    expect(httpClient.delete).toHaveBeenCalledWith(
      `${environment.apiBaseUrl}me/passport/visits/visit-1/occurrences/ride-1?expectedVersion=7`
    );
    const reorderOptions = httpClient.post.mock.calls[0][2] as { headers: HttpHeaders };
    expect(reorderOptions.headers.get('Idempotency-Key')).toBe('move-1');
  });
});

function createOccurrence(): PassportRideOccurrence {
  return {
    id: 'ride-1',
    visitId: 'visit-1',
    parkId: 'park-1',
    parkItemId: 'item-1',
    sortPosition: 1024,
    moment: { localTime: null, isApproximate: false },
    status: 'Completed',
    source: 'Manual',
    historicalConsistency: 'Verified',
    privateNote: null,
    countsAsRide: true,
    version: 1,
    createdAtUtc: '2026-09-03T00:00:00Z',
    updatedAtUtc: '2026-09-03T00:00:00Z'
  };
}
