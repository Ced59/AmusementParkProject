import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EventEmitter, NO_ERRORS_SCHEMA, Signal, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { AuthApiService } from '@data-access/auth/auth-api.service';
import { ImagesApiService } from '@data-access/images/images-api.service';
import { AuthService } from '@app/services/auth/auth.service';
import { ModalService } from '@app/services/modal/modal.service';
import { SharedService } from '@app/services/shared/shared.service';
import { ThemeService } from '@app/services/themes/themes.service';
import { TranslationService } from '@app/services/translation.service';
import { PublicParkNavigationTreeFacade } from '@features/public/navigation/state/public-park-navigation-tree.facade';
import { PublicParkNavigationTreeViewModel } from '@features/public/navigation/models/public-park-navigation-tree.model';
import { AuthModalComponent } from '@features/auth/ui/auth-modal/auth-modal.component';
import { PublicHeaderComponent } from './public-header.component';
import { Avatar } from 'primeng/avatar';
import { Dialog } from 'primeng/dialog';

class PublicParkNavigationTreeFacadeStub {
  private readonly treeSignal = signal<PublicParkNavigationTreeViewModel>({
    isAvailable: false,
    isLoading: false,
    items: []
  });

  readonly tree: Signal<PublicParkNavigationTreeViewModel> = this.treeSignal.asReadonly();
}

describe('PublicHeaderComponent', () => {
  let fixture: ComponentFixture<PublicHeaderComponent>;
  let modalService: jasmine.SpyObj<ModalService>;

  beforeEach(async () => {
    const imagesApiService: jasmine.SpyObj<ImagesApiService> = jasmine.createSpyObj<ImagesApiService>('ImagesApiService', ['resolveImageUrl']);
    imagesApiService.resolveImageUrl.and.returnValue(null);

    const authApiService: jasmine.SpyObj<AuthApiService> = jasmine.createSpyObj<AuthApiService>('AuthApiService', ['getCurrentUserById']);
    const authService: jasmine.SpyObj<AuthService> = jasmine.createSpyObj<AuthService>('AuthService', [
      'ensureValidAccessToken',
      'getUserIdFromToken',
      'hasRole',
      'isLoggedIn'
    ]);
    authService.ensureValidAccessToken.and.returnValue(of(null));
    authService.getUserIdFromToken.and.returnValue(null);
    authService.hasRole.and.returnValue(false);
    authService.isLoggedIn.and.returnValue(false);

    modalService = jasmine.createSpyObj<ModalService>('ModalService', ['closeModal', 'getModalStatus', 'openModal']);
    modalService.getModalStatus.and.returnValue(of(false));

    const sharedService: jasmine.SpyObj<SharedService> = jasmine.createSpyObj<SharedService>('SharedService', ['getLoginStatusListener']);
    sharedService.getLoginStatusListener.and.returnValue(of());

    const themeService: jasmine.SpyObj<ThemeService> = jasmine.createSpyObj<ThemeService>('ThemeService', ['changeTheme', 'getCurrentTheme']);
    themeService.getCurrentTheme.and.returnValue('dark');

    const translationService: jasmine.SpyObj<TranslationService> = jasmine.createSpyObj<TranslationService>(
      'TranslationService',
      ['getCurrentLang', 'useLang'],
      { languageChanged: new EventEmitter<string>() }
    );
    translationService.getCurrentLang.and.returnValue('fr');
    translationService.useLang.and.returnValue(of(null));

    TestBed.overrideComponent(PublicHeaderComponent, {
      remove: {
        imports: [Avatar, AuthModalComponent, Dialog]
      },
      add: {
        schemas: [NO_ERRORS_SCHEMA]
      }
    });

    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, PublicHeaderComponent],
      providers: [
        ...provideCommonTestDependencies(),
        { provide: ImagesApiService, useValue: imagesApiService },
        { provide: AuthApiService, useValue: authApiService },
        { provide: AuthService, useValue: authService },
        { provide: ModalService, useValue: modalService },
        { provide: SharedService, useValue: sharedService },
        { provide: ThemeService, useValue: themeService },
        { provide: TranslationService, useValue: translationService },
        { provide: PublicParkNavigationTreeFacade, useClass: PublicParkNavigationTreeFacadeStub }
      ],
      schemas: [NO_ERRORS_SCHEMA]
    }).compileComponents();

    const translateService: TranslateService = TestBed.inject(TranslateService);
    translateService.setTranslation('fr', {
      sidebar: {
        about: 'A propos',
        home: 'Accueil',
        parks: 'Parcs'
      },
      topbar: {
        choose_language: 'Choisir la langue',
        login_header: 'Connexion / Inscription'
      }
    });
    translateService.use('fr');

    fixture = TestBed.createComponent(PublicHeaderComponent);
    fixture.detectChanges();
  });

  it('keeps an accessible name on the compact login button', () => {
    const loginButton: HTMLButtonElement | null = (fixture.nativeElement as HTMLElement).querySelector('.btn-nav--primary');

    expect(loginButton).not.toBeNull();
    expect(loginButton?.getAttribute('aria-label')).toBe('Connexion / Inscription');
  });

  it('opens the login modal from the named login button', () => {
    const loginButton: HTMLButtonElement = (fixture.nativeElement as HTMLElement).querySelector('.btn-nav--primary') as HTMLButtonElement;

    loginButton.click();

    expect(modalService.openModal).toHaveBeenCalledOnceWith('loginModal');
  });
});
