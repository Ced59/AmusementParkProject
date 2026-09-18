import { HttpClient } from '@angular/common/http';
import { of } from 'rxjs';

import { environment } from '../../../environments/environment';
import { TripPreferenceSummaryApiService } from './trip-preference-summary-api.service';

describe('TripPreferenceSummaryApiService', () => {
  it('loads the private group summary without transfer caching', () => {
    const httpClient = { get: vi.fn().mockReturnValue(of({ items: [] })) };
    const service: TripPreferenceSummaryApiService = new TripPreferenceSummaryApiService(
      httpClient as unknown as HttpClient
    );

    service.get('trip/one').subscribe();

    expect(httpClient.get).toHaveBeenCalledWith(
      `${environment.apiBaseUrl}me/trips/trip%2Fone/preference-summary`,
      { transferCache: false }
    );
  });

  it('writes a manual item decision through its scoped endpoint', () => {
    const httpClient = { put: vi.fn().mockReturnValue(of({ items: [] })) };
    const service: TripPreferenceSummaryApiService = new TripPreferenceSummaryApiService(
      httpClient as unknown as HttpClient
    );
    const request = {
      expectedPlanVersion: 7,
      expectedDecisionVersion: null,
      status: 'SplitGroup' as const,
      reason: 'Le groupe se retrouve après le tour.'
    };

    service.setDecision('trip/one', 'item/two', request).subscribe();

    expect(httpClient.put).toHaveBeenCalledWith(
      `${environment.apiBaseUrl}me/trips/trip%2Fone/preference-summary/item%2Ftwo/decision`,
      request
    );
  });
});
