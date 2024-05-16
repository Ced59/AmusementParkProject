import { Component, OnInit } from '@angular/core';
import { TranslationService } from '../../services/translation.service';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';

@Component({
  selector: 'app-topbar',
  templateUrl: './topbar.component.html',
  styleUrls: ['./topbar.component.scss']
})
export class TopbarComponent implements OnInit {
  languages = [
    { label: 'English', value: 'en' },
    { label: 'French', value: 'fr' },
    { label: 'Spanish', value: 'es' },
    { label: 'German', value: 'de' }
  ];
  selectedLanguage: string | undefined;

  constructor(
    private translationService: TranslationService,
    private router: Router
  ) {}

  ngOnInit() {
    // Écouter les événements de navigation pour mettre à jour la langue sélectionnée
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

  changeLanguage(lang: string) {
    this.translationService.useLang(lang).subscribe({
      next: () => {
        this.selectedLanguage = lang;
        // Mettre à jour l'URL
        this.router.navigate([lang, 'home']);
      },
      error: (err) => console.error('Error changing language:', err)
    });
  }
}
