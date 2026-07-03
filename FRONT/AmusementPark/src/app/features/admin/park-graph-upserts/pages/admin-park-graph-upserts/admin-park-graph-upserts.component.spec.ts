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
  canApply: boolean;
  canPreview: boolean;
  contentChangeCount: number;
  hasJsonDraft: boolean;
  jsonText: string;
  lastAppliedResult: ParkGraphUpsertResult | null;
  mergeEntityType: string;
  mergeSectionChoices: Record<string, string>;
  mergeSourceId: string;
  mergeTargetId: string;
  previewResult: ParkGraphUpsertResult | null;
  searchTerm: string;
  selectedPark: Park | null;
  uiError: string | null;
  addMergeDraft(): void;
  apply(): void;
  exportSelectedParkJson(): void;
  loadExpertJsonFile(event: Event): void;
  preview(): void;
  removePreviewBlock(change: ParkGraphUpsertResult['changes'][number]): void;
  selectMergeEntityType(entityType: string): void;
  setMergeSectionChoice(section: string, choice: string): void;
  updateJsonText(value: string): void;
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

  it('keeps preview disabled until a JSON draft exists', () => {
    createComponent();

    const previewButton: HTMLButtonElement = fixture.nativeElement.querySelector('.editor-actions button') as HTMLButtonElement;
    expect(harness.canPreview).toBeFalse();
    expect(previewButton.disabled).toBeTrue();

    harness.updateJsonText('{"merges":[]}');
    fixture.detectChanges();

    expect(harness.hasJsonDraft).toBeTrue();
    expect(harness.canPreview).toBeTrue();
    const enabledPreviewButton: HTMLButtonElement = fixture.nativeElement.querySelector('.editor-actions button') as HTMLButtonElement;
    expect(enabledPreviewButton.disabled).toBeFalse();
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

  it('invalidates preview results when the JSON draft changes', () => {
    createComponent({
      parkId: 'park-1',
      parkName: 'Selected Park'
    });

    harness.jsonText = '{"park":{"name":"Selected Park"}}';
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
      changes: [],
      warnings: [],
      errors: []
    } satisfies ParkGraphUpsertResult);

    expect(harness.previewResult?.canApply).toBeTrue();
    expect(harness.canApply).toBeTrue();

    harness.updateJsonText('{"park":{"name":"Changed Park"}}');

    expect(harness.previewResult).toBeNull();
    expect(harness.lastAppliedResult).toBeNull();
    expect(harness.canApply).toBeFalse();
  });

  it('previews a manufacturer merge without a selected park', () => {
    createComponent();

    harness.jsonText = JSON.stringify({
      merges: [
        {
          entityType: 'AttractionManufacturer',
          sourceId: 'manufacturer-source',
          targetId: 'manufacturer-target',
          sections: {
            identity: 'source',
            contactDetails: 'target'
          }
        }
      ]
    });
    harness.preview();

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}admin/park-graph-upserts/preview`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      targetParkId: null,
      createIfMissing: false,
      replaceCollections: false,
      document: {
        merges: [
          {
            entityType: 'AttractionManufacturer',
            sourceId: 'manufacturer-source',
            targetId: 'manufacturer-target',
            sections: {
              identity: 'source',
              contactDetails: 'target'
            }
          }
        ]
      }
    });

    request.flush({
      operationId: 'operation-merge',
      mode: 'merge',
      isApplied: false,
      canApply: true,
      previewedAtUtc: '2026-06-18T10:00:00Z',
      targetParkId: null,
      targetParkName: null,
      counts: { created: 0, updated: 1, deleted: 1, unchanged: 0, warnings: 0, errors: 0 },
      changes: [],
      warnings: [],
      errors: []
    } satisfies ParkGraphUpsertResult);

    expect(harness.uiError).toBeNull();
    expect(harness.previewResult?.canApply).toBeTrue();
  });

  it('adds a merge block to the JSON draft with section choices', () => {
    createComponent();

    harness.mergeSourceId = 'source-item';
    harness.mergeTargetId = 'target-item';
    harness.selectMergeEntityType('ParkItem');
    harness.setMergeSectionChoice('descriptions', 'source');
    harness.addMergeDraft();

    const document = JSON.parse(harness.jsonText) as {
      merges: Array<{
        entityType: string;
        sourceId: string;
        targetId: string;
        sections: Record<string, string>;
      }>;
    };

    expect(document.merges).toHaveSize(1);
    expect(document.merges[0]).toEqual(jasmine.objectContaining({
      entityType: 'ParkItem',
      sourceId: 'source-item',
      targetId: 'target-item'
    }));
    expect(document.merges[0].sections['descriptions']).toBe('source');
    expect(document.merges[0].sections['identity']).toBe('target');
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

  it('wraps long preview warnings inside the result panel', () => {
    createComponent({
      parkId: 'park-1',
      parkName: 'Selected Park'
    });

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    host.style.width = '360px';
    const sourceUrl = 'https://cdn.example.test/images/this-is-a-very-long-source-url-that-must-not-overflow-mobile-screens/photo.webp';
    const warning = `Remote image skipped: sourceUrl already exists for Park 'park-1' as image 'image-existing-1': '${sourceUrl}'.`;

    harness.jsonText = '{"images":[]}';
    harness.preview();

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}admin/park-graph-upserts/preview`);
    request.flush({
      operationId: 'operation-1',
      mode: 'merge',
      isApplied: false,
      canApply: true,
      previewedAtUtc: '2026-06-18T10:00:00Z',
      targetParkId: 'park-1-with-a-long-identifier-that-should-wrap',
      targetParkName: 'Selected Park',
      counts: { created: 0, updated: 0, deleted: 0, unchanged: 0, warnings: 1, errors: 0 },
      changes: [
        {
          entityType: 'Image',
          entityId: null,
          entityKey: sourceUrl,
          displayName: sourceUrl,
          changeType: 'Skipped',
          matchedBy: 'sourceUrl',
          fields: [
            {
              field: 'sourceUrl',
              oldValue: null,
              newValue: sourceUrl
            }
          ]
        }
      ],
      warnings: [warning],
      errors: []
    } satisfies ParkGraphUpsertResult);
    fixture.detectChanges();

    const warningMessage = fixture.nativeElement.querySelector('.message-list--warning .message-group p') as HTMLElement;
    const targetCode = fixture.nativeElement.querySelector('.result-head .admin-muted code') as HTMLElement;

    expect(warningMessage).not.toBeNull();
    expect(getComputedStyle(warningMessage).overflowWrap).toBe('anywhere');
    expect(getComputedStyle(targetCode).overflowWrap).toBe('anywhere');
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

  it('removes a newly created preview block without queuing a deletion', () => {
    createComponent({
      parkId: 'park-1',
      parkName: 'Selected Park'
    });

    harness.jsonText = JSON.stringify({
      items: [
        {
          key: 'new-ride',
          name: 'Draft Ride'
        }
      ]
    });
    const change: ParkGraphUpsertResult['changes'][number] = {
      entityType: 'ParkItem',
      entityId: 'generated-preview-id',
      entityKey: 'new-ride',
      displayName: 'Draft Ride',
      changeType: 'Created',
      matchedBy: 'name',
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
      counts: { created: 1, updated: 0, deleted: 0, unchanged: 0, warnings: 0, errors: 0 },
      changes: [change],
      warnings: [],
      errors: []
    };

    harness.removePreviewBlock(change);

    const nextDocument: { items: unknown[]; suppr?: unknown } = JSON.parse(harness.jsonText);
    expect(nextDocument.items).toEqual([]);
    expect(nextDocument.suppr).toBeUndefined();
    expect(harness.previewResult?.changes).toEqual([]);
  });
});
