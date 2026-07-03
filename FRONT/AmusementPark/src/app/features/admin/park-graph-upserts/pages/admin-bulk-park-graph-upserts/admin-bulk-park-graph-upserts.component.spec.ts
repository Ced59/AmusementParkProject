import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpTestingController } from '@angular/common/http/testing';

import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { AdminBulkParkGraphUpsertsComponent } from './admin-bulk-park-graph-upserts.component';
import { BulkParkGraphUpsertResult, ParkGraphExportSection } from '@app/models/admin/park-graph-upsert.models';
import { Park } from '@app/models/parks/park';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { environment } from '../../../../../../environments/environment';

interface AdminBulkParkGraphUpsertsComponentHarness {
  jsonText: string;
  localAdminReviewStatusFilter: string | null;
  localClosedFilter: string;
  localCountryCodeFilter: string;
  localVisibilityFilter: boolean | null;
  operationErrorDetail: string | null;
  previewResult: BulkParkGraphUpsertResult | null;
  isExporting: boolean;
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

interface AdminBulkParkGraphUpsertsComponentPrivateHarness {
  downloadBlob(blob: Blob, fileName: string): void;
}

describe('AdminBulkParkGraphUpsertsComponent', () => {
  let component: AdminBulkParkGraphUpsertsComponent;
  let fixture: ComponentFixture<AdminBulkParkGraphUpsertsComponent>;
  let harness: AdminBulkParkGraphUpsertsComponentHarness;
  let httpTestingController: HttpTestingController;

  beforeEach(async () => {
    TestBed.overrideComponent(AdminBulkParkGraphUpsertsComponent, {
      set: {
        imports: [],
        template: `
          <button class="export-action" type="button" [disabled]="!canExport" (click)="exportBulkJson()">
            {{ isExporting ? 'exporting' : 'export' }}
          </button>
          @if (isExporting) {
            <p class="export-status" role="status">exporting</p>
          }
          @for (park of parks; track park.id) {
            <span class="park-name">{{ park.name }}</span>
          }
        `
      }
    });
    await TestBed.configureTestingModule({
      imports: [AdminBulkParkGraphUpsertsComponent],
      providers: [
        ...provideCommonTestDependencies(),
        { provide: ToastMessageService, useValue: jasmine.createSpyObj<ToastMessageService>('ToastMessageService', ['add']) }
      ],
    }).compileComponents();

    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
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
    spyOn(component as unknown as AdminBulkParkGraphUpsertsComponentPrivateHarness, 'downloadBlob').and.stub();
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

  it('shows export progress while the bulk JSON download is pending', () => {
    createComponent();
    flushInitialParks();

    stubBlobDownload();
    harness.exportBulkJson();

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}admin/park-graph-upserts/bulk/export`);
    fixture.detectChanges();

    const exportButton: HTMLButtonElement | null = fixture.nativeElement.querySelector('.export-action');
    expect(harness.isExporting).toBeTrue();
    expect(exportButton?.disabled).toBeTrue();
    expect(fixture.nativeElement.querySelector('.export-status')).not.toBeNull();

    request.flush(new Blob(['{}'], { type: 'application/json' }));
    fixture.detectChanges();

    expect(harness.isExporting).toBeFalse();
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

  it('sends preview bulk JSON without allowing park creation', () => {
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

    request.flush('preview failed', { status: 500, statusText: 'Server Error' });
    expect(harness.uiError).toBe('admin.bulkParkGraphUpserts.errors.previewFailed');
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
