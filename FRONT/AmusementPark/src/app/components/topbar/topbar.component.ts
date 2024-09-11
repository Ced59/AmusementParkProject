import {Component, OnDestroy, OnInit} from '@angular/core';
import {NavigationEnd, Router} from '@angular/router';
import {filter} from 'rxjs/operators';
import {LANGUAGES} from "../../commons/languages";
import {ApiService} from "../../services/api.service";
import {AuthService} from "../../services/auth/auth.service";
import {TranslationService} from "../../services/translation.service";
import {JwtPayload} from "../../models/users/jwt_payload";
import {ModalService} from "../../services/modal/modal.service";
import {Subscription} from "rxjs";
import {SharedService} from "../../services/shared/shared.service";

@Component({
  selector: 'app-topbar',
  templateUrl: './topbar.component.html',
  styleUrls: ['./topbar.component.scss']
})
export class TopbarComponent implements OnInit, OnDestroy {
  languages = LANGUAGES;
  selectedLanguage: string | undefined;
  displayLoginModal: boolean = false;
  displayLanguageModal: boolean = false;
  isLoggedIn: boolean = false;
  userProfile: JwtPayload | null = null;
  private subscriptions: Subscription = new Subscription();


  constructor(
    private authService: AuthService,
    private translationService: TranslationService,
    private router: Router,
    private modalService: ModalService,
    private sharedService: SharedService
  ) {}

  ngOnInit() {
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
        const currentLang = this.router.url.split('/')[1] || 'en';
        this.selectedLanguage = currentLang;
        this.translationService.useLang(currentLang).subscribe({
          next: () => {},
          error: (err) => console.error('Error loading language:', err)
        });
      })
    );

    this.subscriptions.add(
      this.sharedService.getLoginStatusListener().subscribe(() => {
        this.checkLoginStatus();
      })
    );
  }

  openModal(modalName: string) {
    this.modalService.openModal(modalName);
  }

  closeModal(modalName: string) {
    this.modalService.closeModal(modalName);
    if (modalName === 'loginModal') {
      this.checkLoginStatus();
    }
  }

  selectLanguage(lang: string) {
    this.translationService.useLang(lang).subscribe({
      next: () => {
        this.selectedLanguage = lang;
        this.updateUrlWithNewLang(lang);
        this.closeModal('languageModal');
      },
      error: (err) => console.error('Error changing language:', err)
    });
  }

  private updateUrlWithNewLang(newLang: string): void {

    const urlSegments = this.router.url.split('/');

    if (urlSegments.length > 1 && LANGUAGES.some(lang => lang.value === urlSegments[1])) {
      urlSegments[1] = newLang;
    } else {
      urlSegments.splice(1, 0, newLang);
    }

    this.router.navigateByUrl(urlSegments.join('/')).catch(err => console.error('Failed to navigate:', err));
  }

  ngOnDestroy() {
    this.subscriptions.unsubscribe();
  }


  checkLoginStatus() {
    this.isLoggedIn = this.authService.isLoggedIn();
    if (this.isLoggedIn) {
      this.userProfile = this.authService.getTokenDecoded();
    }
  }
}
