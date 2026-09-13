import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  Signal,
  computed,
  effect,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import {
  ProfileComparisonPark,
  ProfileComparisonRating,
  SharedProfileComparison,
} from '@app/models/sharing/profile-comparison.models';
import { TranslationService } from '@app/services/translation.service';
import { CanonicalUrlService } from '@core/seo/canonical-url.service';
import { SeoService } from '@core/seo/seo.service';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';
import { UiButtonDirective } from '@ui/primitives';
import { PublicShareReportComponent } from '@ui/sharing/public-share-report/public-share-report.component';
import { SharedProfileComparisonStateFacade } from '../state/shared-profile-comparison-state.facade';
import {
  resolveProfileComparisonCorrelationTranslationKey,
  resolveProfileComparisonMissedStatusTranslationKey,
} from './profile-comparison-view.helpers';

@Component({
  selector: 'app-shared-profile-comparison-page',
  templateUrl: './shared-profile-comparison-page.component.html',
  styleUrl: './shared-profile-comparison-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [SharedProfileComparisonStateFacade],
  imports: [TranslateModule, RouterLink, UiButtonDirective, PublicShareReportComponent],
})
export class SharedProfileComparisonPageComponent implements OnInit {
  protected readonly currentLang = signal<string>('en');
  protected readonly shareId = signal<string>('');
  protected readonly result: Signal<SharedProfileComparison | null> =
    this.facade.comparison;
  protected readonly loading: Signal<boolean> = this.facade.loading;
  protected readonly notFound: Signal<boolean> = this.facade.notFound;
  protected readonly error: Signal<boolean> = this.facade.error;
  protected readonly commonParks = computed(
    (): ProfileComparisonPark[] =>
      this.result()?.parks.filter(
        (park: ProfileComparisonPark): boolean =>
          park.creatorVisitCount != null && park.acceptorVisitCount != null,
      ) ?? [],
  );
  protected readonly creatorDiscoveries = computed(
    (): ProfileComparisonPark[] =>
      this.result()?.parks.filter(
        (park: ProfileComparisonPark): boolean =>
          park.creatorVisitCount != null && park.acceptorVisitCount == null,
      ) ?? [],
  );
  protected readonly acceptorDiscoveries = computed(
    (): ProfileComparisonPark[] =>
      this.result()?.parks.filter(
        (park: ProfileComparisonPark): boolean =>
          park.creatorVisitCount == null && park.acceptorVisitCount != null,
      ) ?? [],
  );
  protected readonly closeRatings = computed(
    (): ProfileComparisonRating[] =>
      this.result()?.ratings.filter(
        (rating: ProfileComparisonRating): boolean =>
          rating.affinity === 'Close',
      ) ?? [],
  );
  protected readonly neutralRatings = computed(
    (): ProfileComparisonRating[] =>
      this.result()?.ratings.filter(
        (rating: ProfileComparisonRating): boolean =>
          rating.affinity === 'Neutral',
      ) ?? [],
  );
  protected readonly divergentRatings = computed(
    (): ProfileComparisonRating[] =>
      this.result()?.ratings.filter(
        (rating: ProfileComparisonRating): boolean =>
          rating.affinity === 'Divergent',
      ) ?? [],
  );

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly facade: SharedProfileComparisonStateFacade,
    private readonly translationService: TranslationService,
    private readonly translateService: TranslateService,
    private readonly seoService: SeoService,
    private readonly canonicalUrlService: CanonicalUrlService,
    private readonly ssrHttpStatusService: SsrHttpStatusService,
    private readonly destroyRef: DestroyRef,
  ) {
    effect((): void => {
      const comparison: SharedProfileComparison | null = this.result();
      if (comparison) {
        this.applySeo(comparison);
      }
      if (this.notFound()) {
        this.ssrHttpStatusService.setNotFound();
        this.seoService.applyNotFoundSeo(this.currentLang(), this.router.url);
      }
    });
  }

  public ngOnInit(): void {
    const language: string = resolveLanguageFromActivatedRoute(
      this.route,
      this.translationService.getCurrentLang() || 'en',
    );
    const shareId: string =
      this.route.snapshot.paramMap.get('shareId')?.trim() ?? '';
    this.currentLang.set(language);
    this.shareId.set(shareId);
    this.seoService.applyRouteDefaults(this.router.url);
    if (!shareId) {
      this.ssrHttpStatusService.setNotFound();
      this.seoService.applyNotFoundSeo(language, this.router.url);
      return;
    }
    this.facade.load(shareId);
    this.translationService.languageChanged
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((lang: string): void => {
        this.currentLang.set(lang);
        const comparison: SharedProfileComparison | null = this.result();
        if (comparison) {
          this.applySeo(comparison);
        }
      });
  }

  protected displayName(value: string | null): string {
    return (
      value || this.translateService.instant('profileComparison.result.member')
    );
  }

  protected formatRating(value: number): string {
    return new Intl.NumberFormat(this.currentLang(), {
      minimumFractionDigits: 1,
      maximumFractionDigits: 1,
    }).format(value);
  }

  protected countryName(countryCode: string | null): string {
    if (!countryCode) {
      return '';
    }
    try {
      return (
        new Intl.DisplayNames([this.currentLang()], { type: 'region' }).of(
          countryCode,
        ) ?? countryCode
      );
    } catch {
      return countryCode;
    }
  }

  protected correlationKey(
    value: number | null,
    commonRatingCount: number,
    minimumRatingsForCorrelation: number,
  ): string {
    return resolveProfileComparisonCorrelationTranslationKey(
      value,
      commonRatingCount,
      minimumRatingsForCorrelation,
    );
  }

  protected ratingWidth(value: number): string {
    return `${Math.max(0, Math.min(100, value * 20))}%`;
  }

  protected missedStatusKey(status: string): string {
    return resolveProfileComparisonMissedStatusTranslationKey(status);
  }

  private applySeo(comparison: SharedProfileComparison): void {
    const creator: string = this.displayName(comparison.creatorDisplayName);
    const acceptor: string = this.displayName(comparison.acceptorDisplayName);
    const params: Record<string, string> = { creator, acceptor };
    const title: string = this.translateService.instant(
      'profileComparison.result.seoTitle',
      params,
    );
    const description: string = this.translateService.instant(
      'profileComparison.result.seoDescription',
      params,
    );
    const currentUrl: string =
      this.canonicalUrlService.buildCanonicalFromCurrentUrl(this.router.url);
    const homePath: string = `/${this.currentLang()}/home`;
    this.seoService.applySharedVisitRecapSeo(
      title,
      description,
      this.router.url,
      '/assets/general-icon/logo-amusementpark.png',
      title,
      [
        {
          '@context': 'https://schema.org',
          '@type': 'BreadcrumbList',
          itemListElement: [
            {
              '@type': 'ListItem',
              position: 1,
              name: this.translateService.instant(
                'profileComparison.result.home',
              ),
              item: this.canonicalUrlService.buildAbsoluteUrl(homePath),
            },
            {
              '@type': 'ListItem',
              position: 2,
              name: this.translateService.instant(
                'profileComparison.result.breadcrumb',
              ),
              item: currentUrl,
            },
          ],
        },
      ],
    );
  }
}
