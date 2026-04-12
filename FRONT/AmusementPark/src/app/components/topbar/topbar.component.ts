import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { filter } from 'rxjs/operators';

import { LANGUAGES } from '../../commons/languages';
import { UserDto } from '../../models/users/user_dto';
import { ImagesApiService } from '@data-access/images/images-api.service';
import { AuthApiService } from '@data-access/auth/auth-api.service';
import { AuthService } from '../../services/auth/auth.service';
import { ModalService } from '../../services/modal/modal.service';
import { SharedService } from '../../services/shared/shared.service';
import { TranslationService } from '../../services/translation.service';
import { TopbarViewComponent } from './topbar-view.component';

@Component({
  selector: 'app-topbar',
  templateUrl: './topbar.component.html',
  styleUrls: ['./topbar.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TopbarViewComponent]
})
export class TopbarComponent implements OnInit, OnDestroy {
  languages = LANGUAGES;
  selectedLanguage: string | undefined;
  displayLoginModal: boolean = false;
  displayLanguageModal: boolean = false;
  isLoggedIn: boolean = false;
  userProfile: UserDto | null = null;

  private readonly subscriptions: Subscription = new Subscription();

  constructor(
    private readonly imagesApiService: ImagesApiService,
    private readonly authApiService: AuthApiService,
    private readonly authService: AuthService,
    private readonly translationService: TranslationService,
    private readonly router: Router,
    private readonly modalService: ModalService,
    private readonly sharedService: SharedService,
    private readonly cdr: ChangeDetectorRef) {
  }

  get userAvatarUrl(): string | null {
    return this.imagesApiService.resolveImageUrl(this.userProfile?.avatarUrl);
  }

  ngOnInit(): void {
    this.checkLoginStatus();

    const loginModalStatus$ = this.modalService.getModalStatus('loginModal');
    if (loginModalStatus$) {
      this.subscriptions.add(
        loginModalStatus$.subscribe((status: boolean) => {
          this.displayLoginModal = status;
          this.cdr.markForCheck();
        })
      );
    }

    const languageModalStatus$ = this.modalService.getModalStatus('languageModal');
    if (languageModalStatus$) {
      this.subscriptions.add(
        languageModalStatus$.subscribe((status: boolean) => {
          this.displayLanguageModal = status;
          this.cdr.markForCheck();
        })
      );
    }

    this.subscriptions.add(
      this.router.events.pipe(filter((event: unknown) => event instanceof NavigationEnd)).subscribe(() => {
        const currentLang: string = this.router.url.split('/')[1] || 'en';
        this.selectedLanguage = currentLang;
        this.cdr.markForCheck();
        this.translationService.useLang(currentLang).subscribe({
          next: () => {
          },
          error: (err: unknown) => console.error('Error loading language:', err)
        });
      })
    );

    this.subscriptions.add(
      this.sharedService.getLoginStatusListener().subscribe(() => {
        this.checkLoginStatus();
      })
    );
  }

  openModal(modalName: string): void {
    this.modalService.openModal(modalName);
  }

  closeModal(modalName: string): void {
    this.modalService.closeModal(modalName);
    if (modalName === 'loginModal') {
      this.checkLoginStatus();
    }
  }

  selectLanguage(lang: string): void {
    this.translationService.useLang(lang).subscribe({
      next: () => {
        this.selectedLanguage = lang;
        this.updateUrlWithNewLang(lang);
        this.closeModal('languageModal');
        this.cdr.markForCheck();
      },
      error: (err: unknown) => console.error('Error changing language:', err)
    });
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  private updateUrlWithNewLang(newLang: string): void {
    const urlSegments: string[] = this.router.url.split('/');

    if (urlSegments.length > 1 && LANGUAGES.some((lang) => lang.value === urlSegments[1])) {
      urlSegments[1] = newLang;
    } else {
      urlSegments.splice(1, 0, newLang);
    }

    this.router.navigateByUrl(urlSegments.join('/')).catch((err: unknown) => console.error('Failed to navigate:', err));
  }

  private checkLoginStatus(): void {
    this.isLoggedIn = this.authService.isLoggedIn();
    if (!this.isLoggedIn) {
      this.userProfile = null;
      this.cdr.markForCheck();
      return;
    }

    const userId: string | null = this.authService.getUserIdFromToken();
    if (!userId) {
      this.userProfile = null;
      this.cdr.markForCheck();
      return;
    }

    this.subscriptions.add(
      this.authApiService.getCurrentUserById(userId).subscribe({
        next: (user: UserDto) => {
          this.userProfile = user;
          this.cdr.markForCheck();
        },
        error: () => {
          this.userProfile = null;
          this.cdr.markForCheck();
        }
      })
    );
  }
}
