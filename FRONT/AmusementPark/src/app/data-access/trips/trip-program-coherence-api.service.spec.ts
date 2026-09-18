import { HttpClient } from '@angular/common/http';
import { of } from 'rxjs';

import { environment } from '../../../environments/environment';
import { TripProgramCoherenceApiService } from './trip-program-coherence-api.service';

describe('TripProgramCoherenceApiService', () => {
  it('loads private coherence evidence without transfer caching', () => {
    const httpClient = { get: vi.fn().mockReturnValue(of({ issues: [] })) };
    const service: TripProgramCoherenceApiService = new TripProgramCoherenceApiService(
      httpClient as unknown as HttpClient
    );

    service.get('trip/one').subscribe();

    expect(httpClient.get).toHaveBeenCalledWith(
      `${environment.apiBaseUrl}me/trips/trip%2Fone/coherence`,
      { transferCache: false }
    );
  });
});
