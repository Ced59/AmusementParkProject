import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { EMPTY, switchMap } from 'rxjs';
import { catchError, filter, tap } from 'rxjs/operators';

import { TranslateModule } from '@ngx-translate/core';
import { Avatar } from 'primeng/avatar';
import { Dialog } from 'primeng/dialog';

import { UserDto } from '@app/models/users/user_dto';
import { AuthModalComponent } from '@features/auth/ui/auth-modal/auth-modal.component';
import { ThemeSwitcherComponent } from '@shared/components/theme-switcher/theme-switcher.component';
import { UiButtonDirective, UiChipComponent, UiSectionHeaderComponent } from '@ui/primitives';
import { AuthApiService } from '@data-access/auth/auth-api.service';
import { ImagesApiService } from '@data-access/images/images-api.service';
import { AuthService } from '@app/services/auth/auth.service';
import { ModalName, ModalService } from '@app/services/modal/modal.service';
import { SharedService } from '@app/services/shared/shared.service';
import { TranslationService } from '@app/services/translation.service';
import { LANGUAGES, LanguageOption } from '@shared/models/localization';
import { resolveFlagAssetPath } from '@shared/utils/assets/flag-assets';
import { resolveSupportedLanguage, resolveSupportedLanguageFromUrl } from '@shared/utils/routing/localized-route.helpers';
import { PublicParkNavigationTreeFacade } from '@features/public/navigation/state/public-park-navigation-tree.facade';
import { PublicParkNavigationTreeViewModel } from '@features/public/navigation/models/public-park-navigation-tree.model';
import { MeasurementPreferenceService } from '@app/services/measurements/measurement-preference.service';
import { MeasurementSystem } from '@shared/models/measurements/measurement-system.model';

@Component({
  selector: 'app-public-header',
  templateUrl: './public-header.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    Avatar,
    AuthModalComponent,
    Dialog,
    RouterLink,
    RouterLinkActive,
    ThemeSwitcherComponent,
    TranslateModule,
    UiButtonDirective,
    UiChipComponent,
    UiSectionHeaderComponent
  ]
})
export class PublicHeaderComponent implements OnInit {
  protected readonly languages: readonly LanguageOption[] = LANGUAGES;
  protected readonly selectedLanguage = signal<string>('en');
  protected readonly preferredMeasurementSystem = this.measurementPreferenceService.preferredSystem;
  protected readonly currentUrl = signal<string>('');
  protected readonly displayLoginModal = signal<boolean>(false);
  protected readonly displayLanguageModal = signal<boolean>(false);
  protected readonly isParksNavigationOpen = signal<boolean>(false);
  protected readonly parkNavigationTree: Signal<PublicParkNavigationTreeViewModel> = this.publicParkNavigationTreeFacade.tree;
  protected readonly isLoggedIn = signal<boolean>(false);
  protected readonly isAdmin = signal<boolean>(false);
  protected readonly userProfile = signal<UserDto | null>(null);
  protected readonly userAvatarUrl: Signal<string | null> = computed(() => {
    return this.imagesApiService.resolveImageUrl(this.userProfile()?.avatarUrl);
  });

  constructor(
    private readonly imagesApiService: ImagesApiService,
    private readonly authApiService: AuthApiService,
    private readonly authService: AuthService,
    private readonly translationService: TranslationService,
    private readonly measurementPreferenceService: MeasurementPreferenceService,
    private readonly publicParkNavigationTreeFacade: PublicParkNavigationTreeFacade,
    private readonly router: Router,
    private readonly modalService: ModalService,
    private readonly sharedService: SharedService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  ngOnInit(): void {
    this.selectedLanguage.set(this.getLanguageFromUrl());
    this.currentUrl.set(this.router.url);
    this.checkLoginStatus();

    this.modalService.getModalStatus('loginModal')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((status: boolean): void => {
        this.displayLoginModal.set(status);
      });

    this.modalService.getModalStatus('languageModal')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((status: boolean): void => {
        this.displayLanguageModal.set(status);
      });

    this.router.events.pipe(
      filter((event: unknown): event is NavigationEnd => event instanceof NavigationEnd),
      tap((): void => {
        this.selectedLanguage.set(this.getLanguageFromUrl());
        this.currentUrl.set(this.router.url);
        this.closeParksNavigation();
      }),
      switchMap(() => this.translationService.useLang(this.selectedLanguage()).pipe(
        catchError((error: unknown) => {
          console.error('Error loading language:', error);
          return EMPTY;
        })
      )),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe();

    this.sharedService.getLoginStatusListener()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((): void => {
        this.checkLoginStatus();
      });
  }


  protected toggleParksNavigation(): void {
    if (!this.parkNavigationTree().isAvailable) {
      return;
    }

    this.isParksNavigationOpen.update((value: boolean) => !value);
  }

  protected closeParksNavigation(): void {
    this.isParksNavigationOpen.set(false);
  }

  protected openModal(modalName: ModalName): void {
    this.modalService.openModal(modalName);
  }

  protected closeModal(modalName: ModalName): void {
    this.modalService.closeModal(modalName);

    if (modalName === 'loginModal') {
      this.checkLoginStatus();
    }
  }


  protected navigateToCurrentUserProfile(): void {
    this.authService.ensureValidAccessToken(true)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((token: string | null): void => {
        if (!token) {
          this.checkLoginStatus();
          this.openModal('loginModal');
          return;
        }

        this.checkLoginStatus();

        const currentLanguage: string = this.getCurrentSupportedLanguage();
        this.router.navigate(['/', currentLanguage, 'profile'])
          .catch((error: unknown): void => console.error('Failed to navigate to current user profile:', error));
      });
  }

  protected isParksSectionActive(): boolean {
    const urlWithoutQuery: string = this.currentUrl().split('?')[0] ?? '';
    const segments: string[] = urlWithoutQuery.split('/').filter((segment: string) => segment.length > 0);
    const publicSection: string | undefined = segments[1];

    return publicSection === 'parks' || publicSection === 'park';
  }

  protected isSelectedLanguage(language: LanguageOption): boolean {
    return language.value === this.selectedLanguage();
  }

  protected getLanguageShortCode(language: LanguageOption): string {
    return language.value.toUpperCase();
  }

  protected flagAssetPath(language: string): string {
    return resolveFlagAssetPath(language);
  }

  protected onLanguageDialogVisibleChanged(visible: boolean): void {
    if (!visible) {
      this.closeModal('languageModal');
    }
  }

  protected selectLanguage(lang: string): void {
    this.translationService.useLang(lang)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (): void => {
          this.selectedLanguage.set(lang);
          this.updateUrlWithNewLang(lang);
          this.closeModal('languageModal');
        },
        error: (error: unknown): void => console.error('Error changing language:', error)
      });
  }

  protected measurementSystemShortLabel(): string {
    return this.preferredMeasurementSystem() === 'Imperial' ? 'ft' : 'm';
  }

  protected measurementToggleAriaLabelKey(): string {
    return this.preferredMeasurementSystem() === 'Imperial'
      ? 'measurementSystem.toggleToMetric'
      : 'measurementSystem.toggleToImperial';
  }

  protected toggleMeasurementSystem(): void {
    const nextSystem: MeasurementSystem = this.preferredMeasurementSystem() === 'Imperial' ? 'Metric' : 'Imperial';
    this.measurementPreferenceService.setPreferredSystem(nextSystem);
  }

  private updateUrlWithNewLang(newLang: string): void {
    const urlSegments: string[] = this.router.url.split('/');

    if (urlSegments.length > 1 && LANGUAGES.some((lang: LanguageOption) => lang.value === urlSegments[1])) {
      urlSegments[1] = newLang;
    } else {
      urlSegments.splice(1, 0, newLang);
    }

    this.router.navigateByUrl(urlSegments.join('/')).catch((error: unknown): void => console.error('Failed to navigate:', error));
  }

  private checkLoginStatus(): void {
    const isLoggedIn: boolean = this.authService.isLoggedIn();
    this.isLoggedIn.set(isLoggedIn);

    this.isAdmin.set(isLoggedIn && this.authService.hasRole('ADMIN'));

    if (!isLoggedIn) {
      this.userProfile.set(null);
      return;
    }

    const userId: string | null = this.authService.getUserIdFromToken();
    if (!userId) {
      this.userProfile.set(null);
      return;
    }

    this.authApiService.getCurrentUserById(userId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (user: UserDto): void => {
          this.userProfile.set(user);
          this.measurementPreferenceService.syncFromUser(user);
        },
        error: (): void => {
          this.userProfile.set(null);
        }
      });
  }

  private getLanguageFromUrl(): string {
    return resolveSupportedLanguageFromUrl(this.router.url, this.translationService.getCurrentLang() || 'en');
  }

  private getCurrentSupportedLanguage(): string {
    return resolveSupportedLanguage(this.selectedLanguage(), this.translationService.getCurrentLang() || 'en');
  }
}
