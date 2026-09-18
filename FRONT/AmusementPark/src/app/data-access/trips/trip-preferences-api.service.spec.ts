import { HttpClient } from '@angular/common/http';
import { of } from 'rxjs';

import { environment } from '../../../environments/environment';
import { TripPreferencesApiService } from './trip-preferences-api.service';

describe('TripPreferencesApiService', () => {
  it('loads private preferences without transfer caching', () => {
    const httpClient = { get: vi.fn().mockReturnValue(of({ items: [] })) };
    const service: TripPreferencesApiService = new TripPreferencesApiService(
      httpClient as unknown as HttpClient
    );

    service.getMine('trip/one').subscribe();

    expect(httpClient.get).toHaveBeenCalledWith(
      `${environment.apiBaseUrl}me/trips/trip%2Fone/preferences`,
      { transferCache: false }
    );
  });

  it('saves one attraction through the item-scoped endpoint', () => {
    const httpClient = { put: vi.fn().mockReturnValue(of({ items: [] })) };
    const service: TripPreferencesApiService = new TripPreferencesApiService(
      httpClient as unknown as HttpClient
    );
    const request = {
      expectedPlanVersion: 4,
      expectedPreferenceVersion: null,
      level: 'MustDo' as const,
      reason: 'Sensations' as const
    };

    service.set('trip/one', 'item/two', request).subscribe();

    expect(httpClient.put).toHaveBeenCalledWith(
      `${environment.apiBaseUrl}me/trips/trip%2Fone/preferences/item%2Ftwo`,
      request
    );
  });

  it('saves several attraction choices through the batch endpoint', () => {
    const httpClient = { post: vi.fn().mockReturnValue(of({ items: [] })) };
    const service: TripPreferencesApiService = new TripPreferencesApiService(
      httpClient as unknown as HttpClient
    );
    const request = {
      expectedPlanVersion: 4,
      preferences: [{
        parkItemId: 'item-1',
        expectedPreferenceVersion: 2,
        level: 'NotForMe' as const,
        reason: 'Height' as const
      }]
    };

    service.setBatch('trip/one', request).subscribe();

    expect(httpClient.post).toHaveBeenCalledWith(
      `${environment.apiBaseUrl}me/trips/trip%2Fone/preferences:batch`,
      request
    );
  });
});
