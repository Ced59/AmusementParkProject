import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, Signal, effect, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { SharedYearRecap } from '@app/models/sharing/share-publication.models';
import { TranslationService } from '@app/services/translation.service';
import { CanonicalUrlService } from '@core/seo/canonical-url.service';
import { SeoService } from '@core/seo/seo.service';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';
import { PublicSharePanelComponent } from '@ui/sharing/public-share-panel/public-share-panel.component';
import { UiButtonDirective } from '@ui/primitives';
import { SharedYearRecapStateFacade } from '../state/shared-year-recap-state.facade';

@Component({
  selector: 'app-shared-year-recap-page',
  templateUrl: './shared-year-recap-page.component.html',
  styleUrl: './shared-year-recap-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [SharedYearRecapStateFacade],
  imports: [PublicSharePanelComponent, RouterLink, TranslateModule, UiButtonDirective]
})
export class SharedYearRecapPageComponent implements OnInit {
  protected readonly currentLang = signal<string>('en');
  protected readonly shareId = signal<string>('');
  protected readonly result: Signal<SharedYearRecap | null> = this.facade.recap;
  protected readonly loading: Signal<boolean> = this.facade.loading;
  protected readonly notFound: Signal<boolean> = this.facade.notFound;
  protected readonly error: Signal<boolean> = this.facade.error;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly facade: SharedYearRecapStateFacade,
    private readonly translationService: TranslationService,
    private readonly translateService: TranslateService,
    private readonly seoService: SeoService,
    private readonly canonicalUrlService: CanonicalUrlService,
    private readonly ssrHttpStatusService: SsrHttpStatusService,
    private readonly destroyRef: DestroyRef
  ) {
    effect((): void => {
      const shared: SharedYearRecap | null = this.result();
      if (shared) {
        this.applySeo(shared.yearRecap.year);
      }
      if (this.notFound()) {
        this.ssrHttpStatusService.setNotFound();
        this.seoService.applyNotFoundSeo(this.currentLang(), this.router.url);
      }
    });
  }

  ngOnInit(): void {
    const language: string = resolveLanguageFromActivatedRoute(
      this.route,
      this.translationService.getCurrentLang() || 'en'
    );
    const shareId: string = this.route.snapshot.paramMap.get('shareId')?.trim() ?? '';
    this.currentLang.set(language);
    this.shareId.set(shareId);
    this.seoService.applyRouteDefaults(this.router.url);
    if (shareId.length === 0) {
      this.ssrHttpStatusService.setNotFound();
      this.seoService.applyNotFoundSeo(language, this.router.url);
      return;
    }
    this.facade.load(shareId);
    this.translationService.languageChanged.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((lang: string): void => {
      this.currentLang.set(lang);
      const shared: SharedYearRecap | null = this.result();
      if (shared) {
        this.applySeo(shared.yearRecap.year);
      }
    });
  }

  protected formatRating(value: number | null | undefined): string {
    return value == null ? '–' : new Intl.NumberFormat(this.currentLang(), {
      minimumFractionDigits: 1,
      maximumFractionDigits: 1
    }).format(value);
  }

  protected formatPercent(value: number): string {
    return new Intl.NumberFormat(this.currentLang(), {
      style: 'percent',
      maximumFractionDigits: 0
    }).format(value);
  }

  protected categoryKey(category: string): string {
    return `ratings.categories.${category}`;
  }

  private applySeo(year: number): void {
    const params: Record<string, number> = { year };
    const title: string = this.translateService.instant('yearRecapShare.public.seoTitle', params);
    const description: string = this.translateService.instant('yearRecapShare.public.seoDescription', params);
    const currentUrl: string = this.canonicalUrlService.buildCanonicalFromCurrentUrl(this.router.url);
    const homePath: string = `/${this.currentLang()}/home`;
    const breadcrumbs: unknown[] = [{
      '@context': 'https://schema.org',
      '@type': 'BreadcrumbList',
      itemListElement: [
        {
          '@type': 'ListItem',
          position: 1,
          name: this.translateService.instant('yearRecapShare.public.breadcrumbHome'),
          item: this.canonicalUrlService.buildAbsoluteUrl(homePath)
        },
        {
          '@type': 'ListItem',
          position: 2,
          name: this.translateService.instant('yearRecapShare.public.breadcrumbRecap', params),
          item: currentUrl
        }
      ]
    }];
    this.seoService.applySharedVisitRecapSeo(title, description, this.router.url, title, breadcrumbs);
  }
}
