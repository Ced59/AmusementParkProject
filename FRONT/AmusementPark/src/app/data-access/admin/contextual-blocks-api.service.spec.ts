import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { environment } from '../../../environments/environment';
import { ContextualBlocksApiService } from './contextual-blocks-api.service';

describe('ContextualBlocksApiService', () => {
  let service: ContextualBlocksApiService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: COMMON_TEST_IMPORTS,
      providers: provideCommonTestDependencies(),
    });

    service = TestBed.inject(ContextualBlocksApiService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('downloads bounded contextual block exports as blobs', () => {
    const responseBlob: Blob = new Blob(['{}'], { type: 'application/json' });

    service.downloadBlockExport('park.description', 'park 1').subscribe(response => {
      expect(response.body).toBe(responseBlob);
      expect(response.headers.get('content-disposition')).toContain('block.json');
    });

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}admin/contextual-blocks/park.description/park%201/export`);
    expect(request.request.method).toBe('GET');
    expect(request.request.responseType).toBe('blob');

    request.flush(responseBlob, {
      headers: {
        'content-disposition': 'attachment; filename="block.json"'
      }
    });
  });
});
