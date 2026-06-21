import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { AdminPublicViewModeFacade } from '../../state/admin-public-view-mode.facade';
import { AdminPublicViewToolbarComponent } from './admin-public-view-toolbar.component';

describe('AdminPublicViewToolbarComponent', () => {
  let fixture: ComponentFixture<AdminPublicViewToolbarComponent>;
  let facade: AdminPublicViewModeFacade;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, AdminPublicViewToolbarComponent],
      providers: [
        ...provideCommonTestDependencies(),
        AdminPublicViewModeFacade
      ]
    }).compileComponents();

    const translateService: TranslateService = TestBed.inject(TranslateService);
    translateService.setTranslation('fr', {
      admin: {
        publicViewToolbar: {
          ariaLabel: 'Barre de vue admin',
          viewMode: 'Vue publique',
          viewModeAriaLabel: 'Choisir la vue publique',
          editionOff: 'Edition off',
          editionOn: 'Edition on',
          enableEditionMode: 'Activer edition',
          disableEditionMode: 'Desactiver edition',
          modes: {
            anonymous: { label: 'Visiteur non connecte', short: 'Visiteur', aria: 'Voir comme visiteur non connecte' },
            user: { label: 'Visiteur role user', short: 'User', aria: 'Voir comme visiteur role user' },
            moderator: { label: 'Visiteur role moderateur', short: 'Modo', aria: 'Voir comme visiteur role moderateur' },
            admin: { label: 'Admin', short: 'Admin', aria: 'Voir comme admin' }
          }
        }
      }
    });
    translateService.use('fr');

    facade = TestBed.inject(AdminPublicViewModeFacade);
    fixture = TestBed.createComponent(AdminPublicViewToolbarComponent);
    fixture.detectChanges();
  });

  it('renders every public view mode with edition disabled by default', () => {
    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    const modeButtons: NodeListOf<HTMLButtonElement> = host.querySelectorAll('.admin-public-view-toolbar__mode');
    const editionButton: HTMLButtonElement | null = host.querySelector('.admin-public-view-toolbar__edition');

    expect(modeButtons.length).toBe(4);
    expect(modeButtons[0]?.getAttribute('aria-checked')).toBe('true');
    expect(editionButton?.disabled).toBeTrue();
    expect(facade.viewMode()).toBe('anonymousVisitor');
    expect(facade.editionModeEnabled()).toBeFalse();
  });

  it('enables edition only after selecting admin preview', () => {
    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    const modeButtons: NodeListOf<HTMLButtonElement> = host.querySelectorAll('.admin-public-view-toolbar__mode');
    const editionButton: HTMLButtonElement = host.querySelector('.admin-public-view-toolbar__edition') as HTMLButtonElement;

    modeButtons[3]?.click();
    fixture.detectChanges();

    expect(facade.viewMode()).toBe('adminPreview');
    expect(editionButton.disabled).toBeFalse();

    editionButton.click();
    fixture.detectChanges();

    expect(facade.editionModeEnabled()).toBeTrue();
    expect(editionButton.getAttribute('aria-pressed')).toBe('true');
  });
});
