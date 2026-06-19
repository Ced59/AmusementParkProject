import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin } from 'rxjs';
import { TranslateModule } from '@ngx-translate/core';

import { Park } from '@app/models/parks/park';
import { ParkWeatherDay, ParkWeatherForecast } from '@app/models/parks/park-weather';
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
import { resolveWeatherConditionKey, resolveWeatherIconClass } from '../ui/park-weather-card.component';

interface ParkWeatherPageData {
  park: Park;
  weather: ParkWeatherForecast;
}

@Component({
  selector: 'app-park-weather-page',
  templateUrl: './park-weather-page.component.html',
  styleUrls: ['./park-weather-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PageStateComponent, RouterLink, TranslateModule]
})
export class ParkWeatherPageComponent implements OnInit {
  private readonly stateStore = new SignalScreenStateStore<ParkWeatherPageData>();

  protected readonly state = this.stateStore.state;
  protected readonly currentLanguage = signal<string>('en');
  protected readonly detailLink = signal<string[] | null>(null);

  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private currentParkId: string | null = null;

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

  conditionKey(day: ParkWeatherDay): string {
    return `parkWeather.conditions.${resolveWeatherConditionKey(day.weatherCode ?? null)}`;
  }

  iconClass(day: ParkWeatherDay): string {
    return resolveWeatherIconClass(day.weatherCode ?? null);
  }

  temperatureRange(day: ParkWeatherDay): string {
    const min: string | null = this.formatTemperature(day.temperatureMinCelsius);
    const max: string | null = this.formatTemperature(day.temperatureMaxCelsius);

    if (min && max) {
      return `${min} / ${max}`;
    }

    return min ?? max ?? '';
  }

  formatDate(value: string): string {
    return new Intl.DateTimeFormat(this.currentLanguage(), {
      weekday: 'short',
      month: 'short',
      day: 'numeric'
    }).format(new Date(`${value}T12:00:00`));
  }

  private loadWeatherPage(parkId: string): void {
    const previousData: ParkWeatherPageData | undefined = this.stateStore.data();
    this.stateStore.setLoading(previousData);

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

  private formatTemperature(value: number | null | undefined): string | null {
    return this.measurementConversionService.formatTemperatureFromCelsius(
      value,
      this.measurementPreferenceService.preferredSystem(),
      this.currentLanguage());
  }
}
