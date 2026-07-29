import type { Mock, MockedObject } from 'vitest';
import { DOCUMENT } from '@angular/common';
import { HttpHeaders, HttpResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { ContextualBlocksApiService } from '@data-access/admin/contextual-blocks-api.service';
import { AdminContextualBlockInstance } from '../models/admin-contextual-block.model';
import { ADMIN_CONTEXTUAL_BLOCK_EXPORT_DATA_PORT } from './admin-contextual-block-export-data.ports';
import { AdminContextualBlockExportFacade } from './admin-contextual-block-export.facade';

describe('AdminContextualBlockExportFacade', () => {
  let facade: AdminContextualBlockExportFacade;
  let contextualBlocksApi: MockedObject<ContextualBlocksApiService>;
  let createObjectUrlSpy: Mock;
  let revokeObjectUrlSpy: Mock;
  let anchorClickSpy: Mock;
  let originalCreateObjectUrl: typeof URL.createObjectURL;
  let originalRevokeObjectUrl: typeof URL.revokeObjectURL;

  beforeEach(() => {
    contextualBlocksApi = {
      downloadBlockExport: vi
        .fn()
        .mockName('ContextualBlocksApiService.downloadBlockExport'),
    } as unknown as MockedObject<ContextualBlocksApiService>;

    TestBed.configureTestingModule({
      providers: [
        AdminContextualBlockExportFacade,
        {
          provide: ADMIN_CONTEXTUAL_BLOCK_EXPORT_DATA_PORT,
          useValue: contextualBlocksApi,
        },
      ],
    });

    TestBed.inject(DOCUMENT);
    originalCreateObjectUrl = URL.createObjectURL;
    originalRevokeObjectUrl = URL.revokeObjectURL;
    createObjectUrlSpy = vi.fn().mockReturnValue('blob:contextual-export');
    revokeObjectUrlSpy = vi.fn();
    Object.defineProperty(URL, 'createObjectURL', {
      configurable: true,
      value: createObjectUrlSpy,
    });
    Object.defineProperty(URL, 'revokeObjectURL', {
      configurable: true,
      value: revokeObjectUrlSpy,
    });
    anchorClickSpy = vi
      .spyOn(HTMLAnchorElement.prototype, 'click')
      .mockImplementation(() => {});

    facade = TestBed.inject(AdminContextualBlockExportFacade);
  });

  afterEach(() => {
    Object.defineProperty(URL, 'createObjectURL', {
      configurable: true,
      value: originalCreateObjectUrl,
    });
    Object.defineProperty(URL, 'revokeObjectURL', {
      configurable: true,
      value: originalRevokeObjectUrl,
    });
  });

  it('downloads supported blocks and uses the server filename', () => {
    const blob: Blob = new Blob(['{}'], { type: 'application/json' });
    contextualBlocksApi.downloadBlockExport.mockReturnValue(
      of(
        new HttpResponse({
          body: blob,
          headers: new HttpHeaders({
            'content-disposition': 'attachment; filename="description.json"',
          }),
        }),
      ),
    );

    facade.exportBlock(createBlock(['fullAdminEdit', 'boundedJsonExport']));

    expect(contextualBlocksApi.downloadBlockExport).toHaveBeenCalledTimes(1);

    expect(contextualBlocksApi.downloadBlockExport).toHaveBeenCalledWith(
      'park.description',
      'park-1',
    );
    expect(createObjectUrlSpy).toHaveBeenCalledTimes(1);
    expect(createObjectUrlSpy).toHaveBeenCalledWith(blob);
    expect(anchorClickSpy).toHaveBeenCalled();
    expect(revokeObjectUrlSpy).toHaveBeenCalledTimes(1);
    expect(revokeObjectUrlSpy).toHaveBeenCalledWith('blob:contextual-export');
    expect(facade.errorKey()).toBeNull();
    expect(facade.isExporting()).toBe(false);
  });

  it('does not call the API when the selected block only has a planned export', () => {
    facade.exportBlock(
      createBlock(['fullAdminEdit', 'boundedJsonExportPlanned']),
    );

    expect(contextualBlocksApi.downloadBlockExport).not.toHaveBeenCalled();
    expect(facade.errorKey()).toBe(
      'admin.contextualBlocks.drawer.downloadJsonUnavailable',
    );
  });

  it('exposes a safe error key when the download fails', () => {
    contextualBlocksApi.downloadBlockExport.mockReturnValue(
      throwError(() => new Error('failed')),
    );

    facade.exportBlock(createBlock(['boundedJsonExport']));

    expect(facade.errorKey()).toBe(
      'admin.contextualBlocks.drawer.downloadJsonError',
    );
    expect(facade.isExporting()).toBe(false);
  });
});

function createBlock(
  capabilities: AdminContextualBlockInstance['capabilities'],
): AdminContextualBlockInstance {
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
    adminRoute: ['/', 'fr', 'admin', 'parks', 'edit', 'park-1'],
  };
}
