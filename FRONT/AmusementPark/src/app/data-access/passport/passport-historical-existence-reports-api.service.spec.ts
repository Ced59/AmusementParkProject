import { HttpClient } from '@angular/common/http';
import { of } from 'rxjs';

import { SubmitPassportHistoricalExistenceReportRequest } from '@app/models/passport/passport-historical-existence-report.models';
import { environment } from '../../../environments/environment';
import { PassportHistoricalExistenceReportsApiService } from './passport-historical-existence-reports-api.service';

describe('PassportHistoricalExistenceReportsApiService', () => {
  it('loads only the private visit-scoped reports without transfer caching', () => {
    const httpClient = { get: vi.fn().mockReturnValue(of([])) };
    const service = new PassportHistoricalExistenceReportsApiService(
      httpClient as unknown as HttpClient);

    service.list('visit/one').subscribe();

    expect(httpClient.get).toHaveBeenCalledWith(
      `${environment.apiBaseUrl}me/passport/visits/visit%2Fone/historical-existence-reports`,
      { transferCache: false }
    );
  });

  it('submits evidence through the visit-scoped endpoint', () => {
    const httpClient = { post: vi.fn().mockReturnValue(of({})) };
    const service = new PassportHistoricalExistenceReportsApiService(
      httpClient as unknown as HttpClient);
    const request: SubmitPassportHistoricalExistenceReportRequest = {
      claimedName: 'Ancien Cyclone',
      sourceUrl: 'https://example.org/archive',
      sourceReference: null,
      details: 'Près du lac'
    };

    service.submit('visit/one', request).subscribe();

    expect(httpClient.post).toHaveBeenCalledWith(
      `${environment.apiBaseUrl}me/passport/visits/visit%2Fone/historical-existence-reports`,
      request
    );
  });
});
