import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpTestingController } from '@angular/common/http/testing';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { AdminBulkParkGraphUpsertsComponent } from './admin-bulk-park-graph-upserts.component';
import { BulkParkGraphUpsertResult, ParkGraphExportSection } from '@app/models/admin/park-graph-upsert.models';
import { Park } from '@app/models/parks/park';
import { environment } from '../../../../../../environments/environment';

interface AdminBulkParkGraphUpsertsComponentHarness {
  jsonText: string;
  localAdminReviewStatusFilter: string | null;
  localClosedFilter: string;
  localCountryCodeFilter: string;
  localVisibilityFilter: boolean | null;
  operationErrorDetail: string | null;
  previewResult: BulkParkGraphUpsertResult | null;
  replaceCollections: boolean;
  searchQuery: string;
  selectedParkIds: string[];
  selectedSections: ParkGraphExportSection[];
  selectionMode: string;
  totalRecords: number;
  uiError: string | null;
  exportBulkJson(): void;
  preview(): void;
  updateJsonText(value: string): void;
}

describe('AdminBulkParkGraphUpsertsComponent', () => {
  let component: AdminBulkParkGraphUpsertsComponent;
  let fixture: ComponentFixture<AdminBulkParkGraphUpsertsComponent>;
  let harness: AdminBulkParkGraphUpsertsComponentHarness;
  let httpTestingController: HttpTestingController;
  let originalCreateObjectUrl: typeof URL.createObjectURL;
  let originalRevokeObjectUrl: typeof URL.revokeObjectURL;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, AdminBulkParkGraphUpsertsComponent],
      providers: [
        ...provideCommonTestDependencies()
      ],
    }).compileComponents();

    httpTestingController = TestBed.inject(HttpTestingController);
    originalCreateObjectUrl = URL.createObjectURL;
    originalRevokeObjectUrl = URL.revokeObjectURL;
  });

  afterEach(() => {
    httpTestingController.verify();
    Object.defineProperty(URL, 'createObjectURL', {
      configurable: true,
      value: originalCreateObjectUrl
    });
    Object.defineProperty(URL, 'revokeObjectURL', {
      configurable: true,
      value: originalRevokeObjectUrl
    });
  });

  function createComponent(): void {
    fixture = TestBed.createComponent(AdminBulkParkGraphUpsertsComponent);
    component = fixture.componentInstance;
    harness = component as unknown as AdminBulkParkGraphUpsertsComponentHarness;
    fixture.detectChanges();
  }

  function flushInitialParks(parks: Park[] = []): void {
    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}parks?page=1&size=10`);
    expect(request.request.method).toBe('GET');
    request.flush({
      data: parks,
      pagination: {
        totalItems: parks.length,
        totalPages: 1,
        currentPage: 1,
        itemsPerPage: 10
      }
    });
  }

  function stubBlobDownload(): void {
    Object.defineProperty(URL, 'createObjectURL', {
      configurable: true,
      value: jasmine.createSpy('createObjectURL').and.returnValue('blob:bulk-export')
    });
    Object.defineProperty(URL, 'revokeObjectURL', {
      configurable: true,
      value: jasmine.createSpy('revokeObjectURL')
    });
  }

  it('loads the first admin park page on init', () => {
    createComponent();

    flushInitialParks([
      {
        id: 'park-1',
        name: 'Export Park',
        countryCode: 'FR',
        latitude: 48.5,
        longitude: 2.3
      }
    ]);
    fixture.detectChanges();

    expect(harness.totalRecords).toBe(1);
    expect(fixture.nativeElement.textContent).toContain('Export Park');
  });

  it('exports filtered parks with the selected sections and nullable empty section list support', () => {
    createComponent();
    flushInitialParks();

    stubBlobDownload();
    harness.searchQuery = 'closed';
    harness.localVisibilityFilter = false;
    harness.localAdminReviewStatusFilter = 'Validated';
    harness.localCountryCodeFilter = 'fr';
    harness.localClosedFilter = 'closedOnly';
    harness.selectedSections = [];
    harness.exportBulkJson();

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}admin/park-graph-upserts/bulk/export`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(jasmine.objectContaining({
      selectionMode: 'filtered',
      parkIds: [],
      searchTerm: 'closed',
      isVisible: false,
      adminReviewStatus: 'Validated',
      countryCode: 'FR',
      closedFilter: 'closedOnly',
      sections: []
    }));

    request.flush(new Blob(['{}'], { type: 'application/json' }), {
      headers: {
        'content-disposition': 'attachment; filename="bulk.json"'
      }
    });
  });

  it('exports explicitly selected parks when selection mode is explicit', () => {
    createComponent();
    flushInitialParks();

    stubBlobDownload();
    harness.selectionMode = 'explicit';
    harness.selectedParkIds = ['park-1', 'park-2'];
    harness.selectedSections = ['ParkAudience', 'ParkLocation'];
    harness.exportBulkJson();

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}admin/park-graph-upserts/bulk/export`);
    expect(request.request.body).toEqual(jasmine.objectContaining({
      selectionMode: 'explicit',
      parkIds: ['park-1', 'park-2'],
      sections: ['ParkAudience', 'ParkLocation']
    }));
    request.flush(new Blob(['{}'], { type: 'application/json' }));
  });

  it('previews bulk JSON without allowing park creation', () => {
    createComponent();
    flushInitialParks();

    harness.replaceCollections = true;
    harness.updateJsonText('{"documentType":"AmusementParkBulkParkGraphUpsert","parks":[{"identity":{"parkId":"park-1"}}]}');
    harness.preview();

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}admin/park-graph-upserts/bulk/preview`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      createIfMissing: false,
      replaceCollections: true,
      document: {
        documentType: 'AmusementParkBulkParkGraphUpsert',
        parks: [
          {
            identity: {
              parkId: 'park-1'
            }
          }
        ]
      }
    });

    request.flush({
      operationId: 'bulk-preview',
      isApplied: false,
      canApply: true,
      previewedAtUtc: '2026-07-03T10:00:00Z',
      counts: { created: 0, updated: 0, deleted: 0, unchanged: 1, warnings: 0, errors: 0 },
      parks: [],
      warnings: [],
      errors: []
    } satisfies BulkParkGraphUpsertResult);

    expect(harness.previewResult?.canApply).toBeTrue();
  });

  it('rejects invalid JSON before previewing', () => {
    createComponent();
    flushInitialParks();

    harness.updateJsonText('{');
    harness.preview();

    expect(harness.uiError).toBe('admin.bulkParkGraphUpserts.errors.invalidJson');
    expect(harness.operationErrorDetail).toBeNull();
  });
});
