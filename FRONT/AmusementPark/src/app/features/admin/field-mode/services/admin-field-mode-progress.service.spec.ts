import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../../../environments/environment';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';

import { AdminFieldModeProgressService } from './admin-field-mode-progress.service';

describe('AdminFieldModeProgressService', () => {
  let service: AdminFieldModeProgressService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    service = TestBed.inject(AdminFieldModeProgressService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
    localStorage.clear();
  });

  it('loads processed ids from the field-mode endpoint and mirrors them locally', () => {
    service.getProcessedItemIds('park 1').subscribe((ids: Set<string>) => {
      expect([...ids]).toEqual(['item-1']);
    });

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}admin/field-mode/parks/park%201/processed-items`);
    expect(request.request.method).toBe('GET');
    request.flush({ parkId: 'park 1', itemIds: ['item-1'] });

    expect(JSON.parse(localStorage.getItem('admin.fieldMode.processedItemIds.park 1') ?? '[]')).toEqual(['item-1']);
  });

  it('updates processed state through the field-mode endpoint and stores a local fallback', () => {
    service.setProcessed('park 1', 'item 1', true).subscribe((isProcessed: boolean) => {
      expect(isProcessed).toBeTrue();
    });

    expect(JSON.parse(localStorage.getItem('admin.fieldMode.processedItemIds.park 1') ?? '[]')).toEqual(['item 1']);

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}admin/field-mode/parks/park%201/items/item%201/processed`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ isProcessed: true });
    request.flush({ parkId: 'park 1', itemId: 'item 1', isProcessed: true });
  });

  it('falls back to local processed ids when the field-mode endpoint fails', () => {
    localStorage.setItem('admin.fieldMode.processedItemIds.park-1', JSON.stringify(['item-1']));

    service.getProcessedItemIds('park-1').subscribe((ids: Set<string>) => {
      expect([...ids]).toEqual(['item-1']);
    });

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}admin/field-mode/parks/park-1/processed-items`);
    request.flush({}, { status: 500, statusText: 'Server error' });
  });
});
