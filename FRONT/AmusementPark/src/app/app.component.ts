import { Component, OnInit } from '@angular/core';
import { TranslationService } from './services/translation.service';
import { ActivatedRoute, Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.scss'],
    standalone: false
})
export class AppComponent implements OnInit {
  title = 'Amusement Parks';
  isLoading: boolean = true;
  showTopbar: boolean = true;

  constructor(
    private readonly translationService: TranslationService,
    private readonly route: ActivatedRoute,
    private readonly router: Router) {
  }

  ngOnInit(): void {
    this.router.events.pipe(
      filter((event: unknown) => event instanceof NavigationEnd)
    ).subscribe(() => {
      const lang: string | null | undefined = this.route.root.firstChild?.snapshot.paramMap.get('lang');
      if (lang) {
        this.isLoading = true;
        this.translationService.useLang(lang).subscribe({
          next: (): void => {
            this.isLoading = false;
          },
          error: (err: unknown): void => {
            this.isLoading = false;
            console.error('Error loading language:', err);
          }
        });
      } else {
        this.isLoading = false;
      }
    });
  }
}
