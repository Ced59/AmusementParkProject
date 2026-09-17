import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { of } from 'rxjs';

import { TripPlanWriteRequest } from '@app/models/trips/trip.models';
import { environment } from '../../../environments/environment';
import { TripPlansApiService } from './trip-plans-api.service';

describe('TripPlansApiService', () => {
  it('loads private trips without exposing them through SSR transfer caching', () => {
    const httpClient = { get: vi.fn().mockReturnValue(of([])) };
    const service: TripPlansApiService = new TripPlansApiService(httpClient as unknown as HttpClient);

    service.listMine().subscribe();
    service.getMine('trip/one').subscribe();

    expect(httpClient.get.mock.calls).toEqual([
      [`${environment.apiBaseUrl}me/trips`, { transferCache: false }],
      [`${environment.apiBaseUrl}me/trips/trip%2Fone`, { transferCache: false }]
    ]);
  });

  it('creates a private trip with a stable idempotency key', () => {
    const httpClient = { post: vi.fn().mockReturnValue(of({})) };
    const service: TripPlansApiService = new TripPlansApiService(httpClient as unknown as HttpClient);
    const request: TripPlanWriteRequest = {
      title: 'Road trip',
      dateProposal: { kind: 'None', startDate: null, endDate: null, candidateDates: [] },
      destinationTimeZoneId: null
    };

    service.create(request, 'operation-1').subscribe();

    const call: unknown[] = httpClient.post.mock.calls[0];
    expect(call[0]).toBe(`${environment.apiBaseUrl}me/trips`);
    expect(call[1]).toBe(request);
    expect((call[2] as { headers: HttpHeaders }).headers.get('Idempotency-Key')).toBe('operation-1');
  });

  it('updates dates through the trip-scoped endpoint', () => {
    const httpClient = { post: vi.fn().mockReturnValue(of({})) };
    const service: TripPlansApiService = new TripPlansApiService(httpClient as unknown as HttpClient);
    const request = {
      expectedVersion: 4,
      dateProposal: { kind: 'Fixed' as const, startDate: '2026-10-03', endDate: '2026-10-04', candidateDates: [] },
      destinationTimeZoneId: 'Europe/Paris'
    };

    service.setDates('trip/one', request).subscribe();

    expect(httpClient.post).toHaveBeenCalledWith(
      `${environment.apiBaseUrl}me/trips/trip%2Fone/dates`,
      request
    );
  });

  it('deletes a trip with its optimistic version fence', () => {
    const httpClient = { delete: vi.fn().mockReturnValue(of(undefined)) };
    const service: TripPlansApiService = new TripPlansApiService(httpClient as unknown as HttpClient);

    service.delete('trip/one', 7).subscribe();

    const call: unknown[] = httpClient.delete.mock.calls[0];
    expect(call[0]).toBe(`${environment.apiBaseUrl}me/trips/trip%2Fone`);
    expect(((call[1] as { params: HttpParams }).params).get('expectedVersion')).toBe('7');
  });
});
