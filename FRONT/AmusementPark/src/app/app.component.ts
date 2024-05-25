import { Component, OnInit } from '@angular/core';
import { TranslationService } from './services/translation.service';
import { ActivatedRoute, Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import {ApiService} from "./services/api.service";

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {
  title = "Amusement Parks";
  isLoading = true;

  constructor(
    private translationService: TranslationService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Listen to navigation events to update the loading state
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe(() => {
      const lang = this.route.root.firstChild?.snapshot.paramMap.get('lang');

      if (lang) {
        this.isLoading = true;
        this.translationService.useLang(lang).subscribe({
          next: () => {
            this.isLoading = false;  // Hide the loader once the language is loaded
          },
          error: (err) => {
            this.isLoading = false;  // Hide the loader even if there's an error
          }
        });
      } else {
        this.isLoading = false;  // Hide the loader if no lang is found
      }
    });
  }
}
