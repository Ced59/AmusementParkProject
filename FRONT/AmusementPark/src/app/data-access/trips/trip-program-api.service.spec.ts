import { HttpClient, HttpHeaders } from '@angular/common/http';
import { of } from 'rxjs';

import { environment } from '../../../environments/environment';
import { TripProgramApiService } from './trip-program-api.service';

describe('TripProgramApiService', () => {
  it('loads the private program without SSR transfer caching', () => {
    const httpClient = { get: vi.fn().mockReturnValue(of({ candidates: [], days: [] })) };
    const service: TripProgramApiService = new TripProgramApiService(httpClient as unknown as HttpClient);

    service.get('trip/one').subscribe();

    expect(httpClient.get).toHaveBeenCalledWith(
      `${environment.apiBaseUrl}me/trips/trip%2Fone/program`,
      { transferCache: false }
    );
  });

  it('adds a park with the stable idempotency key and expected plan version', () => {
    const httpClient = { post: vi.fn().mockReturnValue(of({})) };
    const service: TripProgramApiService = new TripProgramApiService(httpClient as unknown as HttpClient);
    const request = {
      expectedPlanVersion: 3,
      parkId: 'park-1',
      candidateDates: ['2026-10-03'],
      source: 'Wishlist' as const,
      collectiveNote: 'Priorité haute'
    };

    service.addPark('trip/one', request, 'operation-2').subscribe();

    const call: unknown[] = httpClient.post.mock.calls[0];
    expect(call[0]).toBe(`${environment.apiBaseUrl}me/trips/trip%2Fone/parks`);
    expect(call[1]).toBe(request);
    expect((call[2] as { headers: HttpHeaders }).headers.get('Idempotency-Key')).toBe('operation-2');
  });

  it('moves a park through its candidate-scoped endpoint', () => {
    const httpClient = { post: vi.fn().mockReturnValue(of({})) };
    const service: TripProgramApiService = new TripProgramApiService(httpClient as unknown as HttpClient);
    const request = { expectedPlanVersion: 5, anchorCandidateId: 'candidate-1', placement: 'After' as const };

    service.movePark('trip/one', 'candidate/two', request).subscribe();

    expect(httpClient.post).toHaveBeenCalledWith(
      `${environment.apiBaseUrl}me/trips/trip%2Fone/parks/candidate%2Ftwo/move`,
      request
    );
  });

  it('saves a day with its local date in the endpoint', () => {
    const httpClient = { put: vi.fn().mockReturnValue(of({})) };
    const service: TripProgramApiService = new TripProgramApiService(httpClient as unknown as HttpClient);
    const request = {
      expectedPlanVersion: 5,
      expectedDayVersion: null,
      parkCandidateId: 'candidate-1',
      desiredArrivalTime: '09:00',
      groupNote: null,
      blocks: []
    };

    service.putDay('trip/one', '2026-10-03', request).subscribe();

    expect(httpClient.put).toHaveBeenCalledWith(
      `${environment.apiBaseUrl}me/trips/trip%2Fone/days/2026-10-03`,
      request
    );
  });
});
