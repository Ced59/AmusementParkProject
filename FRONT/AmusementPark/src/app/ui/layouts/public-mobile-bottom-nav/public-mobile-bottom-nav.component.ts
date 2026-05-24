import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { filter } from 'rxjs/operators';

import { TranslateModule } from '@ngx-translate/core';

import { AuthService } from '@app/services/auth/auth.service';
import { SharedService } from '@app/services/shared/shared.service';
import { ModalService } from '@app/services/modal/modal.service';
import { TranslationService } from '@app/services/translation.service';
import { resolveSupportedLanguage, resolveSupportedLanguageFromUrl } from '@shared/utils/routing/localized-route.helpers';

@Component({
  selector: 'app-public-mobile-bottom-nav',
  templateUrl: './public-mobile-bottom-nav.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, RouterLinkActive, TranslateModule]
})
export class PublicMobileBottomNavComponent implements OnInit {
  protected readonly currentLang = signal<string>('en');
  protected readonly currentUrl = signal<string>('');
  protected readonly isLoggedIn = signal<boolean>(false);
  protected readonly isAdmin = signal<boolean>(false);

  constructor(
    private readonly authService: AuthService,
    private readonly sharedService: SharedService,
    private readonly modalService: ModalService,
    private readonly translationService: TranslationService,
    private readonly router: Router,
    private readonly destroyRef: DestroyRef
  ) {
  }

  ngOnInit(): void {
    this.currentLang.set(this.getLanguageFromUrl());
    this.currentUrl.set(this.router.url);
    this.checkAuthStatus();

    this.translationService.languageChanged
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((lang: string): void => {
        this.currentLang.set(lang);
      });

    this.router.events.pipe(
      filter((event: unknown): event is NavigationEnd => event instanceof NavigationEnd),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe((): void => {
      this.currentLang.set(this.getLanguageFromUrl());
      this.currentUrl.set(this.router.url);
    });

    this.sharedService.getLoginStatusListener()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((): void => {
        this.checkAuthStatus();
      });
  }


  protected navigateToCurrentUserProfile(): void {
    this.authService.ensureValidAccessToken(true)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((token: string | null): void => {
        if (!token) {
          this.checkAuthStatus();
          this.modalService.openModal('loginModal');
          return;
        }

        this.checkAuthStatus();

        const currentLanguage: string = this.getCurrentSupportedLanguage();
        this.router.navigate(['/', currentLanguage, 'profile'])
          .catch((error: unknown): void => console.error('Failed to navigate to current user profile:', error));
      });
  }

  protected isProfileSectionActive(): boolean {
    const urlWithoutQuery: string = this.currentUrl().split('?')[0] ?? '';
    const segments: string[] = urlWithoutQuery.split('/').filter((segment: string) => segment.length > 0);
    const publicSection: string | undefined = segments[1];

    return publicSection === 'profile';
  }

  protected isParksSectionActive(): boolean {
    const urlWithoutQuery: string = this.currentUrl().split('?')[0] ?? '';
    const segments: string[] = urlWithoutQuery.split('/').filter((segment: string) => segment.length > 0);
    const publicSection: string | undefined = segments[1];

    return publicSection === 'parks' || publicSection === 'park';
  }

  private checkAuthStatus(): void {
    const isLoggedIn: boolean = this.authService.isLoggedIn();

    this.isLoggedIn.set(isLoggedIn);
    this.isAdmin.set(isLoggedIn && this.authService.hasRole('ADMIN'));
  }

  private getLanguageFromUrl(): string {
    return resolveSupportedLanguageFromUrl(this.router.url, this.translationService.getCurrentLang() || 'en');
  }

  private getCurrentSupportedLanguage(): string {
    return resolveSupportedLanguage(this.currentLang(), this.translationService.getCurrentLang() || 'en');
  }
}
