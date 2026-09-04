import { HttpClient } from '@angular/common/http';
import { of } from 'rxjs';

import { environment } from '../../../environments/environment';
import { PassportExportsApiService } from './passport-exports-api.service';

describe('PassportExportsApiService', () => {
  it('requests a private JSON export through the passport endpoint', () => {
    const httpClient = { post: vi.fn().mockReturnValue(of({})) };
    const service = new PassportExportsApiService(httpClient as unknown as HttpClient);

    service.requestExport({ format: 'Json' }).subscribe();

    expect(httpClient.post).toHaveBeenCalledWith(
      `${environment.apiBaseUrl}me/passport/exports`,
      { format: 'Json' }
    );
  });

  it('encodes identifiers and disables transfer caching for private status requests', () => {
    const httpClient = { get: vi.fn().mockReturnValue(of({})) };
    const service = new PassportExportsApiService(httpClient as unknown as HttpClient);

    service.getExport('export/one').subscribe();

    expect(httpClient.get).toHaveBeenCalledWith(
      `${environment.apiBaseUrl}me/passport/exports/export%2Fone`,
      { transferCache: false }
    );
  });

  it('downloads the owned artifact as a non-cacheable blob', () => {
    const httpClient = { get: vi.fn().mockReturnValue(of(new Blob())) };
    const service = new PassportExportsApiService(httpClient as unknown as HttpClient);

    service.downloadExport('export one').subscribe();

    expect(httpClient.get).toHaveBeenCalledWith(
      `${environment.apiBaseUrl}me/passport/exports/export%20one?download=true`,
      { responseType: 'blob', transferCache: false }
    );
  });
});
