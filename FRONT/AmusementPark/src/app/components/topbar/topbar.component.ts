import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs/operators';
import { Subscription } from 'rxjs';
import { LANGUAGES } from '../../commons/languages';
import { AuthService } from '../../services/auth/auth.service';
import { TranslationService } from '../../services/translation.service';
import { ModalService } from '../../services/modal/modal.service';
import { SharedService } from '../../services/shared/shared.service';
import { environment } from '../../../environments/environment';
import { CurrentUserService } from '../../services/users/current-user.service';

@Component({
  selector: 'app-topbar',
  templateUrl: './topbar.component.html',
  styleUrls: ['./topbar.component.scss']
})
export class TopbarComponent implements OnInit, OnDestroy {
  readonly languages = LANGUAGES;
  readonly isLoggedIn = signal<boolean>(false);
  readonly currentUser = this.currentUserService.currentUser;

  selectedLanguage: string | undefined;
  displayLoginModal = false;
  displayLanguageModal = false;

  private readonly subscriptions = new Subscription();

  constructor(
    private readonly authService: AuthService,
    private readonly currentUserService: CurrentUserService,
    private readonly translationService: TranslationService,
    private readonly router: Router,
    private readonly modalService: ModalService,
    private readonly sharedService: SharedService
  ) {
  }

  ngOnInit(): void {
    this.syncAuthState();

    const loginModalStatus$ = this.modalService.getModalStatus('loginModal');
    if (loginModalStatus$) {
      this.subscriptions.add(
        loginModalStatus$.subscribe((status: boolean) => {
          this.displayLoginModal = status;
        })
      );
    }

    const languageModalStatus$ = this.modalService.getModalStatus('languageModal');
    if (languageModalStatus$) {
      this.subscriptions.add(
        languageModalStatus$.subscribe((status: boolean) => {
          this.displayLanguageModal = status;
        })
      );
    }

    this.subscriptions.add(
      this.router.events.pipe(filter((event) => event instanceof NavigationEnd)).subscribe(() => {
        const currentLang = this.router.url.split('/')[1] || 'en';
        this.selectedLanguage = currentLang;
        this.translationService.useLang(currentLang).subscribe({
          next: () => {},
          error: (err: unknown) => console.error('Error loading language:', err)
        });
      })
    );

    this.subscriptions.add(
      this.sharedService.getLoginStatusListener().subscribe(() => {
        this.syncAuthState();
      })
    );
  }

  openModal(modalName: string): void {
    this.modalService.openModal(modalName);
  }

  closeModal(modalName: string): void {
    this.modalService.closeModal(modalName);

    if (modalName === 'loginModal') {
      this.syncAuthState();
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

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  getProfileImageUrl(): string | null {
    const avatarUrl = this.currentUser()?.avatarUrl;

    if (!avatarUrl) {
      return null;
    }

    return avatarUrl.startsWith('http') ? avatarUrl : `${environment.apiBaseUrl}${avatarUrl}`;
  }

  private syncAuthState(): void {
    const isLoggedIn = this.authService.isLoggedIn();
    this.isLoggedIn.set(isLoggedIn);

    if (isLoggedIn) {
      this.currentUserService.refreshCurrentUser();
      return;
    }

    this.currentUserService.clearCurrentUser();
  }

  private updateUrlWithNewLang(newLang: string): void {
    const urlSegments = this.router.url.split('/');

    if (urlSegments.length > 1 && LANGUAGES.some((lang) => lang.value === urlSegments[1])) {
      urlSegments[1] = newLang;
    } else {
      urlSegments.splice(1, 0, newLang);
    }

    this.router.navigateByUrl(urlSegments.join('/')).catch((err: unknown) => console.error('Failed to navigate:', err));
  }
}
