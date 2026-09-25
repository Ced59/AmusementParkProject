import { HttpClient, HttpParams } from '@angular/common/http';
import { of } from 'rxjs';

import { environment } from '../../../environments/environment';
import { TripActivityApiService } from './trip-activity-api.service';

describe('TripActivityApiService', () => {
  it('loads a private page without transfer caching and encodes the trip id', () => {
    const httpClient = { get: vi.fn().mockReturnValue(of({ entries: [] })) };
    const service: TripActivityApiService = new TripActivityApiService(
      httpClient as unknown as HttpClient
    );

    service.get('trip/one', 42).subscribe();

    expect(httpClient.get).toHaveBeenCalledWith(
      `${environment.apiBaseUrl}me/trips/trip%2Fone/activity`,
      {
        params: expect.any(HttpParams),
        transferCache: false
      }
    );
    const options = httpClient.get.mock.calls[0][1] as { params: HttpParams };
    expect(options.params.get('beforeSequence')).toBe('42');
  });
});
