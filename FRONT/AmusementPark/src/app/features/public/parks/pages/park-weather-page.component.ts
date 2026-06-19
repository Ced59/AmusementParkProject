import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin } from 'rxjs';
import { TranslateModule } from '@ngx-translate/core';

import { Park } from '@app/models/parks/park';
import {
  ParkWeatherDay,
  ParkWeatherForecast,
  ParkWeatherHistoricalComparison,
  ParkWeatherHistoricalComparisonDay,
  ParkWeatherHistoricalComparisons
} from '@app/models/parks/park-weather';
import { MeasurementPreferenceService } from '@app/services/measurements/measurement-preference.service';
import { TranslationService } from '@app/services/translation.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { SeoService } from '@core/seo/seo.service';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { hasHttpStatus } from '@core/http/http-error-status.helpers';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { MeasurementConversionService } from '@shared/services/measurements/measurement-conversion.service';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';
import { buildPublicParkRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
import { UiButtonDirective } from '@ui/primitives';
import { resolveWeatherConditionKey, resolveWeatherIconClass } from '../ui/park-weather-card.component';

interface ParkWeatherPageData {
  park: Park;
  weather: ParkWeatherForecast;
}

type WeatherDisplayDay = Pick<ParkWeatherDay, 'weatherCode' | 'temperatureMinCelsius' | 'temperatureMaxCelsius' | 'precipitationSumMillimeters' | 'windSpeedMaxKilometersPerHour'>;
type TemperatureSeriesKind = 'min' | 'max';

const CHART_LEFT: number = 64;
const CHART_RIGHT: number = 724;
const CHART_TOP: number = 28;
const CHART_BOTTOM: number = 216;

@Component({
  selector: 'app-park-weather-page',
  templateUrl: './park-weather-page.component.html',
  styleUrls: ['./park-weather-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PageStateComponent, RouterLink, TranslateModule, UiButtonDirective]
})
export class ParkWeatherPageComponent implements OnInit {
  private readonly stateStore = new SignalScreenStateStore<ParkWeatherPageData>();
  private readonly historicalComparisonStore = new SignalScreenStateStore<ParkWeatherHistoricalComparisons>();

  protected readonly state = this.stateStore.state;
  protected readonly historicalComparisonState = this.historicalComparisonStore.state;
  protected readonly historicalComparisonRequested = signal<boolean>(false);
  protected readonly currentLanguage = signal<string>('en');
  protected readonly detailLink = signal<string[] | null>(null);

  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private currentParkId: string | null = null;
  private loadedHistoricalComparisonParkId: string | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly translationService: TranslationService,
    private readonly parksApiService: ParksApiService,
    private readonly seoService: SeoService,
    private readonly measurementPreferenceService: MeasurementPreferenceService,
    private readonly measurementConversionService: MeasurementConversionService,
    private readonly ssrHttpStatusService: SsrHttpStatusService
  ) {
    effect((): void => {
      const currentData: ParkWeatherPageData | undefined = this.stateStore.data();
      if (!currentData) {
        return;
      }

      this.detailLink.set(buildPublicParkRouteCommands({
        language: this.currentLanguage(),
        parkId: currentData.park.id,
        parkName: currentData.park.name
      }));

      this.seoService.applyParkWeatherSeo(
        currentData.park.name ?? 'Park',
        this.currentLanguage(),
        this.router.url,
        currentData.weather.days.length);
    });
  }

  ngOnInit(): void {
    const initialLanguage: string = resolveLanguageFromActivatedRoute(this.route, this.translationService.getCurrentLang() || 'en');
    this.currentLanguage.set(initialLanguage);

    this.translationService.languageChanged.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((language: string) => {
      this.currentLanguage.set(language);
    });

    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params: ParamMap) => {
      const parkId: string | null = params.get('id');
      if (!parkId || parkId === this.currentParkId) {
        return;
      }

      this.currentParkId = parkId;
      this.loadWeatherPage(parkId);
    });
  }

  conditionKey(day: WeatherDisplayDay): string {
    return `parkWeather.conditions.${resolveWeatherConditionKey(day.weatherCode ?? null)}`;
  }

  iconClass(day: WeatherDisplayDay): string {
    return resolveWeatherIconClass(day.weatherCode ?? null);
  }

  temperatureRange(day: WeatherDisplayDay): string {
    const min: string | null = this.formatTemperature(day.temperatureMinCelsius);
    const max: string | null = this.formatTemperature(day.temperatureMaxCelsius);

    if (min && max) {
      return `${min} / ${max}`;
    }

    return min ?? max ?? '';
  }

  temperatureLinePoints(days: ParkWeatherDay[], kind: TemperatureSeriesKind): string {
    return days
      .map((day: ParkWeatherDay, index: number) => {
        const value: number | null | undefined = kind === 'max'
          ? day.temperatureMaxCelsius
          : day.temperatureMinCelsius;

        if (!this.isTemperatureAvailable(value)) {
          return null;
        }

        return `${this.chartDayX(index, days)},${this.temperatureY(value, days)}`;
      })
      .filter((point: string | null): point is string => point !== null)
      .join(' ');
  }

  chartAxisValues(days: ParkWeatherDay[]): number[] {
    const scale: { min: number; max: number } = this.temperatureScale(days);
    const middle: number = scale.min + ((scale.max - scale.min) / 2);
    return [scale.max, middle, scale.min];
  }

  chartDayX(index: number, days: ParkWeatherDay[]): number {
    const dayCount: number = Math.max(1, days.length);
    const step: number = (CHART_RIGHT - CHART_LEFT) / dayCount;
    return Math.round(CHART_LEFT + (step * index) + (step / 2));
  }

  temperatureY(value: number | null | undefined, days: ParkWeatherDay[]): number {
    if (!this.isTemperatureAvailable(value)) {
      return CHART_BOTTOM;
    }

    const scale: { min: number; max: number } = this.temperatureScale(days);
    const ratio: number = (value - scale.min) / Math.max(1, scale.max - scale.min);
    return Math.round(CHART_BOTTOM - (ratio * (CHART_BOTTOM - CHART_TOP)));
  }

  isTemperatureAvailable(value: number | null | undefined): value is number {
    return value !== null && value !== undefined && Number.isFinite(value);
  }

  precipitationProbabilityLabel(day: ParkWeatherDay): string {
    if (day.precipitationProbabilityMaxPercent === null || day.precipitationProbabilityMaxPercent === undefined) {
      return '-';
    }

    return `${this.formatNumber(day.precipitationProbabilityMaxPercent, 0)}%`;
  }

  precipitationSumLabel(day: WeatherDisplayDay): string {
    if (day.precipitationSumMillimeters === null || day.precipitationSumMillimeters === undefined) {
      return '-';
    }

    return `${this.formatNumber(day.precipitationSumMillimeters, 1)} mm`;
  }

  windSpeedLabel(day: WeatherDisplayDay): string {
    return this.measurementConversionService.formatSpeedFromKilometersPerHour(
      day.windSpeedMaxKilometersPerHour,
      this.measurementPreferenceService.preferredSystem(),
      this.currentLanguage()) ?? '-';
  }

  chartTemperatureLabel(value: number | null | undefined): string {
    return this.formatTemperature(value) ?? '-';
  }

  formatDate(value: string): string {
    return new Intl.DateTimeFormat(this.currentLanguage(), {
      weekday: 'short',
      month: 'short',
      day: 'numeric'
    }).format(new Date(`${value}T12:00:00`));
  }

  formatShortDate(value: string): string {
    return new Intl.DateTimeFormat(this.currentLanguage(), {
      weekday: 'short',
      day: 'numeric'
    }).format(new Date(`${value}T12:00:00`));
  }

  comparisonYearLabelKey(yearsBack: number): string {
    return yearsBack === 1
      ? 'parkWeather.history.yearSingular'
      : 'parkWeather.history.yearPlural';
  }

  historicalComparisonDay(comparison: ParkWeatherHistoricalComparison, forecastLocalDate: string): ParkWeatherHistoricalComparisonDay | null {
    return comparison.days.find((day: ParkWeatherHistoricalComparisonDay) => day.forecastLocalDate === forecastLocalDate) ?? null;
  }

  onHistoricalComparisonToggle(event: Event, parkId: string | null | undefined): void {
    const normalizedParkId: string = parkId?.trim() ?? '';
    if (!normalizedParkId) {
      return;
    }

    const details: HTMLDetailsElement | null = event.currentTarget instanceof HTMLDetailsElement
      ? event.currentTarget
      : null;

    if (!details?.open || this.loadedHistoricalComparisonParkId === normalizedParkId) {
      return;
    }

    this.loadHistoricalComparisons(normalizedParkId);
  }

  private loadWeatherPage(parkId: string): void {
    const previousData: ParkWeatherPageData | undefined = this.stateStore.data();
    this.stateStore.setLoading(previousData);
    this.historicalComparisonRequested.set(false);
    this.loadedHistoricalComparisonParkId = null;

    forkJoin({
      park: this.parksApiService.getParkById(parkId, anonymousHttpOptions()),
      weather: this.parksApiService.getParkWeather(parkId, 7, anonymousHttpOptions())
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data: ParkWeatherPageData) => {
        if (!data.weather.days || data.weather.days.length === 0) {
          this.stateStore.setEmpty(data);
          return;
        }

        this.stateStore.setReady(data);
      },
      error: (error: unknown) => {
        console.error('Error loading park weather page', error);

        if (hasHttpStatus(error, 404)) {
          this.ssrHttpStatusService.setNotFound();
        }

        this.stateStore.setError('parkWeather.errorMessage', previousData);
      }
    });
  }

  private loadHistoricalComparisons(parkId: string): void {
    this.historicalComparisonRequested.set(true);
    this.historicalComparisonStore.setLoading();

    this.parksApiService.getParkWeatherHistoricalComparisons(parkId, 7, 10, anonymousHttpOptions())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (comparisons: ParkWeatherHistoricalComparisons) => {
          this.loadedHistoricalComparisonParkId = parkId;
          if (!comparisons.years || comparisons.years.length === 0) {
            this.historicalComparisonStore.setEmpty(comparisons);
            return;
          }

          this.historicalComparisonStore.setReady(comparisons);
        },
        error: (error: unknown) => {
          console.error('Error loading park weather historical comparisons', error);
          this.historicalComparisonStore.setError('parkWeather.history.errorMessage');
        }
      });
  }

  private formatTemperature(value: number | null | undefined): string | null {
    return this.measurementConversionService.formatTemperatureFromCelsius(
      value,
      this.measurementPreferenceService.preferredSystem(),
      this.currentLanguage());
  }

  private temperatureScale(days: ParkWeatherDay[]): { min: number; max: number } {
    const values: number[] = days
      .flatMap((day: ParkWeatherDay) => [day.temperatureMinCelsius, day.temperatureMaxCelsius])
      .filter((value: number | null | undefined): value is number => this.isTemperatureAvailable(value));

    if (values.length === 0) {
      return { min: 0, max: 10 };
    }

    const rawMin: number = Math.min(...values);
    const rawMax: number = Math.max(...values);
    const padding: number = Math.max(2, (rawMax - rawMin) * 0.16);

    if (rawMax === rawMin) {
      return {
        min: rawMin - 3,
        max: rawMax + 3
      };
    }

    return {
      min: Math.floor(rawMin - padding),
      max: Math.ceil(rawMax + padding)
    };
  }

  private formatNumber(value: number, maximumFractionDigits: number): string {
    return new Intl.NumberFormat(this.currentLanguage(), {
      maximumFractionDigits,
      minimumFractionDigits: 0
    }).format(value);
  }
}
