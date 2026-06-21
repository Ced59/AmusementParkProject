import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { AdminContextualBlockInstance } from '../../models/admin-contextual-block.model';
import { AdminContextualBlockSelectionFacade } from '../../state/admin-contextual-block-selection.facade';
import { AdminPublicViewModeFacade } from '../../state/admin-public-view-mode.facade';
import { AdminContextualBlockDrawerComponent } from './admin-contextual-block-drawer.component';

describe('AdminContextualBlockDrawerComponent', () => {
  let fixture: ComponentFixture<AdminContextualBlockDrawerComponent>;
  let publicViewModeFacade: AdminPublicViewModeFacade;
  let selectionFacade: AdminContextualBlockSelectionFacade;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, AdminContextualBlockDrawerComponent],
      providers: [
        ...provideCommonTestDependencies(),
        AdminPublicViewModeFacade,
        AdminContextualBlockSelectionFacade
      ]
    }).compileComponents();

    const translateService: TranslateService = TestBed.inject(TranslateService);
    translateService.setTranslation('fr', {
      admin: {
        contextualBlocks: {
          drawer: {
            ariaLabel: 'Bloc editable selectionne',
            kicker: 'Bloc contexte',
            close: 'Fermer',
            type: 'Type de bloc',
            entity: 'Entite',
            label: 'Element courant',
            capabilities: 'Capacites',
            localizedLanguages: 'Localise en',
            jsonScope: 'Perimetre JSON',
            openAdminEdit: 'Ouvrir edition admin complete'
          },
          capabilities: {
            fullAdminEdit: 'Edition admin complete disponible',
            boundedJsonExportPlanned: 'Export JSON borne prevu'
          },
          blocks: {
            parkDescription: {
              label: 'Description du parc',
              description: 'Description localisee'
            }
          }
        }
      }
    });
    translateService.use('fr');

    publicViewModeFacade = TestBed.inject(AdminPublicViewModeFacade);
    selectionFacade = TestBed.inject(AdminContextualBlockSelectionFacade);
    fixture = TestBed.createComponent(AdminContextualBlockDrawerComponent);
  });

  it('stays absent until a block is selected', () => {
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('.admin-contextual-block-drawer')).toBeNull();
  });

  it('renders selected block diagnostics without exposing mutation actions', () => {
    publicViewModeFacade.setViewMode('adminPreview');
    publicViewModeFacade.setEditionModeEnabled(true);
    selectionFacade.selectBlock(createBlock());
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    const drawer: HTMLElement = host.querySelector('.admin-contextual-block-drawer') as HTMLElement;
    const adminLink: HTMLAnchorElement | null = host.querySelector('.admin-contextual-block-drawer__admin-link');

    expect(drawer.textContent).toContain('park.description');
    expect(drawer.textContent).toContain('park-1');
    expect(drawer.textContent).toContain('fr');
    expect(drawer.textContent).toContain('en');
    expect(adminLink?.textContent).toContain('Ouvrir edition admin complete');
    expect(drawer.querySelector('button[type="submit"]')).toBeNull();
  });

  it('clears the selected block from the close action', () => {
    publicViewModeFacade.setViewMode('adminPreview');
    publicViewModeFacade.setEditionModeEnabled(true);
    selectionFacade.selectBlock(createBlock());
    fixture.detectChanges();

    const closeButton: HTMLButtonElement = (fixture.nativeElement as HTMLElement)
      .querySelector('.admin-contextual-block-drawer__close') as HTMLButtonElement;
    closeButton.click();
    fixture.detectChanges();

    expect(selectionFacade.selectedBlock()).toBeNull();
    expect((fixture.nativeElement as HTMLElement).querySelector('.admin-contextual-block-drawer')).toBeNull();
  });
});

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
    capabilities: ['fullAdminEdit', 'boundedJsonExportPlanned'],
    jsonScope: ['park.id', 'park.descriptions[*].value'],
    localizedLanguageCodes: ['fr', 'en'],
    adminRoute: ['/', 'fr', 'admin', 'parks', 'edit', 'park-1']
  };
}
