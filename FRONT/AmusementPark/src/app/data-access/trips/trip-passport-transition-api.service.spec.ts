import { HttpClient } from '@angular/common/http';
import { of } from 'rxjs';

import { environment } from '../../../environments/environment';
import { TripPassportTransitionApiService } from './trip-passport-transition-api.service';

describe('TripPassportTransitionApiService', () => {
  it('loads the private proposal without transfer caching', () => {
    const httpClient = { get: vi.fn().mockReturnValue(of({ days: [] })) };
    const service: TripPassportTransitionApiService = new TripPassportTransitionApiService(
      httpClient as unknown as HttpClient
    );

    service.get('trip/one').subscribe();

    expect(httpClient.get).toHaveBeenCalledWith(
      `${environment.apiBaseUrl}me/trips/trip%2Fone/passport-transition`,
      { transferCache: false }
    );
  });

  it('confirms one day with only the explicitly selected attractions', () => {
    const httpClient = { post: vi.fn().mockReturnValue(of({ visitId: 'visit-1' })) };
    const service: TripPassportTransitionApiService = new TripPassportTransitionApiService(
      httpClient as unknown as HttpClient
    );
    const request = { parkItemIds: ['item-1'] };

    service.confirm('trip/one', '2027-08-20', request).subscribe();

    expect(httpClient.post).toHaveBeenCalledWith(
      `${environment.apiBaseUrl}me/trips/trip%2Fone/passport-transition/days/2027-08-20/confirm`,
      request
    );
  });
});
