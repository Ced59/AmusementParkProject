import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';

import { AdminContextualBlockInstance } from '../models/admin-contextual-block.model';
import { AdminContextualBlockParkGraphUpsertFacade } from './admin-contextual-block-park-graph-upsert.facade';

describe('AdminContextualBlockParkGraphUpsertFacade', () => {
  let documentRef: Document;
  let facade: AdminContextualBlockParkGraphUpsertFacade;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [AdminContextualBlockParkGraphUpsertFacade]
    });

    documentRef = TestBed.inject(DOCUMENT);
    facade = TestBed.inject(AdminContextualBlockParkGraphUpsertFacade);
  });

  it('only enables copy and download actions for blocks with a graph upsert draft', () => {
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
});

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
    parkGraphUpsertFileName: 'manufacturer-1-park-graph-upsert.json',
    parkGraphUpsertImportRoute: ['/', 'fr', 'admin', 'park-graph-upserts']
  };
}
