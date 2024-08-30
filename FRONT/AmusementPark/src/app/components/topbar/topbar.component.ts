// Importations
import { Component, OnInit } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { LANGUAGES } from "../../commons/languages";
import { ApiService } from "../../services/api.service";
import {AuthService} from "../../services/auth/auth.service";
import {TranslationService} from "../../services/translation.service";

@Component({
  selector: 'app-topbar',
  templateUrl: './topbar.component.html',
  styleUrls: ['./topbar.component.scss']
})
export class TopbarComponent implements OnInit {
  languages = LANGUAGES;
  selectedLanguage: string | undefined;
  displayLoginModal: boolean = false;
  isLoggedIn: boolean = false;
  userEmail: string | undefined;

  constructor(
    private authService: AuthService,
    private translationService: TranslationService,
    private router: Router,
    private apiService: ApiService
  ) {}

  ngOnInit() {
    this.checkLoginStatus();
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe(() => {
      const currentLang = this.router.url.split('/')[1] || 'en'; // Assurez-vous que la structure de l'URL correspond à vos routes
      this.selectedLanguage = currentLang;
      this.translationService.useLang(currentLang).subscribe({
        next: () => {},
        error: (err) => console.error('Error loading language:', err)
      });
    });
  }

  checkLoginStatus() {
    this.isLoggedIn = this.authService.isLoggedIn();
    if (this.isLoggedIn) {
      const decodedToken = this.authService.getTokenDecoded();
      this.userEmail = decodedToken ? decodedToken.email : undefined;
    }
  }

  changeLanguage(lang: string) {
    this.translationService.useLang(lang).subscribe({
      next: () => {
        this.selectedLanguage = lang;
        this.router.navigate([lang, 'home']);
      },
      error: (err) => console.error('Error changing language:', err)
    });
  }

  openLoginModal() {
    this.displayLoginModal = true;
  }

  closeLoginModal() {
    this.displayLoginModal = false;
    this.checkLoginStatus();
  }
}
