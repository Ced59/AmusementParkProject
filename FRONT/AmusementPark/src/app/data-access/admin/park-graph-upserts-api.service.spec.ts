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

  it('downloads completed bulk park graph exports as blobs', () => {
    const responseBlob: Blob = new Blob(['{}'], { type: 'application/json' });
    const downloadUrl: string = 'https://api.test/admin/park-graph-upserts/bulk/export-jobs/job-1/download?token=abc';

    service.downloadBulkParkExport(downloadUrl).subscribe(response => {
      expect(response.body).toBe(responseBlob);
      expect(response.headers.get('content-disposition')).toContain('bulk.json');
    });

    const request = httpTestingController.expectOne(downloadUrl);
    expect(request.request.method).toBe('GET');
    expect(request.request.responseType).toBe('blob');

    request.flush(responseBlob, {
      headers: {
        'content-disposition': 'attachment; filename="bulk.json"'
      }
    });
  });

  it('starts and reads bulk park graph export jobs', () => {
    service.startBulkParkExportJob({
      selectionMode: 'filtered',
      parkIds: [],
      searchTerm: 'closed',
      isVisible: false,
      closedFilter: 'closedOnly',
      sections: ['ParkAudience', 'ParkLocation']
    }).subscribe(response => {
      expect(response.jobId).toBe('job-1');
      expect(response.status).toBe('Running');
    });

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}admin/park-graph-upserts/bulk/export-jobs`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body.selectionMode).toBe('filtered');
    expect(request.request.body.sections).toEqual(['ParkAudience', 'ParkLocation']);
    request.flush({
      jobId: 'job-1',
      status: 'Running',
      progressPercentage: 25,
      createdAtUtc: '2026-07-04T00:00:00Z',
      expiresAtUtc: '2026-07-04T00:30:00Z'
    });

    service.getBulkParkExportJob('job 1').subscribe(response => {
      expect(response.status).toBe('Completed');
      expect(response.downloadUrl).toContain('/download?token=');
    });

    const statusRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}admin/park-graph-upserts/bulk/export-jobs/job%201`);
    expect(statusRequest.request.method).toBe('GET');
    statusRequest.flush({
      jobId: 'job 1',
      status: 'Completed',
      progressPercentage: 100,
      fileName: 'bulk.json',
      downloadUrl: 'https://api.test/admin/park-graph-upserts/bulk/export-jobs/job%201/download?token=abc',
      createdAtUtc: '2026-07-04T00:00:00Z',
      expiresAtUtc: '2026-07-04T00:30:00Z'
    });
  });

  it('previews and applies bulk park graph upserts', () => {
    const document = {
      documentType: 'AmusementParkBulkParkGraphUpsert',
      parks: []
    };
    const result = {
      operationId: 'bulk-1',
      isApplied: false,
      canApply: true,
      previewedAtUtc: '2026-07-03T00:00:00Z',
      counts: { created: 0, updated: 0, deleted: 0, unchanged: 0, warnings: 0, errors: 0 },
      parks: [],
      warnings: [],
      errors: []
    };

    service.previewBulk({ createIfMissing: false, replaceCollections: false, document }).subscribe(response => {
      expect(response.operationId).toBe('bulk-1');
    });

    const previewRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}admin/park-graph-upserts/bulk/preview`);
    expect(previewRequest.request.method).toBe('POST');
    expect(previewRequest.request.body.document).toBe(document);
    previewRequest.flush(result);

    service.applyBulk({ createIfMissing: false, replaceCollections: false, document }).subscribe(response => {
      expect(response.operationId).toBe('bulk-1');
    });

    const applyRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}admin/park-graph-upserts/bulk/apply`);
    expect(applyRequest.request.method).toBe('POST');
    expect(applyRequest.request.body.document).toBe(document);
    applyRequest.flush({ ...result, isApplied: true });
  });
});
