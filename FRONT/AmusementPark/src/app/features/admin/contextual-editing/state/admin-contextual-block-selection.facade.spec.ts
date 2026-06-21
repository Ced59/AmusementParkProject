import { TestBed } from '@angular/core/testing';

import { AdminContextualBlockInstance } from '../models/admin-contextual-block.model';
import { AdminPublicViewModeFacade } from './admin-public-view-mode.facade';
import { AdminContextualBlockSelectionFacade } from './admin-contextual-block-selection.facade';

describe('AdminContextualBlockSelectionFacade', () => {
  let publicViewModeFacade: AdminPublicViewModeFacade;
  let selectionFacade: AdminContextualBlockSelectionFacade;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        AdminPublicViewModeFacade,
        AdminContextualBlockSelectionFacade
      ]
    });

    publicViewModeFacade = TestBed.inject(AdminPublicViewModeFacade);
    selectionFacade = TestBed.inject(AdminContextualBlockSelectionFacade);
  });

  it('ignores block selections while edition mode is disabled', () => {
    selectionFacade.selectBlock(createBlock());

    expect(selectionFacade.selectedBlock()).toBeNull();
    expect(selectionFacade.hasSelection()).toBeFalse();
  });

  it('stores the selected block only in admin edition mode', () => {
    publicViewModeFacade.setViewMode('adminPreview');
    publicViewModeFacade.setEditionModeEnabled(true);

    selectionFacade.selectBlock(createBlock());

    expect(selectionFacade.selectedBlock()?.id).toBe('park.hero:park-1');
    expect(selectionFacade.hasSelection()).toBeTrue();
  });

  it('clears the selected block when edition mode is disabled', () => {
    publicViewModeFacade.setViewMode('adminPreview');
    publicViewModeFacade.setEditionModeEnabled(true);
    selectionFacade.selectBlock(createBlock());

    publicViewModeFacade.setEditionModeEnabled(false);
    TestBed.flushEffects();

    expect(selectionFacade.selectedBlock()).toBeNull();
  });
});

function createBlock(): AdminContextualBlockInstance {
  return {
    id: 'park.hero:park-1',
    type: 'park.hero',
    entityType: 'Park',
    entityId: 'park-1',
    contextLabel: 'Phantasialand',
    ids: { parkId: 'park-1' },
    labelKey: 'admin.contextualBlocks.blocks.parkHero.label',
    descriptionKey: 'admin.contextualBlocks.blocks.parkHero.description',
    iconClass: 'pi pi-image',
    capabilities: ['fullAdminEdit'],
    jsonScope: ['park.id'],
    localizedLanguageCodes: [],
    adminRoute: ['/', 'fr', 'admin', 'parks', 'edit', 'park-1']
  };
}
