import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpTestingController } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, ParamMap } from '@angular/router';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { AdminParkGraphUpsertsComponent } from './admin-park-graph-upserts.component';
import { ParkGraphUpsertResult } from '@app/models/admin/park-graph-upsert.models';
import { Park } from '@app/models/parks/park';
import { environment } from '../../../../../../environments/environment';

interface ActivatedRouteStub {
  snapshot: {
    queryParamMap: ParamMap;
  };
}

interface AdminParkGraphUpsertsComponentHarness {
  appliedResultMessageKey: string | null;
  appliedResultMessageParams: Record<string, number>;
  contentChangeCount: number;
  hasJsonDraft: boolean;
  jsonText: string;
  lastAppliedResult: ParkGraphUpsertResult | null;
  previewResult: ParkGraphUpsertResult | null;
  searchTerm: string;
  selectedPark: Park | null;
  uiError: string | null;
  apply(): void;
  exportSelectedParkJson(): void;
  loadExpertJsonFile(event: Event): void;
  preview(): void;
  removePreviewBlock(change: ParkGraphUpsertResult['changes'][number]): void;
}

describe('AdminParkGraphUpsertsComponent', () => {
  let component: AdminParkGraphUpsertsComponent;
  let fixture: ComponentFixture<AdminParkGraphUpsertsComponent>;
  let harness: AdminParkGraphUpsertsComponentHarness;
  let httpTestingController: HttpTestingController;
  let routeStub: ActivatedRouteStub;

  beforeEach(async () => {
    routeStub = {
      snapshot: {
        queryParamMap: convertToParamMap({})
      }
    };

    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, AdminParkGraphUpsertsComponent],
      providers: [
        ...provideCommonTestDependencies(),
        { provide: ActivatedRoute, useValue: routeStub }
      ],
    }).compileComponents();

    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  function createComponent(queryParams: Record<string, string> = {}): void {
    routeStub.snapshot.queryParamMap = convertToParamMap(queryParams);
    fixture = TestBed.createComponent(AdminParkGraphUpsertsComponent);
    component = fixture.componentInstance;
    harness = component as unknown as AdminParkGraphUpsertsComponentHarness;
    fixture.detectChanges();
  }

  it('renders the simplified JSON workspace without the removed wizard screens', () => {
    createComponent();

    expect(harness.jsonText).toBe('');
    expect(harness.hasJsonDraft).toBeFalse();
    expect(fixture.nativeElement.querySelector('.graph-wizard')).toBeNull();
    expect(fixture.nativeElement.querySelector('.template-list')).toBeNull();
    expect(fixture.nativeElement.querySelector('.graph-builder')).toBeNull();
    expect(fixture.nativeElement.querySelector('.history-list')).toBeNull();

    const editor: HTMLTextAreaElement = fixture.nativeElement.querySelector('.json-editor') as HTMLTextAreaElement;
    expect(editor).not.toBeNull();
    expect(editor.value).toBe('');
    expect(editor.placeholder).toContain('AmusementParkParkGraphUpsert');
  });

  it('preselects the park received from the export shortcut query params', () => {
    createComponent({
      parkId: 'park-1',
      parkName: 'Export Park',
      parkCountryCode: 'FR',
      parkCity: 'Paris',
      parkLatitude: '48.5',
      parkLongitude: '2.3'
    });

    expect(harness.selectedPark?.id).toBe('park-1');
    expect(harness.selectedPark?.name).toBe('Export Park');
    expect(harness.selectedPark?.countryCode).toBe('FR');
    expect(harness.selectedPark?.city).toBe('Paris');
    expect(harness.selectedPark?.latitude).toBe(48.5);
    expect(harness.selectedPark?.longitude).toBe(2.3);
    expect(harness.searchTerm).toBe('Export Park');
    expect(fixture.nativeElement.textContent).toContain('Export Park');
  });

  it('requires a selected existing park before previewing or exporting', () => {
    createComponent();

    harness.jsonText = '{"park":{"name":"Draft"}}';
    harness.preview();

    expect(harness.uiError).toBe('admin.parkGraphUpserts.errors.noParkSelected');

    harness.exportSelectedParkJson();

    expect(harness.uiError).toBe('admin.parkGraphUpserts.errors.noParkSelected');
  });

  it('loads uploaded JSON files into the raw draft', () => {
    createComponent();

    const uploadedJson: string = '{"park":{"name":"Uploaded Park"},"items":[]}';
    const fakeReader: Partial<FileReader> = {
      get result(): string {
        return uploadedJson;
      },
      onload: null,
      onerror: null,
      readAsText(): void {
        this.onload?.call(this as FileReader, new ProgressEvent('load') as ProgressEvent<FileReader>);
      }
    };
    spyOn(window, 'FileReader').and.returnValue(fakeReader as FileReader);

    const file: File = new File([uploadedJson], 'park.json', { type: 'application/json' });
    const input: HTMLInputElement = { files: [file], value: 'park.json' } as unknown as HTMLInputElement;

    harness.loadExpertJsonFile({ target: input } as unknown as Event);

    expect(harness.jsonText).toBe(uploadedJson);
    expect(harness.uiError).toBeNull();
    expect(input.value).toBe('');
  });

  it('previews the raw JSON against the selected park in merge mode', () => {
    createComponent({
      parkId: 'park-1',
      parkName: 'Selected Park'
    });

    harness.jsonText = '{"park":{"name":"Selected Park"}}';
    harness.preview();

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}admin/park-graph-upserts/preview`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      targetParkId: 'park-1',
      createIfMissing: false,
      replaceCollections: false,
      document: {
        park: {
          name: 'Selected Park'
        }
      }
    });

    request.flush({
      operationId: 'operation-1',
      mode: 'merge',
      isApplied: false,
      canApply: true,
      previewedAtUtc: '2026-06-18T10:00:00Z',
      targetParkId: 'park-1',
      targetParkName: 'Selected Park',
      counts: { created: 0, updated: 1, deleted: 0, unchanged: 0, warnings: 0, errors: 0 },
      changes: [],
      warnings: [],
      errors: []
    } satisfies ParkGraphUpsertResult);

    expect(harness.previewResult?.targetParkId).toBe('park-1');
    expect(harness.previewResult?.canApply).toBeTrue();
  });

  it('shows when previewed content fields will be updated', () => {
    createComponent({
      parkId: 'park-1',
      parkName: 'Selected Park'
    });

    harness.jsonText = '{"park":{"id":"park-1"}}';
    harness.preview();

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}admin/park-graph-upserts/preview`);
    request.flush({
      operationId: 'operation-1',
      mode: 'merge',
      isApplied: false,
      canApply: true,
      previewedAtUtc: '2026-06-18T10:00:00Z',
      targetParkId: 'park-1',
      targetParkName: 'Selected Park',
      counts: { created: 0, updated: 1, deleted: 0, unchanged: 0, warnings: 0, errors: 0 },
      changes: [
        {
          entityType: 'ParkItem',
          entityId: 'item-1',
          entityKey: null,
          displayName: 'Wakala',
          changeType: 'Updated',
          matchedBy: 'id',
          fields: [
            {
              field: 'descriptions.fr',
              oldValue: 'Ancienne description',
              newValue: 'Nouvelle description'
            }
          ]
        }
      ],
      warnings: [],
      errors: []
    } satisfies ParkGraphUpsertResult);
    fixture.detectChanges();

    expect(harness.contentChangeCount).toBe(1);
    expect(fixture.nativeElement.querySelector('.admin-alert--info')).not.toBeNull();
  });

  it('shows a partial apply result with the rejected image detail', () => {
    createComponent({
      parkId: 'park-1',
      parkName: 'Selected Park'
    });

    harness.jsonText = '{"items":[],"images":[]}';
    harness.previewResult = {
      operationId: 'operation-preview',
      mode: 'merge',
      isApplied: false,
      canApply: true,
      previewedAtUtc: '2026-06-18T10:00:00Z',
      targetParkId: 'park-1',
      targetParkName: 'Selected Park',
      counts: { created: 1, updated: 0, deleted: 0, unchanged: 0, warnings: 0, errors: 0 },
      changes: [],
      warnings: [],
      errors: []
    };

    harness.apply();

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}admin/park-graph-upserts/apply`);
    expect(request.request.method).toBe('POST');
    request.flush({
      operationId: 'operation-apply',
      mode: 'merge',
      isApplied: true,
      canApply: false,
      previewedAtUtc: '2026-06-18T10:00:00Z',
      appliedAtUtc: '2026-06-18T10:01:00Z',
      targetParkId: 'park-1',
      targetParkName: 'Selected Park',
      counts: { created: 1, updated: 0, deleted: 0, unchanged: 0, warnings: 0, errors: 1 },
      changes: [
        {
          entityType: 'ParkItem',
          entityId: 'item-1',
          entityKey: 'item:coaster',
          displayName: 'Coaster',
          changeType: 'Created',
          matchedBy: 'name',
          fields: []
        },
        {
          entityType: 'Image',
          entityId: null,
          entityKey: 'https://cdn.example.test/photo.webp',
          displayName: 'https://cdn.example.test/photo.webp',
          changeType: 'Skipped',
          matchedBy: 'sourceUrl',
          fields: [
            {
              field: 'sourceUrl',
              oldValue: null,
              newValue: 'https://cdn.example.test/photo.webp'
            }
          ]
        }
      ],
      warnings: [],
      errors: ["Remote image was not imported: 'https://cdn.example.test/photo.webp'."]
    } satisfies ParkGraphUpsertResult);
    fixture.detectChanges();

    expect(harness.appliedResultMessageKey).toBe('admin.parkGraphUpserts.result.appliedPartial');
    expect(harness.appliedResultMessageParams).toEqual({ applied: 1, failed: 1 });
    expect(fixture.nativeElement.querySelector('.admin-alert--warning')).not.toBeNull();
    expect(fixture.nativeElement.textContent).toContain('https://cdn.example.test/photo.webp');
  });

  it('queues a deletion and removes the source image block from the JSON draft', () => {
    createComponent({
      parkId: 'park-1',
      parkName: 'Selected Park'
    });

    harness.jsonText = JSON.stringify({
      images: [
        {
          imageId: 'image-1',
          description: 'Duplicate image'
        }
      ]
    });
    const change: ParkGraphUpsertResult['changes'][number] = {
      entityType: 'Image',
      entityId: 'image-1',
      entityKey: null,
      displayName: 'Duplicate image',
      changeType: 'Updated',
      matchedBy: 'imageId',
      fields: []
    };
    harness.previewResult = {
      operationId: 'operation-1',
      mode: 'merge',
      isApplied: false,
      canApply: true,
      previewedAtUtc: '2026-06-18T10:00:00Z',
      targetParkId: 'park-1',
      targetParkName: 'Selected Park',
      counts: { created: 0, updated: 1, deleted: 0, unchanged: 0, warnings: 0, errors: 0 },
      changes: [change],
      warnings: [],
      errors: []
    };

    harness.removePreviewBlock(change);

    const nextDocument: { images: unknown[]; suppr: Array<{ entityType: string; id: string }> } = JSON.parse(harness.jsonText);
    expect(nextDocument.images).toEqual([]);
    expect(nextDocument.suppr).toEqual([{ entityType: 'Image', id: 'image-1' }]);
    expect(harness.previewResult?.changes).toEqual([]);
  });
});
