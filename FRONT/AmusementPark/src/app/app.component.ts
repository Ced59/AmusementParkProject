import { Component } from '@angular/core';
import { TranslationService } from './services/translation.service';
import { ActivatedRoute, Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent {
  title = "Amusement Parks";
  isLoading = true;

  constructor(
    private translationService: TranslationService,
    private route: ActivatedRoute,
    private router: Router
  ) {
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe(() => {
      const lang = this.route.root.firstChild?.snapshot.paramMap.get('lang');
      console.log("Current Lang:", lang);
      if (lang) {
        this.isLoading = true;
        this.translationService.useLang(lang).subscribe({
          next: () => {
            this.isLoading = false;  // Hide the loader once the language is loaded
          },
          error: (err) => {
            console.error('Error loading language:', err);
            this.isLoading = false;  // Hide the loader even if there's an error
          }
        });
      } else {
        this.isLoading = false;  // Hide the loader if no lang is found
      }
    });
  }
}
