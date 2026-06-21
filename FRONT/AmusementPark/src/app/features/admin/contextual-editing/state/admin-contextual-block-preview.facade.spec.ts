import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { ContextualBlockPreviewResult } from '@shared/models/admin/contextual-block-preview.models';
import { ContextualBlocksApiService } from '@data-access/admin/contextual-blocks-api.service';
import { AdminContextualBlockInstance } from '../models/admin-contextual-block.model';
import { ADMIN_CONTEXTUAL_BLOCK_PREVIEW_DATA_PORT } from './admin-contextual-block-preview-data.ports';
import { AdminContextualBlockPreviewFacade } from './admin-contextual-block-preview.facade';

describe('AdminContextualBlockPreviewFacade', () => {
  let facade: AdminContextualBlockPreviewFacade;
  let contextualBlocksApi: jasmine.SpyObj<ContextualBlocksApiService>;

  beforeEach(() => {
    contextualBlocksApi = jasmine.createSpyObj<ContextualBlocksApiService>('ContextualBlocksApiService', ['previewBlock']);

    TestBed.configureTestingModule({
      providers: [
        AdminContextualBlockPreviewFacade,
        {
          provide: ADMIN_CONTEXTUAL_BLOCK_PREVIEW_DATA_PORT,
          useValue: contextualBlocksApi
        }
      ]
    });

    facade = TestBed.inject(AdminContextualBlockPreviewFacade);
  });

  it('sends parsed JSON to the bounded preview endpoint', () => {
    const previewResult: ContextualBlockPreviewResult = createPreviewResult(true);
    contextualBlocksApi.previewBlock.and.returnValue(of(previewResult));
    facade.resetForBlock(createBlock(['boundedJsonPreview']));
    facade.setJsonDraft('{ "block": { "parkId": "park-1" } }');

    facade.previewBlock(createBlock(['boundedJsonPreview']));

    expect(contextualBlocksApi.previewBlock).toHaveBeenCalledOnceWith('park.description', 'park-1', {
      block: {
        parkId: 'park-1'
      }
    });
    expect(facade.previewResult()).toBe(previewResult);
    expect(facade.errorKey()).toBeNull();
    expect(facade.isPreviewing()).toBeFalse();
  });

  it('keeps the current draft when JSON parsing fails', () => {
    facade.resetForBlock(createBlock(['boundedJsonPreview']));
    facade.setJsonDraft('{ invalid json');

    facade.previewBlock(createBlock(['boundedJsonPreview']));

    expect(contextualBlocksApi.previewBlock).not.toHaveBeenCalled();
    expect(facade.jsonDraft()).toBe('{ invalid json');
    expect(facade.errorKey()).toBe('admin.contextualBlocks.drawer.previewJsonInvalid');
  });

  it('does not call the API for unsupported blocks', () => {
    facade.resetForBlock(createBlock(['boundedJsonExportPlanned']));
    facade.setJsonDraft('{ "block": { "parkId": "park-1" } }');

    facade.previewBlock(createBlock(['boundedJsonExportPlanned']));

    expect(contextualBlocksApi.previewBlock).not.toHaveBeenCalled();
    expect(facade.errorKey()).toBe('admin.contextualBlocks.drawer.previewJsonUnavailable');
  });

  it('surfaces safe API errors without wiping the draft', () => {
    contextualBlocksApi.previewBlock.and.returnValue(throwError(() => new Error('failed')));
    facade.resetForBlock(createBlock(['boundedJsonPreview']));
    facade.setJsonDraft('{ "block": { "parkId": "park-1" } }');

    facade.previewBlock(createBlock(['boundedJsonPreview']));

    expect(facade.jsonDraft()).toBe('{ "block": { "parkId": "park-1" } }');
    expect(facade.previewResult()).toBeNull();
    expect(facade.errorKey()).toBe('admin.contextualBlocks.drawer.previewJsonError');
    expect(facade.isPreviewing()).toBeFalse();
  });

  it('resets draft and result when the selected block changes', () => {
    facade.resetForBlock(createBlock(['boundedJsonPreview']));
    facade.setJsonDraft('{ "block": { "parkId": "park-1" } }');

    facade.resetForBlock({
      ...createBlock(['boundedJsonPreview']),
      id: 'park.practical:park-2',
      entityId: 'park-2',
      ids: { parkId: 'park-2' }
    });

    expect(facade.jsonDraft()).toBe('');
    expect(facade.previewResult()).toBeNull();
    expect(facade.errorKey()).toBeNull();
  });
});

function createPreviewResult(canApply: boolean): ContextualBlockPreviewResult {
  return {
    operationId: 'operation-1',
    blockType: 'park.description',
    isApplied: false,
    canApply,
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
      unchanged: 0,
      warnings: 0,
      errors: canApply ? 0 : 1
    },
    changes: [],
    warnings: [],
    errors: []
  };
}

function createBlock(capabilities: AdminContextualBlockInstance['capabilities']): AdminContextualBlockInstance {
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
    capabilities,
    jsonScope: ['park.id', 'park.descriptions[*].value'],
    localizedLanguageCodes: ['fr', 'en'],
    locationFallbackCenter: null,
    adminRoute: ['/', 'fr', 'admin', 'parks', 'edit', 'park-1']
  };
}
