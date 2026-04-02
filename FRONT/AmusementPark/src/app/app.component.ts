import { Component, OnInit } from '@angular/core';
import { TranslationService } from './services/translation.service';
import { ActivatedRoute, Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { ApiService } from "./services/api.service";

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.scss'],
    standalone: false
})
export class AppComponent implements OnInit {
  title = "Amusement Parks";
  isLoading = true;
  showTopbar = true;  // Contrôle initial de la visibilité de la topbar

  constructor(
    private translationService: TranslationService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    const pathToExclude = ['/signin-google'];


    // Listen to navigation events to update the loading state and topbar visibility
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe((event: NavigationEnd) => {
      // Décider de montrer ou masquer la topbar
      this.isLoading = event.url.includes('/signin-google');

      // Mise à jour de l'état de chargement basé sur la langue
      const lang = this.route.root.firstChild?.snapshot.paramMap.get('lang');
      if (lang) {
        this.isLoading = true;
        this.translationService.useLang(lang).subscribe({
          next: () => {
            this.isLoading = false;  // Hide the loader once the language is loaded
          },
          error: (err) => {
            this.isLoading = false;  // Hide the loader even if there's an error
            console.error('Error loading language:', err);
          }
        });
      } else {
        this.isLoading = false;  // Hide the loader if no lang is found
      }
    });
  }
}
