import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { ParkGraphUpsertRequest, ParkGraphUpsertResult } from '@app/models/admin/park-graph-upsert.models';
import { AdminContextualBlockInstance } from '../models/admin-contextual-block.model';
import {
  ADMIN_CONTEXTUAL_BLOCK_PARK_GRAPH_UPSERT_DATA_PORT,
  AdminContextualBlockParkGraphUpsertDataPort
} from './admin-contextual-block-park-graph-upsert-data.ports';
import { AdminContextualBlockParkGraphUpsertFacade } from './admin-contextual-block-park-graph-upsert.facade';

describe('AdminContextualBlockParkGraphUpsertFacade', () => {
  let documentRef: Document;
  let facade: AdminContextualBlockParkGraphUpsertFacade;
  let parkGraphUpsertsApi: jasmine.SpyObj<AdminContextualBlockParkGraphUpsertDataPort>;

  beforeEach(() => {
    parkGraphUpsertsApi = jasmine.createSpyObj<AdminContextualBlockParkGraphUpsertDataPort>('AdminContextualBlockParkGraphUpsertDataPort', ['apply']);
    parkGraphUpsertsApi.apply.and.returnValue(of(createResult()));

    TestBed.configureTestingModule({
      providers: [
        AdminContextualBlockParkGraphUpsertFacade,
        { provide: ADMIN_CONTEXTUAL_BLOCK_PARK_GRAPH_UPSERT_DATA_PORT, useValue: parkGraphUpsertsApi }
      ]
    });

    documentRef = TestBed.inject(DOCUMENT);
    facade = TestBed.inject(AdminContextualBlockParkGraphUpsertFacade);
  });

  it('only enables copy and download actions for blocks with a JSON upsert draft', () => {
    expect(facade.canUseDraft(createBlock('{ "documentType": "AmusementParkParkGraphUpsert" }'))).toBeTrue();
    expect(facade.canUseDraft(createBlock(''))).toBeFalse();
    expect(facade.canUseDraft({ ...createBlock('{}'), capabilities: ['fullAdminEdit'] })).toBeFalse();
  });

  it('copies the draft through the document clipboard fallback', async () => {
    const navigatorRef: Navigator | undefined = documentRef.defaultView?.navigator;
    const hadOwnClipboard: boolean = navigatorRef
      ? Object.prototype.hasOwnProperty.call(navigatorRef, 'clipboard')
      : false;
    const clipboardDescriptor: PropertyDescriptor | undefined = navigatorRef
      ? Object.getOwnPropertyDescriptor(navigatorRef, 'clipboard')
      : undefined;
    if (navigatorRef) {
      Object.defineProperty(navigatorRef, 'clipboard', { configurable: true, value: undefined });
    }

    spyOn(documentRef, 'execCommand').and.returnValue(true);

    await facade.copyDraft(createBlock('{ "references": { "manufacturers": [] } }'));

    expect(documentRef.execCommand).toHaveBeenCalledWith('copy');
    expect(facade.successKey()).toBe('admin.contextualBlocks.drawer.parkGraphUpsertCopied');
    expect(facade.errorKey()).toBeNull();

    if (navigatorRef && clipboardDescriptor) {
      Object.defineProperty(navigatorRef, 'clipboard', clipboardDescriptor);
    } else if (navigatorRef && !hadOwnClipboard) {
      Reflect.deleteProperty(navigatorRef, 'clipboard');
    }
  });

  it('imports a selected JSON file without a target park', async () => {
    const file: File = new File(['{ "references": { "manufacturers": [{ "name": "Mack Rides" }] } }'], 'manufacturer.json', { type: 'application/json' });

    await facade.importDraftFile(createBlock('{ "documentType": "AmusementParkParkGraphUpsert" }'), file);

    const request: ParkGraphUpsertRequest = parkGraphUpsertsApi.apply.calls.mostRecent().args[0];
    expect(request.targetParkId).toBeNull();
    expect(request.createIfMissing).toBeFalse();
    expect(request.replaceCollections).toBeFalse();
    expect(request.document).toEqual({ references: { manufacturers: [{ name: 'Mack Rides' }] } });
    expect(facade.successKey()).toBe('admin.contextualBlocks.drawer.parkGraphUpsertImported');
    expect(facade.errorKey()).toBeNull();
  });

  it('rejects non JSON import files before calling the API', async () => {
    const file: File = new File(['{}'], 'manufacturer.txt', { type: 'text/plain' });

    await facade.importDraftFile(createBlock('{ "documentType": "AmusementParkParkGraphUpsert" }'), file);

    expect(parkGraphUpsertsApi.apply).not.toHaveBeenCalled();
    expect(facade.errorKey()).toBe('admin.contextualBlocks.drawer.parkGraphUpsertImportInvalidFile');
  });
});

function createResult(): ParkGraphUpsertResult {
  return {
    operationId: 'operation-1',
    mode: 'merge',
    isApplied: true,
    canApply: true,
    previewedAtUtc: '2026-06-22T00:00:00Z',
    appliedAtUtc: '2026-06-22T00:00:00Z',
    targetParkId: null,
    targetParkName: null,
    counts: {
      created: 1,
      updated: 0,
      deleted: 0,
      unchanged: 0,
      warnings: 0,
      errors: 0
    },
    changes: [],
    warnings: [],
    errors: []
  };
}

function createBlock(draft: string): AdminContextualBlockInstance {
  return {
    id: 'reference.manufacturer:manufacturer-1',
    type: 'reference.manufacturer',
    entityType: 'AttractionManufacturer',
    entityId: 'manufacturer-1',
    contextLabel: 'Mack Rides',
    ids: { manufacturerId: 'manufacturer-1' },
    labelKey: 'admin.contextualBlocks.blocks.manufacturerReference.label',
    descriptionKey: 'admin.contextualBlocks.blocks.manufacturerReference.description',
    iconClass: 'pi pi-wrench',
    capabilities: ['fullAdminEdit', 'parkGraphUpsertDraft'],
    jsonScope: ['references.manufacturers[*].name'],
    localizedLanguageCodes: ['fr', 'en'],
    locationFallbackCenter: null,
    adminRoute: ['/', 'fr', 'admin', 'manufacturers', 'edit', 'manufacturer-1'],
    parkGraphUpsertDraftJson: draft,
    parkGraphUpsertFileName: 'manufacturer-1-manufacturer-upsert.json'
  };
}
