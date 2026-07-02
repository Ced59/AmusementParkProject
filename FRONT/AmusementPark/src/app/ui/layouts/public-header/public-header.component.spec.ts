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
import { Dialog } from '@shared/ui/primitives/dialog';
import { UserDto } from '@app/models/users/user_dto';

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
  let authApiService: jasmine.SpyObj<AuthApiService>;
  let authService: jasmine.SpyObj<AuthService>;
  let modalService: jasmine.SpyObj<ModalService>;

  beforeEach(async () => {
    const imagesApiService: jasmine.SpyObj<ImagesApiService> = jasmine.createSpyObj<ImagesApiService>('ImagesApiService', ['resolveImageUrl']);
    imagesApiService.resolveImageUrl.and.returnValue(null);

    authApiService = jasmine.createSpyObj<AuthApiService>('AuthApiService', ['getCurrentUserById']);
    authService = jasmine.createSpyObj<AuthService>('AuthService', [
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
        imports: [AuthModalComponent, Dialog]
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
      },
      measurementSystem: {
        toggleToImperial: 'Passer aux unités impériales',
        toggleToMetric: 'Passer aux unités métriques'
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

  it('renders the official logo asset in the brand link', () => {
    const logoImage: HTMLImageElement | null = (fixture.nativeElement as HTMLElement).querySelector('.app-public-nav__logo-mark');

    expect(logoImage).not.toBeNull();
    expect(logoImage?.getAttribute('src')).toBe('/assets/general-icon/logo-amusementpark.png');
    expect(logoImage?.getAttribute('alt')).toBe('AMUSEMENT-PARKS.fun');
  });

  it('renders a descriptive alt text on the selected language flag', () => {
    const languageFlag: HTMLImageElement | null = (fixture.nativeElement as HTMLElement).querySelector('.app-public-nav__flag');

    expect(languageFlag).not.toBeNull();
    expect(languageFlag?.getAttribute('alt')).toBe('Fran\u00e7ais');
    expect(languageFlag?.getAttribute('aria-hidden')).toBe('true');
  });

  it('renders the public brand wordmark with the .fun signature', () => {
    const brandLink: HTMLAnchorElement | null = (fixture.nativeElement as HTMLElement).querySelector('.app-public-nav__logo');
    const wordmark: HTMLElement | null = brandLink?.querySelector('.app-brand-wordmark') ?? null;
    const base: HTMLElement | null = wordmark?.querySelector('.app-brand-wordmark__base') ?? null;
    const dot: HTMLElement | null = wordmark?.querySelector('.app-brand-wordmark__dot') ?? null;
    const fun: HTMLElement | null = wordmark?.querySelector('.app-brand-wordmark__fun-text') ?? null;

    expect(brandLink?.getAttribute('aria-label')).toBe('AMUSEMENT-PARKS.fun');
    expect(base?.textContent).toBe('AMUSEMENT-PARKS');
    expect(dot?.textContent).toBe('.');
    expect(fun?.textContent).toBe('fun');
  });

  it('opens the login modal from the named login button', () => {
    const loginButton: HTMLButtonElement = (fixture.nativeElement as HTMLElement).querySelector('.btn-nav--primary') as HTMLButtonElement;

    loginButton.click();

    expect(modalService.openModal).toHaveBeenCalledOnceWith('loginModal');
  });

  it('places the visitor measurement toggle before login and shows the active unit', () => {
    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    const measurementButton: HTMLButtonElement | null = host.querySelector('.app-public-nav__measurement-toggle');
    const loginButton: HTMLButtonElement | null = host.querySelector('.btn-nav--primary');

    expect(measurementButton).not.toBeNull();
    expect(loginButton).not.toBeNull();
    expect(measurementButton?.textContent?.trim()).toBe('m');
    expect(measurementButton?.getAttribute('aria-label')).toBe('Passer aux unités impériales');
    const measurementButtonPosition: number = measurementButton?.compareDocumentPosition(loginButton as Node) ?? 0;
    expect(measurementButtonPosition & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();

    measurementButton?.click();
    fixture.detectChanges();

    expect(measurementButton?.textContent?.trim()).toBe('ft');
    expect(measurementButton?.getAttribute('aria-label')).toBe('Passer aux unités métriques');
  });

  it('hides the header measurement toggle for authenticated users', () => {
    fixture.destroy();

    const user: UserDto = {
      id: 'user-1',
      email: 'user@example.com',
      firstName: 'Ced',
      lastName: 'Caudron',
      isActivated: true,
      isBlocked: false,
      roles: [],
      preferredLanguage: 'FR',
      preferredMeasurementSystem: 'Metric',
      avatarUrl: '',
      createdAt: '2026-06-18T00:00:00Z',
      updatedAt: '2026-06-18T00:00:00Z'
    };

    authService.isLoggedIn.and.returnValue(true);
    authService.getUserIdFromToken.and.returnValue('user-1');
    authApiService.getCurrentUserById.and.returnValue(of(user));

    fixture = TestBed.createComponent(PublicHeaderComponent);
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    expect(host.querySelector('.app-public-nav__measurement-toggle')).toBeNull();
    expect(host.querySelector('.app-public-nav__profile')).not.toBeNull();
  });

  it('renders authenticated user avatars with descriptive alt text', () => {
    fixture.destroy();

    const user: UserDto = {
      id: 'user-1',
      email: 'user@example.com',
      firstName: 'Ced',
      lastName: 'Caudron',
      isActivated: true,
      isBlocked: false,
      roles: [],
      preferredLanguage: 'FR',
      preferredMeasurementSystem: 'Metric',
      avatarUrl: 'avatars/user-1.png',
      createdAt: '2026-06-18T00:00:00Z',
      updatedAt: '2026-06-18T00:00:00Z'
    };

    authService.isLoggedIn.and.returnValue(true);
    authService.getUserIdFromToken.and.returnValue('user-1');
    authApiService.getCurrentUserById.and.returnValue(of(user));
    const imagesApiService: jasmine.SpyObj<ImagesApiService> = TestBed.inject(ImagesApiService) as jasmine.SpyObj<ImagesApiService>;
    imagesApiService.resolveImageUrl.and.returnValue('/api/images/avatar-user-1');

    fixture = TestBed.createComponent(PublicHeaderComponent);
    fixture.detectChanges();

    const avatarImage: HTMLImageElement | null = (fixture.nativeElement as HTMLElement).querySelector('.app-public-nav__profile-avatar');
    expect(avatarImage).not.toBeNull();
    expect(avatarImage?.getAttribute('src')).toBe('/api/images/avatar-user-1');
    expect(avatarImage?.getAttribute('alt')).toBe('Ced Caudron avatar');
  });
});
