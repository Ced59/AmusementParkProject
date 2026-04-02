import { Component, OnDestroy, OnInit } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { filter } from 'rxjs/operators';

import { LANGUAGES } from '../../commons/languages';
import { UserDto } from '../../models/users/user_dto';
import { ApiService } from '../../services/api.service';
import { AuthService } from '../../services/auth/auth.service';
import { ModalService } from '../../services/modal/modal.service';
import { SharedService } from '../../services/shared/shared.service';
import { TranslationService } from '../../services/translation.service';

@Component({
  selector: 'app-topbar',
  templateUrl: './topbar.component.html',
  styleUrls: ['./topbar.component.scss'],
  standalone: false
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
    private readonly apiService: ApiService,
    private readonly authService: AuthService,
    private readonly translationService: TranslationService,
    private readonly router: Router,
    private readonly modalService: ModalService,
    private readonly sharedService: SharedService) {
  }

  ngOnInit(): void {
    this.checkLoginStatus();

    const loginModalStatus$ = this.modalService.getModalStatus('loginModal');
    if (loginModalStatus$) {
      this.subscriptions.add(
        loginModalStatus$.subscribe((status: boolean) => {
          this.displayLoginModal = status;
        })
      );
    } else {
      console.error('loginModal status observable is null');
    }

    const languageModalStatus$ = this.modalService.getModalStatus('languageModal');
    if (languageModalStatus$) {
      this.subscriptions.add(
        languageModalStatus$.subscribe((status: boolean) => {
          this.displayLanguageModal = status;
        })
      );
    } else {
      console.error('languageModal status observable is null');
    }

    this.subscriptions.add(
      this.router.events.pipe(filter(event => event instanceof NavigationEnd)).subscribe(() => {
        const currentLang: string = this.router.url.split('/')[1] || 'en';
        this.selectedLanguage = currentLang;
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
      },
      error: (err: unknown) => console.error('Error changing language:', err)
    });
  }

  getUserAvatarUrl(): string | null {
    return this.apiService.resolveImageUrl(this.userProfile?.avatarUrl);
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  private updateUrlWithNewLang(newLang: string): void {
    const urlSegments: string[] = this.router.url.split('/');

    if (urlSegments.length > 1 && LANGUAGES.some(lang => lang.value === urlSegments[1])) {
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
      return;
    }

    const userId: string | null = this.authService.getUserIdFromToken();
    if (!userId) {
      this.userProfile = null;
      return;
    }

    this.subscriptions.add(
      this.apiService.getUserById(userId).subscribe({
        next: (user: UserDto) => {
          this.userProfile = user;
        },
        error: () => {
          this.userProfile = null;
        }
      })
    );
  }
}
