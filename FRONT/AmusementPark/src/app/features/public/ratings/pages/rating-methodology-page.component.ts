import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, Signal, effect, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { RatingMethodology } from '@app/models/ratings/rating-methodology.models';
import { TranslationService } from '@app/services/translation.service';
import { CanonicalUrlService } from '@core/seo/canonical-url.service';
import { JsonLdService } from '@core/seo/json-ld.service';
import { SeoService } from '@core/seo/seo.service';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';
import { UiButtonDirective, UiSectionHeaderComponent } from '@ui/primitives';
import { RatingMethodologyStateFacade } from '../state/rating-methodology-state.facade';

@Component({
  selector: 'app-rating-methodology-page',
  templateUrl: './rating-methodology-page.component.html',
  styleUrl: './rating-methodology-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [RatingMethodologyStateFacade],
  imports: [DatePipe, RouterLink, TranslateModule, UiButtonDirective, UiSectionHeaderComponent]
})
export class RatingMethodologyPageComponent implements OnInit {
  protected readonly currentLang = signal<string>('en');
  protected readonly methodology: Signal<RatingMethodology | null> = this.stateFacade.methodology;
  protected readonly history: Signal<RatingMethodology[]> = this.stateFacade.history;
  protected readonly loading: Signal<boolean> = this.stateFacade.loading;
  protected readonly error: Signal<boolean> = this.stateFacade.error;
  protected readonly notFound: Signal<boolean> = this.stateFacade.notFound;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly stateFacade: RatingMethodologyStateFacade,
    private readonly translationService: TranslationService,
    private readonly translateService: TranslateService,
    private readonly seoService: SeoService,
    private readonly canonicalUrlService: CanonicalUrlService,
    private readonly jsonLdService: JsonLdService,
    private readonly destroyRef: DestroyRef
  ) {
    effect((): void => {
      if (this.notFound()) {
        this.seoService.applyNotFoundSeo(this.currentLang(), this.router.url);
      }
    });
  }

  ngOnInit(): void {
    const language: string = resolveLanguageFromActivatedRoute(
      this.route,
      this.translationService.getCurrentLang() || 'en'
    );
    this.currentLang.set(language);
    this.applySeoAndBreadcrumb();

    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params: ParamMap): void => {
      this.stateFacade.load(params.get('version'));
    });
    this.translationService.languageChanged.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((lang: string): void => {
      this.currentLang.set(lang);
      this.applySeoAndBreadcrumb();
    });
  }

  protected methodologyRoute(version: string | null = null): string[] {
    const base: string[] = ['/', this.currentLang(), 'rankings', 'methodology'];
    return version ? [...base, version] : base;
  }

  protected rankingRoute(): string[] {
    return ['/', this.currentLang(), 'rankings'];
  }

  protected contactRoute(): string[] {
    return ['/', this.currentLang(), 'contact'];
  }

  protected percent(value: number): number {
    return Math.round(value * 100);
  }

  protected formula(methodology: RatingMethodology): string {
    return `(x̄ × n + ${methodology.bayesian.priorMean} × ${methodology.bayesian.priorWeight}) ÷ (n + ${methodology.bayesian.priorWeight})`;
  }

  protected versionTranslationKey(version: string, field: 'summary' | 'reason' | 'effect'): string {
    return `ratings.methodology.versions.${version}.${field}`;
  }

  private applySeoAndBreadcrumb(): void {
    this.seoService.applyRouteDefaults(this.router.url);
    const language: string = this.currentLang();
    const homeUrl: string = this.canonicalUrlService.buildAbsoluteUrl(`/${language}/home`);
    const rankingsUrl: string = this.canonicalUrlService.buildAbsoluteUrl(`/${language}/rankings`);
    const methodologyUrl: string = this.canonicalUrlService.buildAbsoluteUrl(this.router.url);
    this.jsonLdService.replaceJsonLdByType('BreadcrumbList', {
      '@context': 'https://schema.org',
      '@type': 'BreadcrumbList',
      itemListElement: [
        {
          '@type': 'ListItem',
          position: 1,
          name: this.translateService.instant('ratings.methodology.breadcrumb.home'),
          item: homeUrl
        },
        {
          '@type': 'ListItem',
          position: 2,
          name: this.translateService.instant('ratings.methodology.breadcrumb.rankings'),
          item: rankingsUrl
        },
        {
          '@type': 'ListItem',
          position: 3,
          name: this.translateService.instant('ratings.methodology.breadcrumb.methodology'),
          item: methodologyUrl
        }
      ]
    });
  }
}
