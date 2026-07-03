import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { PublicContextualBlockMarker } from '@features/public/contextual-editing/models/public-contextual-block-marker.model';
import { PublicContextualBlockDirective } from '@features/public/contextual-editing/ui/public-contextual-block.directive';
import { AdminContextualBlockSelectionFacade } from '../state/admin-contextual-block-selection.facade';
import { AdminPublicViewModeFacade } from '../state/admin-public-view-mode.facade';
import { AdminContextualBlockDomControllerService } from './admin-contextual-block-dom-controller.service';

@Component({
  template: `
    <section [appPublicContextualBlock]="marker">
      <a href="/fr/parks" (click)="$event.preventDefault()">Visitor link</a>
      <p>Visitor content</p>
    </section>
  `,
  imports: [PublicContextualBlockDirective]
})
class HostComponent {
  marker: PublicContextualBlockMarker = {
    type: 'park.description',
    parkId: 'park-1',
    contextLabel: 'Phantasialand',
    languageCode: 'fr'
  };
}

describe('AdminContextualBlockDomControllerService', () => {
  let fixture: ComponentFixture<HostComponent>;
  let controller: AdminContextualBlockDomControllerService;
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

    controller = TestBed.inject(AdminContextualBlockDomControllerService);
    publicViewModeFacade = TestBed.inject(AdminPublicViewModeFacade);
    selectionFacade = TestBed.inject(AdminContextualBlockSelectionFacade);
    fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
  });

  afterEach(() => {
    controller.stop();
  });

  it('activates public markers only when admin edition mode is enabled', () => {
    const host: HTMLElement = getHostElement(fixture);

    controller.start();
    expect(host.querySelector('.admin-contextual-block__edit-button')).toBeNull();

    enableEditionMode(publicViewModeFacade);
    fixture.detectChanges();

    const button: HTMLButtonElement | null = host.querySelector('.admin-contextual-block__edit-button');
    expect(host.classList.contains('admin-contextual-block')).toBeTrue();
    expect(button?.textContent?.trim()).toBe('Modifier');

    host.dispatchEvent(new MouseEvent('click', { bubbles: true }));
    fixture.detectChanges();

    expect(selectionFacade.selectedBlock()?.id).toBe('park.description:park-1');
  });

  it('preserves keyboard activation for nested visitor links', () => {
    controller.start();
    enableEditionMode(publicViewModeFacade);
    fixture.detectChanges();

    const link: HTMLAnchorElement = getHostElement(fixture).querySelector('a') as HTMLAnchorElement;
    const event = new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true });
    link.dispatchEvent(event);
    fixture.detectChanges();

    expect(event.defaultPrevented).toBeFalse();
    expect(selectionFacade.selectedBlock()).toBeNull();
  });
});

function enableEditionMode(publicViewModeFacade: AdminPublicViewModeFacade): void {
  publicViewModeFacade.setViewMode('adminPreview');
  publicViewModeFacade.setEditionModeEnabled(true);
}

function getHostElement(fixture: ComponentFixture<HostComponent>): HTMLElement {
  return (fixture.nativeElement as HTMLElement).querySelector('section') as HTMLElement;
}
