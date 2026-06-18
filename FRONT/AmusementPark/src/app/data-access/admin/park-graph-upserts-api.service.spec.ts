import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { environment } from '../../../environments/environment';
import { ParkGraphUpsertsApiService } from './park-graph-upserts-api.service';

describe('ParkGraphUpsertsApiService', () => {
  let service: ParkGraphUpsertsApiService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: COMMON_TEST_IMPORTS,
      providers: provideCommonTestDependencies(),
    });

    service = TestBed.inject(ParkGraphUpsertsApiService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('downloads park graph exports as blobs', () => {
    const responseBlob: Blob = new Blob(['{}'], { type: 'application/json' });

    service.downloadParkExport('park 1').subscribe(response => {
      expect(response.body).toBe(responseBlob);
      expect(response.headers.get('content-disposition')).toContain('park.json');
    });

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}admin/park-graph-upserts/parks/park%201/export`);
    expect(request.request.method).toBe('GET');
    expect(request.request.responseType).toBe('blob');

    request.flush(responseBlob, {
      headers: {
        'content-disposition': 'attachment; filename="park.json"'
      }
    });
  });
});
