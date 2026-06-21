import { TestBed } from '@angular/core/testing';

import { AdminPublicViewModeFacade } from './admin-public-view-mode.facade';

describe('AdminPublicViewModeFacade', () => {
  let facade: AdminPublicViewModeFacade;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [AdminPublicViewModeFacade]
    });

    facade = TestBed.inject(AdminPublicViewModeFacade);
  });

  it('starts in anonymous visitor mode with edition disabled', () => {
    expect(facade.viewMode()).toBe('anonymousVisitor');
    expect(facade.editionModeEnabled()).toBeFalse();
    expect(facade.canEdit()).toBeFalse();
  });

  it('allows edition only in admin preview mode', () => {
    facade.setEditionModeEnabled(true);

    expect(facade.editionModeEnabled()).toBeFalse();

    facade.setViewMode('adminPreview');
    facade.setEditionModeEnabled(true);

    expect(facade.canEdit()).toBeTrue();
    expect(facade.editionModeEnabled()).toBeTrue();
  });

  it('disables edition when switching back to a visitor view', () => {
    facade.setViewMode('adminPreview');
    facade.setEditionModeEnabled(true);

    facade.setViewMode('userVisitor');

    expect(facade.viewMode()).toBe('userVisitor');
    expect(facade.canEdit()).toBeFalse();
    expect(facade.editionModeEnabled()).toBeFalse();
  });

  it('resets view and edition state together', () => {
    facade.setViewMode('adminPreview');
    facade.setEditionModeEnabled(true);

    facade.reset();

    expect(facade.viewMode()).toBe('anonymousVisitor');
    expect(facade.editionModeEnabled()).toBeFalse();
  });
});
