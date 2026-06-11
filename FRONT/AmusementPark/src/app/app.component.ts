import { Component, DestroyRef, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { EMPTY, Observable, switchMap } from 'rxjs';
import { catchError, filter, tap } from 'rxjs/operators';

import { TranslationService } from '@app/services/translation.service';
import { Bind } from 'primeng/bind';
import { Toast } from 'primeng/toast';
import { CookieConsentBannerComponent } from '@ui/layouts/cookie-consent-banner/cookie-consent-banner.component';
import { SeoService } from '@core/seo/seo.service';
import { resolveLanguageFromActivatedRoute, resolveLanguageFromUrl } from '@shared/utils/routing/route-language.utils';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
  imports: [RouterOutlet, Bind, Toast, CookieConsentBannerComponent]
})
export class AppComponent implements OnInit {
  title: string = 'Amusement Parks';
  isLoading: boolean = true;

  constructor(
    private readonly translationService: TranslationService,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly seoService: SeoService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  ngOnInit(): void {
    this.applyLanguageForCurrentRoute(this.router.url).pipe(takeUntilDestroyed(this.destroyRef)).subscribe();

    this.router.events.pipe(
      filter((event: unknown): event is NavigationEnd => event instanceof NavigationEnd),
      tap((event: NavigationEnd): void => {
        this.isLoading = true;
        this.seoService.applyRouteDefaults(event.urlAfterRedirects);
      }),
      switchMap((event: NavigationEnd): Observable<unknown> => this.applyLanguageForCurrentRoute(event.urlAfterRedirects)),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe();
  }

  private applyLanguageForCurrentRoute(url: string): Observable<unknown> {
    const routeLanguage: string = resolveLanguageFromActivatedRoute(
      this.route,
      resolveLanguageFromUrl(url, this.translationService.getCurrentLang() || 'en')
    );

    return this.translationService.useLang(routeLanguage).pipe(
      tap((): void => {
        this.isLoading = false;
        this.seoService.setHtmlLanguage(routeLanguage);
      }),
      catchError((error: unknown) => {
        this.isLoading = false;
        console.error('Error loading language:', error);
        return EMPTY;
      })
    );
  }
}
