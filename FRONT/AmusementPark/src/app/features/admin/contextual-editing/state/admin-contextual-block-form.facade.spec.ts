import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { ContextualBlocksApiService } from '@data-access/admin/contextual-blocks-api.service';
import { ContextualBlockExportDocument, ContextualParkDescriptionBlock, ContextualParkItemDescriptionBlock } from '@shared/models/admin/contextual-block-export.models';
import { ContextualBlockPreviewResult } from '@shared/models/admin/contextual-block-preview.models';
import { AdminContextualBlockInstance } from '../models/admin-contextual-block.model';
import { ADMIN_CONTEXTUAL_BLOCK_FORM_DATA_PORT } from './admin-contextual-block-form-data.ports';
import { AdminContextualBlockFormFacade } from './admin-contextual-block-form.facade';
import { AdminContextualBlockRefreshEvents } from './admin-contextual-block-refresh-events.service';

describe('AdminContextualBlockFormFacade', () => {
  let facade: AdminContextualBlockFormFacade;
  let contextualBlocksApi: jasmine.SpyObj<ContextualBlocksApiService>;
  let refreshEvents: jasmine.SpyObj<AdminContextualBlockRefreshEvents>;

  beforeEach(() => {
    contextualBlocksApi = jasmine.createSpyObj<ContextualBlocksApiService>('ContextualBlocksApiService', ['getBlockExportDocument', 'applyBlock']);
    refreshEvents = jasmine.createSpyObj<AdminContextualBlockRefreshEvents>('AdminContextualBlockRefreshEvents', ['notifyBlockApplied']);

    TestBed.configureTestingModule({
      providers: [
        AdminContextualBlockFormFacade,
        {
          provide: ADMIN_CONTEXTUAL_BLOCK_FORM_DATA_PORT,
          useValue: contextualBlocksApi
        },
        {
          provide: AdminContextualBlockRefreshEvents,
          useValue: refreshEvents
        }
      ]
    });

    facade = TestBed.inject(AdminContextualBlockFormFacade);
  });

  it('loads localized description fields from the bounded export document', () => {
    contextualBlocksApi.getBlockExportDocument.and.returnValue(of(createDocument()));

    facade.resetForBlock(createBlock());

    expect(contextualBlocksApi.getBlockExportDocument).toHaveBeenCalledOnceWith('park.description', 'park-1');
    expect(facade.localizedFields()).toEqual([
      { languageCode: 'en', value: 'English description' },
      { languageCode: 'fr', value: '' }
    ]);
    expect(facade.errorKey()).toBeNull();
    expect(facade.isLoading()).toBeFalse();
  });

  it('saves localized fields through bounded apply and emits a refresh event', () => {
    const applyResult: ContextualBlockPreviewResult = createApplyResult(true);
    contextualBlocksApi.getBlockExportDocument.and.returnValue(of(createDocument()));
    contextualBlocksApi.applyBlock.and.returnValue(of(applyResult));
    const block: AdminContextualBlockInstance = createBlock();
    facade.resetForBlock(block);

    facade.updateLocalizedValue('fr', 'Description francaise');
    facade.saveForm(block);

    expect(contextualBlocksApi.applyBlock).toHaveBeenCalledOnceWith('park.description', 'park-1', jasmine.objectContaining({
      block: jasmine.objectContaining({
        descriptions: [
          { languageCode: 'en', value: 'English description' },
          { languageCode: 'fr', value: 'Description francaise' }
        ]
      })
    }));
    expect(facade.successKey()).toBe('admin.contextualBlocks.drawer.formSaveSucceeded');
    expect(facade.errorKey()).toBeNull();
    expect(refreshEvents.notifyBlockApplied).toHaveBeenCalledOnceWith(jasmine.objectContaining({
      blockType: 'park.description',
      entityType: 'Park',
      entityId: 'park-1'
    }));
  });

  it('saves park item localized fields through the same bounded form flow', () => {
    const applyResult: ContextualBlockPreviewResult = {
      ...createApplyResult(true),
      blockType: 'parkItem.description',
      target: {
        entityType: 'ParkItem',
        entityId: 'item-1',
        displayName: 'Wakala'
      }
    };
    contextualBlocksApi.getBlockExportDocument.and.returnValue(of(createParkItemDocument()));
    contextualBlocksApi.applyBlock.and.returnValue(of(applyResult));
    const block: AdminContextualBlockInstance = createParkItemBlock();
    facade.resetForBlock(block);

    facade.updateLocalizedValue('fr', 'Description item');
    facade.saveForm(block);

    expect(contextualBlocksApi.getBlockExportDocument).toHaveBeenCalledOnceWith('parkItem.description', 'item-1');
    expect(contextualBlocksApi.applyBlock).toHaveBeenCalledOnceWith('parkItem.description', 'item-1', jasmine.objectContaining({
      block: jasmine.objectContaining({
        parkId: 'park-1',
        parkItemId: 'item-1',
        descriptions: [
          { languageCode: 'en', value: 'English item description' },
          { languageCode: 'fr', value: 'Description item' }
        ]
      })
    }));
    expect(refreshEvents.notifyBlockApplied).toHaveBeenCalledOnceWith(jasmine.objectContaining({
      blockType: 'parkItem.description',
      entityType: 'ParkItem',
      entityId: 'item-1'
    }));
  });

  it('keeps edited fields when save fails', () => {
    contextualBlocksApi.getBlockExportDocument.and.returnValue(of(createDocument()));
    contextualBlocksApi.applyBlock.and.returnValue(throwError(() => new Error('failed')));
    const block: AdminContextualBlockInstance = createBlock();
    facade.resetForBlock(block);

    facade.updateLocalizedValue('fr', 'Draft');
    facade.saveForm(block);

    expect(facade.localizedFields()[1]).toEqual({ languageCode: 'fr', value: 'Draft' });
    expect(facade.errorKey()).toBe('admin.contextualBlocks.drawer.formSaveError');
    expect(refreshEvents.notifyBlockApplied).not.toHaveBeenCalled();
  });

  it('does not load unsupported blocks', () => {
    facade.resetForBlock({
      ...createBlock(),
      capabilities: ['boundedJsonExport']
    });

    expect(contextualBlocksApi.getBlockExportDocument).not.toHaveBeenCalled();
    expect(facade.localizedFields()).toEqual([]);
  });
});

function createDocument(): ContextualBlockExportDocument<ContextualParkDescriptionBlock> {
  return {
    documentType: 'AmusementParkContextualBlockUpsert',
    schemaVersion: '2026-06-21',
    blockType: 'park.description',
    target: {
      entityType: 'Park',
      entityId: 'park-1'
    },
    ids: {
      parkId: 'park-1'
    },
    block: {
      parkId: 'park-1',
      descriptions: [
        { languageCode: 'en', value: 'English description' },
        { languageCode: 'fr', value: null }
      ]
    },
    metadata: {
      source: 'admin-contextual-block-export',
      exportedAtUtc: '2026-06-21T10:00:00Z'
    }
  };
}

function createParkItemDocument(): ContextualBlockExportDocument<ContextualParkItemDescriptionBlock> {
  return {
    documentType: 'AmusementParkContextualBlockUpsert',
    schemaVersion: '2026-06-21',
    blockType: 'parkItem.description',
    target: {
      entityType: 'ParkItem',
      entityId: 'item-1'
    },
    ids: {
      parkId: 'park-1',
      parkItemId: 'item-1',
      zoneId: 'zone-1'
    },
    block: {
      parkId: 'park-1',
      parkItemId: 'item-1',
      zoneId: 'zone-1',
      descriptions: [
        { languageCode: 'en', value: 'English item description' },
        { languageCode: 'fr', value: null }
      ]
    },
    metadata: {
      source: 'admin-contextual-block-export',
      exportedAtUtc: '2026-06-21T10:00:00Z'
    }
  };
}

function createApplyResult(isApplied: boolean): ContextualBlockPreviewResult {
  return {
    operationId: 'operation-1',
    blockType: 'park.description',
    isApplied,
    canApply: true,
    previewedAtUtc: '2026-06-21T10:00:00Z',
    target: {
      entityType: 'Park',
      entityId: 'park-1',
      displayName: 'Phantasialand'
    },
    counts: {
      created: 0,
      updated: isApplied ? 1 : 0,
      deleted: 0,
      unchanged: isApplied ? 1 : 2,
      warnings: 0,
      errors: 0
    },
    changes: [],
    warnings: [],
    errors: []
  };
}

function createBlock(): AdminContextualBlockInstance {
  return {
    id: 'park.description:park-1',
    type: 'park.description',
    entityType: 'Park',
    entityId: 'park-1',
    contextLabel: 'Phantasialand',
    ids: { parkId: 'park-1' },
    labelKey: 'admin.contextualBlocks.blocks.parkDescription.label',
    descriptionKey: 'admin.contextualBlocks.blocks.parkDescription.description',
    iconClass: 'pi pi-align-left',
    capabilities: ['fullAdminEdit', 'boundedJsonExport', 'boundedJsonPreview', 'boundedJsonApply', 'contextualFormEdit'],
    jsonScope: ['park.id', 'park.descriptions[*].value'],
    localizedLanguageCodes: ['fr', 'en'],
    adminRoute: ['/', 'fr', 'admin', 'parks', 'edit', 'park-1']
  };
}

function createParkItemBlock(): AdminContextualBlockInstance {
  return {
    id: 'parkItem.description:item-1',
    type: 'parkItem.description',
    entityType: 'ParkItem',
    entityId: 'item-1',
    contextLabel: 'Wakala',
    ids: { parkId: 'park-1', parkItemId: 'item-1' },
    labelKey: 'admin.contextualBlocks.blocks.parkItemDescription.label',
    descriptionKey: 'admin.contextualBlocks.blocks.parkItemDescription.description',
    iconClass: 'pi pi-align-left',
    capabilities: ['fullAdminEdit', 'boundedJsonExport', 'boundedJsonPreview', 'boundedJsonApply', 'contextualFormEdit'],
    jsonScope: ['parkItem.id', 'parkItem.descriptions[*].value'],
    localizedLanguageCodes: ['fr', 'en'],
    adminRoute: ['/', 'fr', 'admin', 'parks', 'edit', 'park-1', 'items', 'item-1']
  };
}
