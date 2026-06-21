import { signal, WritableSignal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { ContextualBlockPreviewResult } from '@shared/models/admin/contextual-block-preview.models';
import { ContextualBlocksApiService } from '@data-access/admin/contextual-blocks-api.service';
import { AdminContextualBlockInstance } from '../models/admin-contextual-block.model';
import { ADMIN_CONTEXTUAL_BLOCK_APPLY_DATA_PORT } from './admin-contextual-block-apply-data.ports';
import { AdminContextualBlockApplyFacade } from './admin-contextual-block-apply.facade';
import { AdminContextualBlockPreviewFacade } from './admin-contextual-block-preview.facade';
import { AdminContextualBlockRefreshEvents } from './admin-contextual-block-refresh-events.service';

describe('AdminContextualBlockApplyFacade', () => {
  let facade: AdminContextualBlockApplyFacade;
  let contextualBlocksApi: jasmine.SpyObj<ContextualBlocksApiService>;
  let previewFacade: {
    jsonDraft: WritableSignal<string>;
    previewResult: WritableSignal<ContextualBlockPreviewResult | null>;
    useServerResult: jasmine.Spy;
  };
  let refreshEvents: jasmine.SpyObj<AdminContextualBlockRefreshEvents>;

  beforeEach(() => {
    contextualBlocksApi = jasmine.createSpyObj<ContextualBlocksApiService>('ContextualBlocksApiService', ['applyBlock']);
    previewFacade = {
      jsonDraft: signal<string>(''),
      previewResult: signal<ContextualBlockPreviewResult | null>(null),
      useServerResult: jasmine.createSpy('useServerResult')
    };
    refreshEvents = jasmine.createSpyObj<AdminContextualBlockRefreshEvents>('AdminContextualBlockRefreshEvents', ['notifyBlockApplied']);

    TestBed.configureTestingModule({
      providers: [
        AdminContextualBlockApplyFacade,
        {
          provide: ADMIN_CONTEXTUAL_BLOCK_APPLY_DATA_PORT,
          useValue: contextualBlocksApi
        },
        {
          provide: AdminContextualBlockPreviewFacade,
          useValue: previewFacade
        },
        {
          provide: AdminContextualBlockRefreshEvents,
          useValue: refreshEvents
        }
      ]
    });

    facade = TestBed.inject(AdminContextualBlockApplyFacade);
  });

  it('applies the parsed JSON and emits a targeted refresh event', () => {
    const block: AdminContextualBlockInstance = createBlock(['boundedJsonApply']);
    const applyResult: ContextualBlockPreviewResult = createPreviewResult(true, true);
    contextualBlocksApi.applyBlock.and.returnValue(of(applyResult));
    previewFacade.jsonDraft.set('{ "block": { "parkId": "park-1" } }');
    previewFacade.previewResult.set(createPreviewResult(true, false));

    facade.applyBlock(block);

    expect(contextualBlocksApi.applyBlock).toHaveBeenCalledOnceWith('park.description', 'park-1', {
      block: {
        parkId: 'park-1'
      }
    });
    expect(previewFacade.useServerResult).toHaveBeenCalledOnceWith(applyResult);
    expect(facade.applyResult()).toBe(applyResult);
    expect(facade.errorKey()).toBeNull();
    expect(facade.isApplying()).toBeFalse();
    expect(refreshEvents.notifyBlockApplied).toHaveBeenCalledOnceWith(jasmine.objectContaining({
      blockType: 'park.description',
      entityType: 'Park',
      entityId: 'park-1'
    }));
  });

  it('requires an accepted preview before applying JSON', () => {
    const block: AdminContextualBlockInstance = createBlock(['boundedJsonApply']);
    previewFacade.jsonDraft.set('{ "block": { "parkId": "park-1" } }');
    previewFacade.previewResult.set(createPreviewResult(false, false));

    facade.applyBlock(block);

    expect(contextualBlocksApi.applyBlock).not.toHaveBeenCalled();
    expect(refreshEvents.notifyBlockApplied).not.toHaveBeenCalled();
    expect(facade.errorKey()).toBe('admin.contextualBlocks.drawer.applyJsonPreviewRequired');
  });

  it('keeps the draft and avoids refresh events when apply fails', () => {
    contextualBlocksApi.applyBlock.and.returnValue(throwError(() => new Error('failed')));
    previewFacade.jsonDraft.set('{ "block": { "parkId": "park-1" } }');
    previewFacade.previewResult.set(createPreviewResult(true, false));

    facade.applyBlock(createBlock(['boundedJsonApply']));

    expect(previewFacade.jsonDraft()).toBe('{ "block": { "parkId": "park-1" } }');
    expect(facade.applyResult()).toBeNull();
    expect(facade.errorKey()).toBe('admin.contextualBlocks.drawer.applyJsonError');
    expect(refreshEvents.notifyBlockApplied).not.toHaveBeenCalled();
  });
});

function createPreviewResult(canApply: boolean, isApplied: boolean): ContextualBlockPreviewResult {
  return {
    operationId: 'operation-1',
    blockType: 'park.description',
    isApplied,
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
      unchanged: 7,
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
