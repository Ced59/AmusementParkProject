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

    service.downloadBlockExport('park.description', 'park 1').subscribe((response) => {
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

  it('posts bounded JSON preview requests without mutating public data', () => {
    const document: unknown = { blockType: 'park.description', block: { parkId: 'park-1' } };

    service.previewBlock('park.description', 'park 1', document).subscribe((response) => {
      expect(response.canApply).toBeTrue();
      expect(response.changes.length).toBe(0);
    });

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}admin/contextual-blocks/park.description/park%201/preview`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ document });

    request.flush({
      operationId: 'operation-1',
      blockType: 'park.description',
      isApplied: false,
      canApply: true,
      previewedAtUtc: '2026-06-21T10:00:00Z',
      target: {
        entityType: 'Park',
        entityId: 'park-1',
        displayName: 'Phantasialand'
      },
      counts: {
        created: 0,
        updated: 0,
        deleted: 0,
        unchanged: 0,
        warnings: 0,
        errors: 0
      },
      changes: [],
      warnings: [],
      errors: []
    });
  });

  it('posts bounded JSON apply requests to mutate selected blocks', () => {
    const document: unknown = { blockType: 'park.description', block: { parkId: 'park-1' } };

    service.applyBlock('park.description', 'park 1', document).subscribe((response) => {
      expect(response.isApplied).toBeTrue();
      expect(response.canApply).toBeTrue();
    });

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}admin/contextual-blocks/park.description/park%201/apply`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ document });

    request.flush({
      operationId: 'operation-1',
      blockType: 'park.description',
      isApplied: true,
      canApply: true,
      previewedAtUtc: '2026-06-21T10:00:00Z',
      target: {
        entityType: 'Park',
        entityId: 'park-1',
        displayName: 'Phantasialand'
      },
      counts: {
        created: 0,
        updated: 1,
        deleted: 0,
        unchanged: 7,
        warnings: 0,
        errors: 0
      },
      changes: [],
      warnings: [],
      errors: []
    });
  });
});
