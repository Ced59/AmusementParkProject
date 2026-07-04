import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpTestingController } from '@angular/common/http/testing';

import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { AdminBulkParkGraphUpsertsComponent } from './admin-bulk-park-graph-upserts.component';
import { BulkParkGraphUpsertResult, ParkGraphBulkExportJob, ParkGraphExportSection } from '@app/models/admin/park-graph-upsert.models';
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
  exportJob: ParkGraphBulkExportJob | null;
  exportBulkJson(): void;
  preview(): void;
  updateJsonText(value: string): void;
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
          @if (isExporting || exportJob) {
            <p class="export-progress" role="status">{{ exportProgressPercentage }}%</p>
          }
          @if (exportJob?.status === 'Completed' && exportJob?.downloadUrl) {
            <a class="download-link" [href]="exportJob?.downloadUrl" [download]="exportJob?.fileName || 'bulk-park-graph-export.json'" target="_blank">download</a>
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
    fixture?.destroy();
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

  function createRunningJob(jobId: string = 'job-1'): ParkGraphBulkExportJob {
    return {
      jobId,
      status: 'Running',
      progressPercentage: 25,
      message: 'Export JSON bulk en cours.',
      exportedParkCount: 2,
      processedParkCount: 0,
      createdAtUtc: '2026-07-04T00:00:00Z',
      expiresAtUtc: '2026-07-04T00:30:00Z'
    };
  }

  function createCompletedJob(jobId: string = 'job-1'): ParkGraphBulkExportJob {
    return {
      ...createRunningJob(jobId),
      status: 'Completed',
      progressPercentage: 100,
      processedParkCount: 2,
      fileName: 'bulk.json',
      downloadUrl: 'https://api.test/admin/park-graph-upserts/bulk/export-jobs/job-1/download?token=abc',
      completedAtUtc: '2026-07-04T00:01:00Z'
    };
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

    harness.searchQuery = 'closed';
    harness.localVisibilityFilter = false;
    harness.localAdminReviewStatusFilter = 'Validated';
    harness.localCountryCodeFilter = 'fr';
    harness.localClosedFilter = 'closedOnly';
    harness.selectedSections = [];
    harness.exportBulkJson();

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}admin/park-graph-upserts/bulk/export-jobs`);
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

    request.flush(createCompletedJob());
  });

  it('shows export progress while the bulk JSON job is pending', () => {
    createComponent();
    flushInitialParks();

    harness.exportBulkJson();

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}admin/park-graph-upserts/bulk/export-jobs`);
    request.flush(createRunningJob());
    const statusRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}admin/park-graph-upserts/bulk/export-jobs/job-1`);
    statusRequest.flush(createRunningJob());
    fixture.detectChanges();

    const exportButton: HTMLButtonElement | null = fixture.nativeElement.querySelector('.export-action');
    expect(harness.isExporting).toBeTrue();
    expect(exportButton?.disabled).toBeTrue();
    expect(fixture.nativeElement.querySelector('.export-progress')).not.toBeNull();
    expect(harness.exportJob?.progressPercentage).toBe(25);
  });

  it('does not start another status request while a slow export poll is pending', () => {
    jasmine.clock().install();
    try {
      createComponent();
      flushInitialParks();

      harness.exportBulkJson();

      const request = httpTestingController.expectOne(`${environment.apiBaseUrl}admin/park-graph-upserts/bulk/export-jobs`);
      request.flush(createRunningJob('job-1'));
      const pendingStatusRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}admin/park-graph-upserts/bulk/export-jobs/job-1`);

      jasmine.clock().tick(1000);
      httpTestingController.expectNone(`${environment.apiBaseUrl}admin/park-graph-upserts/bulk/export-jobs/job-1`);

      pendingStatusRequest.flush(createCompletedJob('job-1'));
      jasmine.clock().tick(0);
      httpTestingController.expectNone(`${environment.apiBaseUrl}admin/park-graph-upserts/bulk/export-jobs/job-1`);
    } finally {
      jasmine.clock().uninstall();
    }
  });

  it('polls and exposes the generated download link when the export job completes', () => {
    createComponent();
    flushInitialParks();

    harness.selectionMode = 'explicit';
    harness.selectedParkIds = ['park-1', 'park-2'];
    harness.selectedSections = ['ParkAudience', 'ParkLocation'];
    harness.exportBulkJson();

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}admin/park-graph-upserts/bulk/export-jobs`);
    expect(request.request.body).toEqual(jasmine.objectContaining({
      selectionMode: 'explicit',
      parkIds: ['park-1', 'park-2'],
      sections: ['ParkAudience', 'ParkLocation']
    }));
    request.flush(createRunningJob('job-1'));

    const statusRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}admin/park-graph-upserts/bulk/export-jobs/job-1`);
    expect(statusRequest.request.method).toBe('GET');
    statusRequest.flush(createCompletedJob('job-1'));

    expect(harness.isExporting).toBeFalse();
    expect(harness.exportJob?.status).toBe('Completed');
    fixture.detectChanges();

    const downloadLink: HTMLAnchorElement | null = fixture.nativeElement.querySelector('.download-link');
    expect(downloadLink?.href).toBe('https://api.test/admin/park-graph-upserts/bulk/export-jobs/job-1/download?token=abc');
    expect(downloadLink?.download).toBe('bulk.json');
    expect(downloadLink?.target).toBe('_blank');
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
