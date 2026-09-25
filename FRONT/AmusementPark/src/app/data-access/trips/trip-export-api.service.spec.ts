import { HttpClient, HttpHeaders } from '@angular/common/http';
import { of } from 'rxjs';

import { environment } from '../../../environments/environment';
import { TripExportApiService } from './trip-export-api.service';

describe('TripExportApiService', () => {
  it('loads the private portable plan without transfer caching', () => {
    const httpClient = { get: vi.fn().mockReturnValue(of({ title: 'Voyage' })) };
    const service: TripExportApiService = new TripExportApiService(
      httpClient as unknown as HttpClient
    );

    service.get('trip/one', 'export-request-1').subscribe();

    expect(httpClient.get).toHaveBeenCalledWith(
      `${environment.apiBaseUrl}me/trips/trip%2Fone/export`,
      {
        headers: expect.any(HttpHeaders),
        transferCache: false
      }
    );
    const options = httpClient.get.mock.calls[0][1] as { headers: HttpHeaders };
    expect(options.headers.get('Idempotency-Key')).toBe('export-request-1');
  });
});
