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
  let contextualBlocksApi: jasmine.SpyObj<ContextualBlocksApiService>;
  let createObjectUrlSpy: jasmine.Spy;
  let revokeObjectUrlSpy: jasmine.Spy;
  let anchorClickSpy: jasmine.Spy;
  let originalCreateObjectUrl: typeof URL.createObjectURL;
  let originalRevokeObjectUrl: typeof URL.revokeObjectURL;

  beforeEach(() => {
    contextualBlocksApi = jasmine.createSpyObj<ContextualBlocksApiService>('ContextualBlocksApiService', ['downloadBlockExport']);

    TestBed.configureTestingModule({
      providers: [
        AdminContextualBlockExportFacade,
        {
          provide: ADMIN_CONTEXTUAL_BLOCK_EXPORT_DATA_PORT,
          useValue: contextualBlocksApi
        }
      ]
    });

    TestBed.inject(DOCUMENT);
    originalCreateObjectUrl = URL.createObjectURL;
    originalRevokeObjectUrl = URL.revokeObjectURL;
    createObjectUrlSpy = jasmine.createSpy('createObjectURL').and.returnValue('blob:contextual-export');
    revokeObjectUrlSpy = jasmine.createSpy('revokeObjectURL');
    Object.defineProperty(URL, 'createObjectURL', {
      configurable: true,
      value: createObjectUrlSpy
    });
    Object.defineProperty(URL, 'revokeObjectURL', {
      configurable: true,
      value: revokeObjectUrlSpy
    });
    anchorClickSpy = spyOn(HTMLAnchorElement.prototype, 'click').and.stub();

    facade = TestBed.inject(AdminContextualBlockExportFacade);
  });

  afterEach(() => {
    Object.defineProperty(URL, 'createObjectURL', {
      configurable: true,
      value: originalCreateObjectUrl
    });
    Object.defineProperty(URL, 'revokeObjectURL', {
      configurable: true,
      value: originalRevokeObjectUrl
    });
  });

  it('downloads supported blocks and uses the server filename', () => {
    const blob: Blob = new Blob(['{}'], { type: 'application/json' });
    contextualBlocksApi.downloadBlockExport.and.returnValue(of(new HttpResponse({
      body: blob,
      headers: new HttpHeaders({
        'content-disposition': 'attachment; filename="description.json"'
      })
    })));

    facade.exportBlock(createBlock(['fullAdminEdit', 'boundedJsonExport']));

    expect(contextualBlocksApi.downloadBlockExport).toHaveBeenCalledOnceWith('park.description', 'park-1');
    expect(createObjectUrlSpy).toHaveBeenCalledOnceWith(blob);
    expect(anchorClickSpy).toHaveBeenCalled();
    expect(revokeObjectUrlSpy).toHaveBeenCalledOnceWith('blob:contextual-export');
    expect(facade.errorKey()).toBeNull();
    expect(facade.isExporting()).toBeFalse();
  });

  it('does not call the API when the selected block only has a planned export', () => {
    facade.exportBlock(createBlock(['fullAdminEdit', 'boundedJsonExportPlanned']));

    expect(contextualBlocksApi.downloadBlockExport).not.toHaveBeenCalled();
    expect(facade.errorKey()).toBe('admin.contextualBlocks.drawer.downloadJsonUnavailable');
  });

  it('exposes a safe error key when the download fails', () => {
    contextualBlocksApi.downloadBlockExport.and.returnValue(throwError(() => new Error('failed')));

    facade.exportBlock(createBlock(['boundedJsonExport']));

    expect(facade.errorKey()).toBe('admin.contextualBlocks.drawer.downloadJsonError');
    expect(facade.isExporting()).toBeFalse();
  });
});

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
