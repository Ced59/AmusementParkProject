import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Signal, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { ContextualBlockPreviewResult } from '@shared/models/admin/contextual-block-preview.models';
import { AdminContextualBlockInstance } from '../../models/admin-contextual-block.model';
import { AdminContextualBlockApplyFacade } from '../../state/admin-contextual-block-apply.facade';
import { AdminContextualBlockExportFacade } from '../../state/admin-contextual-block-export.facade';
import { AdminContextualBlockPreviewFacade } from '../../state/admin-contextual-block-preview.facade';
import { AdminContextualBlockSelectionFacade } from '../../state/admin-contextual-block-selection.facade';
import { AdminPublicViewModeFacade } from '../../state/admin-public-view-mode.facade';
import { AdminContextualBlockDrawerComponent } from './admin-contextual-block-drawer.component';

describe('AdminContextualBlockDrawerComponent', () => {
  let fixture: ComponentFixture<AdminContextualBlockDrawerComponent>;
  let publicViewModeFacade: AdminPublicViewModeFacade;
  let selectionFacade: AdminContextualBlockSelectionFacade;
  let exportFacade: {
    isExporting: Signal<boolean>;
    errorKey: Signal<string | null>;
    canExport: jasmine.Spy;
    exportBlock: jasmine.Spy;
  };
  let previewFacade: {
    jsonDraft: Signal<string>;
    previewResult: Signal<ContextualBlockPreviewResult | null>;
    isPreviewing: Signal<boolean>;
    errorKey: Signal<string | null>;
    canPreview: jasmine.Spy;
    setJsonDraft: jasmine.Spy;
    clearDraft: jasmine.Spy;
    previewBlock: jasmine.Spy;
    resetForBlock: jasmine.Spy;
  };
  let applyFacade: {
    applyResult: Signal<ContextualBlockPreviewResult | null>;
    isApplying: Signal<boolean>;
    errorKey: Signal<string | null>;
    canApply: jasmine.Spy;
    hasAcceptedPreview: jasmine.Spy;
    applyBlock: jasmine.Spy;
    resetForBlock: jasmine.Spy;
    clearResult: jasmine.Spy;
  };

  beforeEach(async () => {
    const isExportingSignal = signal<boolean>(false);
    const errorKeySignal = signal<string | null>(null);
    const jsonDraftSignal = signal<string>('');
    const previewResultSignal = signal<ContextualBlockPreviewResult | null>(null);
    const isPreviewingSignal = signal<boolean>(false);
    const previewErrorKeySignal = signal<string | null>(null);
    const applyResultSignal = signal<ContextualBlockPreviewResult | null>(null);
    const isApplyingSignal = signal<boolean>(false);
    const applyErrorKeySignal = signal<string | null>(null);
    exportFacade = {
      isExporting: isExportingSignal.asReadonly(),
      errorKey: errorKeySignal.asReadonly(),
      canExport: jasmine.createSpy('canExport').and.callFake((block: AdminContextualBlockInstance | null): boolean => {
        return Boolean(block?.capabilities.includes('boundedJsonExport'));
      }),
      exportBlock: jasmine.createSpy('exportBlock')
    };
    previewFacade = {
      jsonDraft: jsonDraftSignal.asReadonly(),
      previewResult: previewResultSignal.asReadonly(),
      isPreviewing: isPreviewingSignal.asReadonly(),
      errorKey: previewErrorKeySignal.asReadonly(),
      canPreview: jasmine.createSpy('canPreview').and.callFake((block: AdminContextualBlockInstance | null): boolean => {
        return Boolean(block?.capabilities.includes('boundedJsonPreview'));
      }),
      setJsonDraft: jasmine.createSpy('setJsonDraft').and.callFake((value: string): void => {
        jsonDraftSignal.set(value);
      }),
      clearDraft: jasmine.createSpy('clearDraft').and.callFake((): void => {
        jsonDraftSignal.set('');
      }),
      previewBlock: jasmine.createSpy('previewBlock'),
      resetForBlock: jasmine.createSpy('resetForBlock').and.callFake((block: AdminContextualBlockInstance | null): void => {
        if (!block) {
          jsonDraftSignal.set('');
        }
      })
    };
    applyFacade = {
      applyResult: applyResultSignal.asReadonly(),
      isApplying: isApplyingSignal.asReadonly(),
      errorKey: applyErrorKeySignal.asReadonly(),
      canApply: jasmine.createSpy('canApply').and.callFake((block: AdminContextualBlockInstance | null): boolean => {
        return Boolean(block?.capabilities.includes('boundedJsonApply'));
      }),
      hasAcceptedPreview: jasmine.createSpy('hasAcceptedPreview').and.returnValue(true),
      applyBlock: jasmine.createSpy('applyBlock'),
      resetForBlock: jasmine.createSpy('resetForBlock'),
      clearResult: jasmine.createSpy('clearResult').and.callFake((): void => {
        applyResultSignal.set(null);
        applyErrorKeySignal.set(null);
      })
    };

    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, AdminContextualBlockDrawerComponent],
      providers: [
        ...provideCommonTestDependencies(),
        AdminPublicViewModeFacade,
        AdminContextualBlockSelectionFacade,
        {
          provide: AdminContextualBlockExportFacade,
          useValue: exportFacade
        },
        {
          provide: AdminContextualBlockPreviewFacade,
          useValue: previewFacade
        },
        {
          provide: AdminContextualBlockApplyFacade,
          useValue: applyFacade
        }
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
            openAdminEdit: 'Ouvrir edition admin complete',
            downloadJson: 'Telecharger le JSON du bloc',
            downloadJsonBusy: 'Telechargement...',
            downloadJsonError: 'Le telechargement JSON a echoue.',
            downloadJsonUnavailable: 'Export JSON indisponible',
            exportJsonAriaLabel: 'Telecharger le JSON borne de ce bloc',
            jsonDraft: 'Brouillon JSON',
            jsonDraftAriaLabel: 'Brouillon JSON borne du bloc',
            previewJson: 'Previsualiser',
            previewJsonBusy: 'Previsualisation...',
            clearJsonDraft: 'Effacer',
            previewJsonInvalid: 'JSON invalide',
            previewJsonError: 'La previsualisation JSON a echoue.',
            previewJsonUnavailable: 'Previsualisation JSON indisponible',
            previewJsonCanApply: 'Previsualisation valide',
            previewJsonBlocked: 'Previsualisation bloquee',
            applyJson: 'Appliquer',
            applyJsonBusy: 'Application...',
            applyJsonInvalid: 'JSON invalide',
            applyJsonError: 'L application JSON a echoue.',
            applyJsonUnavailable: 'Application JSON indisponible',
            applyJsonPreviewRequired: 'Previsualise avant d appliquer.',
            applyJsonSucceeded: 'JSON applique',
            previewChanged: 'Modifies',
            previewErrors: 'Erreurs',
            previewWarnings: 'Alertes',
            previewChanges: 'Changements',
            previewCurrentValue: 'Actuel',
            previewProposedValue: 'Propose',
            previewJsonNoChanges: 'Aucun changement detecte',
            emptyValue: 'Vide'
          },
          capabilities: {
            fullAdminEdit: 'Edition admin complete disponible',
            boundedJsonExport: 'Export JSON borne disponible',
            boundedJsonPreview: 'Previsualisation JSON borne disponible',
            boundedJsonApply: 'Application JSON borne disponible',
            boundedJsonExportPlanned: 'Export JSON borne prevu',
            boundedJsonUpsertPlanned: 'Upsert JSON borne prevu',
            formEditPlanned: 'Formulaire contextuel prevu'
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

  it('renders selected block diagnostics and bounded JSON actions without exposing submit forms', () => {
    publicViewModeFacade.setViewMode('adminPreview');
    publicViewModeFacade.setEditionModeEnabled(true);
    selectionFacade.selectBlock(createBlock());
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    const drawer: HTMLElement = host.querySelector('.admin-contextual-block-drawer') as HTMLElement;
    const adminLink: HTMLAnchorElement | null = host.querySelector('.admin-contextual-block-drawer__footer .admin-contextual-block-drawer__action--primary');
    const exportButton: HTMLButtonElement | null = host.querySelector('.admin-contextual-block-drawer__footer .admin-contextual-block-drawer__action--secondary');
    const previewTextArea: HTMLTextAreaElement | null = host.querySelector('.admin-contextual-block-drawer__json-input');

    expect(drawer.textContent).toContain('park.description');
    expect(drawer.textContent).toContain('park-1');
    expect(drawer.textContent).toContain('fr');
    expect(drawer.textContent).toContain('en');
    expect(previewTextArea).not.toBeNull();
    expect(exportButton?.textContent).toContain('Telecharger le JSON du bloc');
    expect(drawer.textContent).toContain('Appliquer');
    expect(adminLink?.textContent).toContain('Ouvrir edition admin complete');
    expect(drawer.querySelector('button[type="submit"]')).toBeNull();
  });

  it('delegates bounded JSON downloads to the export facade', () => {
    const block: AdminContextualBlockInstance = createBlock();
    publicViewModeFacade.setViewMode('adminPreview');
    publicViewModeFacade.setEditionModeEnabled(true);
    selectionFacade.selectBlock(block);
    fixture.detectChanges();

    const exportButton: HTMLButtonElement = (fixture.nativeElement as HTMLElement)
      .querySelector('.admin-contextual-block-drawer__footer .admin-contextual-block-drawer__action--secondary') as HTMLButtonElement;
    exportButton.click();

    expect(exportFacade.exportBlock).toHaveBeenCalledOnceWith(block);
  });

  it('delegates bounded JSON previews without clearing the draft', () => {
    const block: AdminContextualBlockInstance = createBlock();
    publicViewModeFacade.setViewMode('adminPreview');
    publicViewModeFacade.setEditionModeEnabled(true);
    selectionFacade.selectBlock(block);
    fixture.detectChanges();

    const textArea: HTMLTextAreaElement = (fixture.nativeElement as HTMLElement)
      .querySelector('.admin-contextual-block-drawer__json-input') as HTMLTextAreaElement;
    textArea.value = '{ "block": { "parkId": "park-1" } }';
    textArea.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const previewButton: HTMLButtonElement = (fixture.nativeElement as HTMLElement)
      .querySelector('.admin-contextual-block-drawer__preview-actions .admin-contextual-block-drawer__action--primary') as HTMLButtonElement;
    previewButton.click();

    expect(previewFacade.setJsonDraft).toHaveBeenCalledWith('{ "block": { "parkId": "park-1" } }');
    expect(previewFacade.previewBlock).toHaveBeenCalledOnceWith(block);
    expect(applyFacade.clearResult).toHaveBeenCalled();
    expect(previewFacade.clearDraft).not.toHaveBeenCalled();
  });

  it('delegates bounded JSON apply after an accepted preview', () => {
    const block: AdminContextualBlockInstance = createBlock();
    publicViewModeFacade.setViewMode('adminPreview');
    publicViewModeFacade.setEditionModeEnabled(true);
    selectionFacade.selectBlock(block);
    fixture.detectChanges();

    const buttons: NodeListOf<HTMLButtonElement> = (fixture.nativeElement as HTMLElement)
      .querySelectorAll('.admin-contextual-block-drawer__preview-actions .admin-contextual-block-drawer__action');
    const applyButton: HTMLButtonElement = buttons.item(1);
    applyButton.click();

    expect(applyFacade.applyBlock).toHaveBeenCalledOnceWith(block);
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
    capabilities: ['fullAdminEdit', 'boundedJsonExport', 'boundedJsonPreview', 'boundedJsonApply'],
    jsonScope: ['park.id', 'park.descriptions[*].value'],
    localizedLanguageCodes: ['fr', 'en'],
    adminRoute: ['/', 'fr', 'admin', 'parks', 'edit', 'park-1']
  };
}
