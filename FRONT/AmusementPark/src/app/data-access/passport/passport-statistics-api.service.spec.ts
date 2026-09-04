import { HttpClient } from '@angular/common/http';
import { of } from 'rxjs';

import { environment } from '../../../environments/environment';
import { PassportStatisticsApiService } from './passport-statistics-api.service';

describe('PassportStatisticsApiService', () => {
  it('loads every private scope without transfer cache', () => {
    const httpClient: Pick<HttpClient, 'get'> = { get: vi.fn().mockReturnValue(of({})) };
    const service: PassportStatisticsApiService = new PassportStatisticsApiService(httpClient as HttpClient);

    service.getItemStatistics('item/one').subscribe();
    service.getParkStatistics('park/one').subscribe();
    service.getYearStatistics(2026).subscribe();

    expect(httpClient.get).toHaveBeenNthCalledWith(
      1,
      `${environment.apiBaseUrl}me/passport/items/item%2Fone/stats`,
      { transferCache: false }
    );
    expect(httpClient.get).toHaveBeenNthCalledWith(
      2,
      `${environment.apiBaseUrl}me/passport/parks/park%2Fone/stats`,
      { transferCache: false }
    );
    expect(httpClient.get).toHaveBeenNthCalledWith(
      3,
      `${environment.apiBaseUrl}me/passport/years/2026/stats`,
      { transferCache: false }
    );
  });
});
