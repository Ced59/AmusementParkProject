import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { skip } from 'rxjs/operators';

import { TranslationService } from '@app/services/translation.service';
import { SeoService } from '@core/seo/seo.service';
import { findNearestLanguageActivatedRoute, resolveLanguageFromActivatedRoute, resolveLanguageFromParamMap } from '@shared/utils/routing/route-language.utils';
import { UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { PublicSitemapStateFacade } from '../state/public-sitemap-state.facade';
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

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly translationService: TranslationService,
    private readonly seoService: SeoService,
    private readonly stateFacade: PublicSitemapStateFacade,
    private readonly destroyRef: DestroyRef
  ) {
  }

  ngOnInit(): void {
    this.location.set(resolvePublicSitemapLocation(this.route.snapshot.queryParamMap));
    this.applyLanguage(resolveLanguageFromActivatedRoute(this.route, this.translationService.getCurrentLang() || 'en'));
    this.watchRouteLanguageChanges();

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
    });
  }

  private applyLanguage(language: string): void {
    if (this.activeLanguage === language) {
      return;
    }

    this.activeLanguage = language;
    this.currentLang.set(language);
    this.loadPage();
  }

  private loadPage(): void {
    this.seoService.applyRouteDefaults(this.router.url);
    this.stateFacade.loadPage(this.currentLang(), this.location());
  }
}
