import { TestBed } from '@angular/core/testing';

import { AdminPublicViewModeFacade } from './admin-public-view-mode.facade';

describe('AdminPublicViewModeFacade', () => {
  let facade: AdminPublicViewModeFacade;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [AdminPublicViewModeFacade],
    });

    facade = TestBed.inject(AdminPublicViewModeFacade);
  });

  it('starts in anonymous visitor mode with edition disabled', () => {
    expect(facade.viewMode()).toBe('anonymousVisitor');
    expect(facade.editionModeEnabled()).toBe(false);
    expect(facade.canEdit()).toBe(false);
  });

  it('allows edition only in admin preview mode', () => {
    facade.setEditionModeEnabled(true);

    expect(facade.editionModeEnabled()).toBe(false);

    facade.setViewMode('adminPreview');
    facade.setEditionModeEnabled(true);

    expect(facade.canEdit()).toBe(true);
    expect(facade.editionModeEnabled()).toBe(true);
  });

  it('disables edition when switching back to a visitor view', () => {
    facade.setViewMode('adminPreview');
    facade.setEditionModeEnabled(true);

    facade.setViewMode('userVisitor');

    expect(facade.viewMode()).toBe('userVisitor');
    expect(facade.canEdit()).toBe(false);
    expect(facade.editionModeEnabled()).toBe(false);
  });

  it('resets view and edition state together', () => {
    facade.setViewMode('adminPreview');
    facade.setEditionModeEnabled(true);

    facade.reset();

    expect(facade.viewMode()).toBe('anonymousVisitor');
    expect(facade.editionModeEnabled()).toBe(false);
  });
});
