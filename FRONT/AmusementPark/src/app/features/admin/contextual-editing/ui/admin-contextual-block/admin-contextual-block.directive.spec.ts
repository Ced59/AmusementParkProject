import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { AdminContextualBlockInstance } from '../../models/admin-contextual-block.model';
import { AdminContextualBlockSelectionFacade } from '../../state/admin-contextual-block-selection.facade';
import { AdminPublicViewModeFacade } from '../../state/admin-public-view-mode.facade';
import { AdminContextualBlockDirective } from './admin-contextual-block.directive';

@Component({
  template: `
    <section [appAdminContextualBlock]="block">
      <a href="/fr/parks" (click)="$event.preventDefault()">Visitor link</a>
      <p>Visitor content</p>
    </section>
  `,
  imports: [AdminContextualBlockDirective]
})
class HostComponent {
  block: AdminContextualBlockInstance | null = createBlock();
}

describe('AdminContextualBlockDirective', () => {
  let fixture: ComponentFixture<HostComponent>;
  let publicViewModeFacade: AdminPublicViewModeFacade;
  let selectionFacade: AdminContextualBlockSelectionFacade;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, HostComponent],
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
          editAction: 'Modifier'
        }
      }
    });
    translateService.use('fr');

    publicViewModeFacade = TestBed.inject(AdminPublicViewModeFacade);
    selectionFacade = TestBed.inject(AdminContextualBlockSelectionFacade);
    fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
  });

  it('does not add admin controls or attributes while edition mode is disabled', () => {
    const host: HTMLElement = getHostElement(fixture);

    expect(host.querySelector('.admin-contextual-block__edit-button')).toBeNull();
    expect(host.classList.contains('admin-contextual-block')).toBeFalse();
    expect(host.hasAttribute('data-admin-contextual-block-type')).toBeFalse();
  });

  it('adds a touch-friendly edit action in edition mode', () => {
    enableEditionMode(publicViewModeFacade);
    fixture.detectChanges();

    const host: HTMLElement = getHostElement(fixture);
    const button: HTMLButtonElement | null = host.querySelector('.admin-contextual-block__edit-button');

    expect(host.classList.contains('admin-contextual-block')).toBeTrue();
    expect(host.getAttribute('data-admin-contextual-block-type')).toBe('park.hero');
    expect(button?.textContent?.trim()).toBe('Modifier');
  });

  it('selects the block from the edit action without mutating data', () => {
    enableEditionMode(publicViewModeFacade);
    fixture.detectChanges();

    const button: HTMLButtonElement = getHostElement(fixture).querySelector('.admin-contextual-block__edit-button') as HTMLButtonElement;
    button.click();
    fixture.detectChanges();

    expect(selectionFacade.selectedBlock()?.id).toBe('park.hero:park-1');
    expect(getHostElement(fixture).classList.contains('admin-contextual-block--selected')).toBeTrue();
  });

  it('keeps visitor links usable instead of selecting the block', () => {
    enableEditionMode(publicViewModeFacade);
    fixture.detectChanges();

    const link: HTMLAnchorElement = getHostElement(fixture).querySelector('a') as HTMLAnchorElement;
    link.click();
    fixture.detectChanges();

    expect(selectionFacade.selectedBlock()).toBeNull();
  });

  it('keeps keyboard activation on visitor links usable instead of selecting the block', () => {
    enableEditionMode(publicViewModeFacade);
    fixture.detectChanges();

    const link: HTMLAnchorElement = getHostElement(fixture).querySelector('a') as HTMLAnchorElement;
    const event = new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true });
    link.dispatchEvent(event);
    fixture.detectChanges();

    expect(event.defaultPrevented).toBeFalse();
    expect(selectionFacade.selectedBlock()).toBeNull();
  });

  it('removes admin controls when edition mode is disabled again', () => {
    enableEditionMode(publicViewModeFacade);
    fixture.detectChanges();

    publicViewModeFacade.setEditionModeEnabled(false);
    fixture.detectChanges();

    const host: HTMLElement = getHostElement(fixture);
    expect(host.querySelector('.admin-contextual-block__edit-button')).toBeNull();
    expect(host.classList.contains('admin-contextual-block')).toBeFalse();
    expect(host.hasAttribute('data-admin-contextual-block-type')).toBeFalse();
  });
});

function enableEditionMode(publicViewModeFacade: AdminPublicViewModeFacade): void {
  publicViewModeFacade.setViewMode('adminPreview');
  publicViewModeFacade.setEditionModeEnabled(true);
}

function getHostElement(fixture: ComponentFixture<HostComponent>): HTMLElement {
  return (fixture.nativeElement as HTMLElement).querySelector('section') as HTMLElement;
}

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
    locationFallbackCenter: null,
    adminRoute: ['/', 'fr', 'admin', 'parks', 'edit', 'park-1']
  };
}
