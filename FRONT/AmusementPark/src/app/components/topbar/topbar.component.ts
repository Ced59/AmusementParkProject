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
    private apiService: ApiService,
    private modalService: ModalService
  ) {}

  ngOnInit() {
    this.checkLoginStatus();

    // Abonnement pour contrôler l'affichage de la modal de connexion
    this.subscriptions.add(
      this.modalService.displayLoginModal$.subscribe(show => {
        this.displayLoginModal = show;
      })
    );

    // Abonnement aux changements de route pour mettre à jour la langue sélectionnée
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
  }


  checkLoginStatus() {
    this.isLoggedIn = this.authService.isLoggedIn();
    if (this.isLoggedIn) {
      this.userProfile = this.authService.getTokenDecoded();
    }
  }

  openLoginModal() {
    this.modalService.openLoginModal();
  }

  closeLoginModal() {
    this.modalService.closeLoginModal();
    this.checkLoginStatus();
  }

  openLanguageModal() {
    this.displayLanguageModal = true;
  }

  selectLanguage(lang: string) {
    this.translationService.useLang(lang).subscribe({
      next: () => {
        this.selectedLanguage = lang;
        this.router.navigate([lang, 'home']);
        this.displayLanguageModal = false;
      },
      error: (err) => console.error('Error changing language:', err)
    });
  }

  ngOnDestroy() {
    this.subscriptions.unsubscribe();
  }
}
