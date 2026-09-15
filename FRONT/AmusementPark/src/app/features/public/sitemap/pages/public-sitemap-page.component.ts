import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, effect, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, NavigationEnd, ParamMap, Router, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { filter, skip } from 'rxjs/operators';

import { TranslationService } from '@app/services/translation.service';
import { SeoService } from '@core/seo/seo.service';
import { findNearestLanguageActivatedRoute, resolveLanguageFromActivatedRoute, resolveLanguageFromParamMap } from '@shared/utils/routing/route-language.utils';
import { UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { PublicSitemapResolvedPage, PublicSitemapStateFacade } from '../state/public-sitemap-state.facade';
import { PublicSitemapLocation, buildPublicSitemapQuery, resolvePublicSitemapLocation } from '../state/public-sitemap-location';

@Component({
  selector: 'app-public-sitemap-page',
  templateUrl: './public-sitemap-page.component.html',
  styleUrls: ['./public-sitemap-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [PublicSitemapStateFacade],
  imports: [CommonModule, RouterLink, TranslateModule, UiKickerComponent, UiSurfaceDirective]
})
export class PublicSitemapPageComponent implements OnInit {
  protected readonly currentLang = signal<string>('en');
  protected readonly nodes = this.stateFacade.nodes;
  protected readonly breadcrumbs = this.stateFacade.breadcrumbs;
  protected readonly loading = this.stateFacade.loading;
  protected readonly errorKey = this.stateFacade.errorKey;
  protected readonly page = this.stateFacade.page;
  protected readonly pageCount = this.stateFacade.pageCount;
  private readonly location = signal<PublicSitemapLocation>({ nodeIds: [], page: 1, isValid: true });
  protected readonly previousQueryParams = computed(() => buildPublicSitemapQuery(this.location().nodeIds, this.page() - 1));
  protected readonly nextQueryParams = computed(() => buildPublicSitemapQuery(this.location().nodeIds, this.page() + 1));
  private activeLanguage: string | null = null;
  private snapshotLanguageResetPending = false;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly translationService: TranslationService,
    private readonly seoService: SeoService,
    private readonly stateFacade: PublicSitemapStateFacade,
    private readonly destroyRef: DestroyRef
  ) {
    effect(() => this.applyResolvedPageSeo());
  }

  ngOnInit(): void {
    this.location.set(resolvePublicSitemapLocation(this.route.snapshot.queryParamMap));
    this.applyLanguage(resolveLanguageFromActivatedRoute(this.route, this.translationService.getCurrentLang() || 'en'));
    this.watchRouteLanguageChanges();
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(() => this.applyResolvedPageSeo());

    this.route.queryParamMap.pipe(skip(1), takeUntilDestroyed(this.destroyRef)).subscribe((params: ParamMap): void => {
      this.location.set(resolvePublicSitemapLocation(params));
      this.loadPage();
    });
    this.translationService.languageChanged.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((language: string): void => {
      this.applyLanguage(language);
    });
  }

  private watchRouteLanguageChanges(): void {
    const languageRoute: ActivatedRoute | null = findNearestLanguageActivatedRoute(this.route);
    languageRoute?.paramMap.pipe(skip(1), takeUntilDestroyed(this.destroyRef)).subscribe((params: ParamMap): void => {
      this.applyLanguage(resolveLanguageFromParamMap(params, this.currentLang()));
      if (this.snapshotLanguageResetPending) {
        this.snapshotLanguageResetPending = false;
        void this.router.navigate([], {
          relativeTo: this.route,
          queryParams: buildPublicSitemapQuery(this.location().nodeIds),
          replaceUrl: true
        });
      }
    });
  }

  private applyLanguage(language: string): void {
    if (this.activeLanguage === language) {
      return;
    }

    if (this.activeLanguage !== null && this.location().isValid && this.location().nodeIds[0] === 'snapshot-sections') {
      this.location.set({ nodeIds: ['snapshot-sections'], page: 1, isValid: true });
      // The header emits its language change before navigating to the localized URL.
      this.snapshotLanguageResetPending = true;
    }

    this.activeLanguage = language;
    this.currentLang.set(language);
    this.loadPage();
  }

  private loadPage(): void {
    this.seoService.applyRouteDefaults(this.router.url);
    this.stateFacade.loadPage(this.currentLang(), this.location());
  }

  private applyResolvedPageSeo(): void {
    const resolved: PublicSitemapResolvedPage | null = this.stateFacade.resolvedPage();
    const language: string = this.currentLang();
    const path: string = this.router.url.split(/[?#]/)[0];
    if (path !== `/${language}/sitemap` && path !== `/${language}/sitemap/`) {
      return;
    }

    const actual: PublicSitemapLocation = resolvePublicSitemapLocation(this.router.parseUrl(this.router.url).queryParamMap);
    if (!resolved || !actual.isValid || resolved.language !== language
      || actual.page !== resolved.location.page || actual.nodeIds.join('/') !== resolved.location.nodeIds.join('/')) {
      this.seoService.applyRouteDefaults(this.router.url);
      return;
    }

    // AppComponent applies defaults on NavigationEnd, including with synchronous SSR TransferState.
    this.seoService.applyPublicSitemapSeo(language, actual.nodeIds, actual.page, resolved.breadcrumbLabels);
  }
}
