import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, effect, inject, signal } from '@angular/core';
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
  buildParkPricingHistorySeries,
  hasSingleHistoryCurrency,
  parkPriceChartAmount,
  ParkPriceFormattingLabels,
  ParkPricingHistoryChannel,
  ParkPricingHistoryPoint,
  ParkPricingHistorySeries,
  resolvePricingLocalizedText
} from '../models/park-pricing.presentation';
import { ParkLifecycleNoticeComponent } from '../ui/park-lifecycle-notice.component';

interface ParkPricingPageData {
  park: Park;
  parkImageId: string | null;
  pricing: ParkPricing | null;
}

interface ParkPricingEvolution {
  channel: ParkPricingHistoryChannel;
  amount: string;
  percentage: string | null;
  directionKey: string;
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
  protected readonly historySeries = computed((): ParkPricingHistorySeries[] => {
    const pricing: ParkPricing | null | undefined = this.stateStore.data()?.pricing;
    return pricing
      ? buildParkPricingHistorySeries(pricing, this.currentLanguage(), new Date().getUTCFullYear(), 5)
      : [];
  });
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
        ? data.pricing.admissionOffers.length + (data.pricing.creditOffers?.length ?? 0) + data.pricing.annualPasses.length + data.pricing.parkingOffers.length
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
          ? data.pricing.admissionOffers.length + (data.pricing.creditOffers?.length ?? 0) + data.pricing.annualPasses.length + data.pricing.parkingOffers.length
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

  protected formatCreditPrice(value: number | null | undefined, currencyCode: string): string | null {
    if (value === null || value === undefined) {
      return null;
    }

    return new Intl.NumberFormat(this.currentLanguage(), {
      style: 'currency',
      currency: currencyCode || 'EUR',
      maximumFractionDigits: 2
    }).format(value);
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

  protected historyKindLabelKey(series: ParkPricingHistorySeries): string {
    return series.kind === 'credit'
      ? 'parkPricingCredits.historyKind'
      : `parkPricing.history.kinds.${series.kind}`;
  }

  protected historyHasSingleCurrency(series: ParkPricingHistorySeries): boolean {
    return hasSingleHistoryCurrency(series);
  }

  protected historyCurrencies(series: ParkPricingHistorySeries): string {
    return [...new Set(series.points.map((point: ParkPricingHistoryPoint): string => point.currencyCode))].join(' → ');
  }

  protected historyLinePoints(series: ParkPricingHistorySeries, channel: ParkPricingHistoryChannel): string {
    return series.points
      .map((point: ParkPricingHistoryPoint, index: number): string | null => {
        const amount: number | null = parkPriceChartAmount(point[channel]);
        return amount === null
          ? null
          : `${this.historyPointX(index, series.points.length)},${this.historyPriceY(amount, series)}`;
      })
      .filter((point: string | null): point is string => point !== null)
      .join(' ');
  }

  protected historyPointX(index: number, pointCount: number): number {
    return pointCount <= 1 ? 320 : 54 + ((532 * index) / (pointCount - 1));
  }

  protected historyPriceY(amount: number, series: ParkPricingHistorySeries): number {
    const amounts: number[] = this.historyAmounts(series);
    const minimum: number = Math.min(...amounts);
    const maximum: number = Math.max(...amounts);
    if (maximum === minimum) {
      return 105;
    }

    return 180 - (((amount - minimum) / (maximum - minimum)) * 135);
  }

  protected historyChartAmount(point: ParkPricingHistoryPoint, channel: ParkPricingHistoryChannel): number | null {
    return parkPriceChartAmount(point[channel]);
  }

  protected historyHasChannel(series: ParkPricingHistorySeries, channel: ParkPricingHistoryChannel): boolean {
    return series.points.some((point: ParkPricingHistoryPoint): boolean => point[channel] !== null && point[channel] !== undefined);
  }

  protected historyPriceLabel(point: ParkPricingHistoryPoint, channel: ParkPricingHistoryChannel): string {
    return this.formatPrice(point[channel], point.currencyCode) ?? '—';
  }

  protected historyEvolution(series: ParkPricingHistorySeries): ParkPricingEvolution | null {
    if (!hasSingleHistoryCurrency(series)) {
      return null;
    }

    const channel: ParkPricingHistoryChannel | null = this.evolutionChannel(series);
    if (!channel) {
      return null;
    }

    const pricedPoints: Array<{ point: ParkPricingHistoryPoint; amount: number }> = series.points
      .map((point: ParkPricingHistoryPoint): { point: ParkPricingHistoryPoint; amount: number } | null => {
        const amount: number | null = parkPriceChartAmount(point[channel]);
        return amount === null ? null : { point, amount };
      })
      .filter((item): item is { point: ParkPricingHistoryPoint; amount: number } => item !== null);
    if (pricedPoints.length < 2) {
      return null;
    }

    const oldest = pricedPoints[0];
    const latest = pricedPoints[pricedPoints.length - 1];
    const difference: number = latest.amount - oldest.amount;
    const percentage: number | null = oldest.amount === 0 ? null : (difference / oldest.amount) * 100;
    const formatter = new Intl.NumberFormat(this.currentLanguage(), {
      style: 'currency',
      currency: latest.point.currencyCode,
      maximumFractionDigits: 2
    });
    const percentageFormatter = new Intl.NumberFormat(this.currentLanguage(), {
      style: 'percent',
      maximumFractionDigits: 1
    });

    return {
      channel,
      amount: formatter.format(Math.abs(difference)),
      percentage: percentage === null ? null : percentageFormatter.format(Math.abs(percentage) / 100),
      directionKey: difference > 0
        ? percentage === null ? 'parkPricing.history.increaseAmount' : 'parkPricing.history.increase'
        : difference < 0
          ? percentage === null ? 'parkPricing.history.decreaseAmount' : 'parkPricing.history.decrease'
          : 'parkPricing.history.stable'
    };
  }

  private evolutionChannel(series: ParkPricingHistorySeries): ParkPricingHistoryChannel | null {
    const onlineCount: number = series.points.filter(
      (point: ParkPricingHistoryPoint): boolean => parkPriceChartAmount(point.onlinePrice) !== null).length;
    if (onlineCount >= 2) {
      return 'onlinePrice';
    }

    const gateCount: number = series.points.filter(
      (point: ParkPricingHistoryPoint): boolean => parkPriceChartAmount(point.gatePrice) !== null).length;
    return gateCount >= 2 ? 'gatePrice' : null;
  }

  private historyAmounts(series: ParkPricingHistorySeries): number[] {
    const amounts: number[] = [];
    for (const point of series.points) {
      const onlineAmount: number | null = parkPriceChartAmount(point.onlinePrice);
      const gateAmount: number | null = parkPriceChartAmount(point.gatePrice);
      if (onlineAmount !== null) {
        amounts.push(onlineAmount);
      }
      if (gateAmount !== null) {
        amounts.push(gateAmount);
      }
    }

    return amounts.length > 0 ? amounts : [0];
  }

  private loadPricingPage(parkId: string): Observable<ParkPricingPageData> {
    const previousData: ParkPricingPageData | undefined = this.stateStore.data();
    let resolvedSummary: ParkDetailSummary | null = null;
    this.stateStore.setLoading(previousData);
    this.unavailablePark.set(null);
    this.unavailableParkImageId.set(null);

    return this.parksApiService.getParkDetailSummary(parkId, anonymousHttpOptions()).pipe(
      switchMap((summary: ParkDetailSummary) => {
        resolvedSummary = summary;
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

        this.stateStore.setError('parkPricing.page.errorMessage');
        this.applyPricingErrorSeo(resolvedSummary);
        return EMPTY;
      })
    );
  }

  private applyPricingErrorSeo(summary: ParkDetailSummary | null): void {
    if (!summary) {
      this.detailLink.set(null);
      this.seoService.applyParkPricingSeo(
        '',
        this.currentLanguage(),
        this.router.url,
        0);
      return;
    }

    const routeTarget = {
      language: this.currentLanguage(),
      parkId: summary.park.id,
      parkName: summary.park.name
    };
    this.detailLink.set(buildPublicParkRouteCommands(routeTarget));
    this.seoService.applyParkPricingSeo(
      summary.park.name ?? '',
      this.currentLanguage(),
      this.router.url,
      0,
      resolveParkSummarySocialImageId(summary),
      buildPublicRoutePath(buildPublicParkPricingRouteCommands(routeTarget)));
  }
}
