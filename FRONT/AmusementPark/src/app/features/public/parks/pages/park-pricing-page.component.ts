import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { TranslateModule, TranslateService as NgxTranslateService } from '@ngx-translate/core';
import { catchError, distinctUntilChanged, EMPTY, map, Observable, of, switchMap, throwError } from 'rxjs';

import {
  ParkPriceValue,
  ParkPricing
} from '@app/models/parks/park-pricing';
import { Park } from '@app/models/parks/park';
import { ParkDetailSummary } from '@app/models/parks/park-detail-summary';
import { LocalizedItem } from '@app/models/shared/localized-item';
import { TranslationService } from '@app/services/translation.service';
import { SeoService } from '@core/seo/seo.service';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { hasHttpStatus } from '@core/http/http-error-status.helpers';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { applySsrPublicDataErrorStatus } from '@core/ssr/ssr-public-error-status';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { SafeExternalUrlPipe } from '@shared/pipes';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { resolveParkSummarySocialImageId } from '@shared/utils/images/park-social-image.helpers';
import { isParkOpenToVisitors } from '@shared/utils/parks/park-status.presentation';
import {
  buildPublicParkPricingRouteCommands,
  buildPublicParkRouteCommands,
  buildPublicRoutePath
} from '@shared/utils/routing/public-detail-route.helpers';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';
import { UiButtonDirective } from '@ui/primitives';
import {
  formatParkPrice,
  ParkPriceFormattingLabels,
  resolvePricingLocalizedText
} from '../models/park-pricing.presentation';
import { ParkLifecycleNoticeComponent } from '../ui/park-lifecycle-notice.component';

interface ParkPricingPageData {
  park: Park;
  parkImageId: string | null;
  pricing: ParkPricing | null;
}

@Component({
  selector: 'app-park-pricing-page',
  templateUrl: './park-pricing-page.component.html',
  styleUrls: ['./park-pricing-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PageStateComponent,
    ParkLifecycleNoticeComponent,
    RouterLink,
    SafeExternalUrlPipe,
    TranslateModule,
    UiButtonDirective
  ]
})
export class ParkPricingPageComponent implements OnInit {
  private readonly stateStore = new SignalScreenStateStore<ParkPricingPageData>();

  protected readonly state = this.stateStore.state;
  protected readonly currentLanguage = signal<string>('en');
  protected readonly detailLink = signal<string[] | null>(null);
  protected readonly unavailablePark = signal<Park | null>(null);
  private readonly unavailableParkImageId = signal<string | null>(null);

  private readonly destroyRef: DestroyRef = inject(DestroyRef);

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly translationService: TranslationService,
    private readonly ngxTranslateService: NgxTranslateService,
    private readonly parksApiService: ParksApiService,
    private readonly seoService: SeoService,
    private readonly ssrHttpStatusService: SsrHttpStatusService
  ) {
    effect((): void => {
      const language: string = this.currentLanguage();
      const unavailablePark: Park | null = this.unavailablePark();
      if (unavailablePark) {
        const routeTarget = {
          language,
          parkId: unavailablePark.id,
          parkName: unavailablePark.name
        };

        this.detailLink.set(buildPublicParkRouteCommands(routeTarget));
        this.seoService.applyParkUnavailableFeatureSeo(
          unavailablePark,
          'pricing',
          language,
          this.router.url,
          this.unavailableParkImageId(),
          buildPublicRoutePath(buildPublicParkRouteCommands(routeTarget)));
        return;
      }

      const data: ParkPricingPageData | undefined = this.stateStore.data();
      if (!data) {
        return;
      }

      const routeTarget = {
        language,
        parkId: data.park.id,
        parkName: data.park.name
      };
      const offerCount: number = data.pricing
        ? data.pricing.admissionOffers.length + data.pricing.annualPasses.length + data.pricing.parkingOffers.length
        : 0;

      this.detailLink.set(buildPublicParkRouteCommands(routeTarget));
      this.seoService.applyParkPricingSeo(
        data.park.name ?? '',
        language,
        this.router.url,
        offerCount,
        data.parkImageId,
        buildPublicRoutePath(buildPublicParkPricingRouteCommands(routeTarget)));
    });
  }

  ngOnInit(): void {
    const initialLanguage: string = resolveLanguageFromActivatedRoute(
      this.route,
      this.translationService.getCurrentLang() || 'en');
    this.currentLanguage.set(initialLanguage);

    this.translationService.languageChanged
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((language: string): void => {
        this.currentLanguage.set(language);
      });

    this.route.paramMap
      .pipe(
        map((params: ParamMap): string | null => params.get('id')),
        distinctUntilChanged(),
        switchMap((parkId: string | null): Observable<ParkPricingPageData> =>
          parkId ? this.loadPricingPage(parkId) : EMPTY),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((data: ParkPricingPageData): void => {
        const offerCount: number = data.pricing
          ? data.pricing.admissionOffers.length + data.pricing.annualPasses.length + data.pricing.parkingOffers.length
          : 0;

        if (offerCount === 0) {
          this.ssrHttpStatusService.setNotFound();
          this.stateStore.setEmpty(data);
          return;
        }

        this.stateStore.setReady(data);
      });
  }

  protected localizedText(
    values: readonly LocalizedItem<string>[] | null | undefined,
    fallback: string = ''
  ): string {
    return resolvePricingLocalizedText(values, this.currentLanguage(), fallback);
  }

  protected formatPrice(value: ParkPriceValue | null | undefined, currencyCode: string): string | null {
    const labels: ParkPriceFormattingLabels = {
      from: this.ngxTranslateService.instant('parkPricing.price.from'),
      upTo: this.ngxTranslateService.instant('parkPricing.price.upTo'),
      dynamic: this.ngxTranslateService.instant('parkPricing.price.dynamic')
    };

    return formatParkPrice(value, currencyCode, this.currentLanguage(), labels);
  }

  protected modeLabelKey(value: ParkPriceValue): string {
    return `parkPricing.modes.${value.mode}`;
  }

  protected audienceLabel(category: string): string {
    const normalizedCategory: string = category.trim().toLowerCase().replace(/[^a-z0-9]+/g, '-');
    const key: string = `parkPricing.audiences.${normalizedCategory}`;
    const translated: string = this.ngxTranslateService.instant(key);
    return translated === key ? category : translated;
  }

  protected formatDate(value: string): string {
    return new Intl.DateTimeFormat(this.currentLanguage(), {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    }).format(new Date(`${value}T12:00:00`));
  }

  protected formatDateTime(value: string): string {
    return new Intl.DateTimeFormat(this.currentLanguage(), {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    }).format(new Date(value));
  }

  private loadPricingPage(parkId: string): Observable<ParkPricingPageData> {
    const previousData: ParkPricingPageData | undefined = this.stateStore.data();
    this.stateStore.setLoading(previousData);
    this.unavailablePark.set(null);
    this.unavailableParkImageId.set(null);

    return this.parksApiService.getParkDetailSummary(parkId, anonymousHttpOptions()).pipe(
      switchMap((summary: ParkDetailSummary) => {
        const parkImageId: string | null = resolveParkSummarySocialImageId(summary);
        const routeTarget = {
          language: this.currentLanguage(),
          parkId: summary.park.id,
          parkName: summary.park.name
        };

        if (!isParkOpenToVisitors(summary.park.status)) {
          this.unavailableParkImageId.set(parkImageId);
          this.unavailablePark.set(summary.park);
          this.detailLink.set(buildPublicParkRouteCommands(routeTarget));
          this.stateStore.setEmpty();
          this.ssrHttpStatusService.setNotFound();
          return EMPTY;
        }

        return this.parksApiService.getParkPricing(parkId, anonymousHttpOptions()).pipe(
          map((pricing: ParkPricing): ParkPricingPageData => ({
            park: summary.park,
            parkImageId,
            pricing
          })),
          catchError((error: unknown) => {
            if (hasHttpStatus(error, 404)) {
              return of({
                park: summary.park,
                parkImageId,
                pricing: null
              });
            }

            return throwError((): unknown => error);
          })
        );
      }),
      catchError((error: unknown): Observable<never> => {
        console.error('Error loading park pricing page', error);
        applySsrPublicDataErrorStatus(error, this.ssrHttpStatusService);

        if (hasHttpStatus(error, 404)) {
          this.stateStore.setEmpty();
          this.detailLink.set(null);
          this.seoService.applyNotFoundSeo(this.currentLanguage(), this.router.url);
          return EMPTY;
        }

        this.stateStore.setError('parkPricing.page.errorMessage', previousData);
        return EMPTY;
      })
    );
  }
}
