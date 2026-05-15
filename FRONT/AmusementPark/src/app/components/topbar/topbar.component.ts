import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router } from '@angular/router';
import { EMPTY, switchMap } from 'rxjs';
import { catchError, filter, tap } from 'rxjs/operators';

import { LANGUAGES, LanguageOption } from '@shared/models/localization';
import { UserDto } from '@app/models/users/user_dto';
import { ImagesApiService } from '@data-access/images/images-api.service';
import { AuthApiService } from '@data-access/auth/auth-api.service';
import { AuthService } from '@app/services/auth/auth.service';
import { ModalName, ModalService } from '@app/services/modal/modal.service';
import { SharedService } from '@app/services/shared/shared.service';
import { TranslationService } from '@app/services/translation.service';
import { TopbarViewComponent } from './topbar-view.component';

@Component({
  selector: 'app-topbar',
  templateUrl: './topbar.component.html',
  styleUrls: ['./topbar.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TopbarViewComponent]
})
export class TopbarComponent implements OnInit {
  protected readonly languages: readonly LanguageOption[] = LANGUAGES;
  protected readonly selectedLanguage = signal<string | undefined>(undefined);
  protected readonly displayLoginModal = signal(false);
  protected readonly displayLanguageModal = signal(false);
  protected readonly isLoggedIn = signal(false);
  protected readonly userProfile = signal<UserDto | null>(null);
  protected readonly userAvatarUrl: Signal<string | null> = computed(() => {
    return this.imagesApiService.resolveImageUrl(this.userProfile()?.avatarUrl);
  });

  constructor(
    private readonly imagesApiService: ImagesApiService,
    private readonly authApiService: AuthApiService,
    private readonly authService: AuthService,
    private readonly translationService: TranslationService,
    private readonly router: Router,
    private readonly modalService: ModalService,
    private readonly sharedService: SharedService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  ngOnInit(): void {
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
        const currentLang: string = this.router.url.split('/')[1] || 'en';
        this.selectedLanguage.set(currentLang);
      }),
      switchMap(() => this.translationService.useLang(this.selectedLanguage() ?? 'en').pipe(
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

  openModal(modalName: ModalName): void {
    this.modalService.openModal(modalName);
  }

  closeModal(modalName: ModalName): void {
    this.modalService.closeModal(modalName);
    if (modalName === 'loginModal') {
      this.checkLoginStatus();
    }
  }

  selectLanguage(lang: string): void {
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
        },
        error: (): void => {
          this.userProfile.set(null);
        }
      });
  }
}
