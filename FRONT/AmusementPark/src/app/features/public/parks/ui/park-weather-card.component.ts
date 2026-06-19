import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { ParkWeatherDay, ParkWeatherForecast } from '@app/models/parks/park-weather';
import { MeasurementPreferenceService } from '@app/services/measurements/measurement-preference.service';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { MeasurementConversionService } from '@shared/services/measurements/measurement-conversion.service';

@Component({
  selector: 'app-park-weather-card',
  templateUrl: './park-weather-card.component.html',
  styleUrls: ['./park-weather-card.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslateModule]
})
export class ParkWeatherCardComponent {
  @Input() forecast: ParkWeatherForecast | null = null;
  @Input() state: ScreenState<unknown, string> | null = null;
  @Input() weatherLink: string[] | null = null;
  @Input() currentLang: string = 'en';

  constructor(
    private readonly measurementPreferenceService: MeasurementPreferenceService,
    private readonly measurementConversionService: MeasurementConversionService
  ) {
  }

  get today(): ParkWeatherDay | null {
    return this.forecast?.days?.[0] ?? null;
  }

  get isLoading(): boolean {
    return this.state?.kind === 'loading';
  }

  conditionKey(day: ParkWeatherDay | null): string {
    return `parkWeather.conditions.${resolveWeatherConditionKey(day?.weatherCode ?? null)}`;
  }

  iconClass(day: ParkWeatherDay | null): string {
    return resolveWeatherIconClass(day?.weatherCode ?? null);
  }

  temperatureRange(day: ParkWeatherDay | null): string {
    if (!day) {
      return '';
    }

    const min: string | null = this.formatTemperature(day.temperatureMinCelsius);
    const max: string | null = this.formatTemperature(day.temperatureMaxCelsius);

    if (min && max) {
      return `${min} / ${max}`;
    }

    return min ?? max ?? '';
  }

  private formatTemperature(value: number | null | undefined): string | null {
    if (!Number.isFinite(value)) {
      return null;
    }

    return this.measurementConversionService.formatTemperatureFromCelsius(
      value,
      this.measurementPreferenceService.preferredSystem(),
      this.currentLang);
  }
}

export function resolveWeatherConditionKey(weatherCode: number | null): string {
  if (weatherCode === null) {
    return 'unknown';
  }

  if (weatherCode === 0) {
    return 'clear';
  }

  if (weatherCode === 1 || weatherCode === 2) {
    return 'partlyCloudy';
  }

  if (weatherCode === 3) {
    return 'cloudy';
  }

  if (weatherCode === 45 || weatherCode === 48) {
    return 'fog';
  }

  if ((weatherCode >= 51 && weatherCode <= 67) || (weatherCode >= 80 && weatherCode <= 82)) {
    return 'rain';
  }

  if ((weatherCode >= 71 && weatherCode <= 77) || weatherCode === 85 || weatherCode === 86) {
    return 'snow';
  }

  if (weatherCode === 95 || weatherCode === 96 || weatherCode === 99) {
    return 'thunderstorm';
  }

  return 'unknown';
}

export function resolveWeatherIconClass(weatherCode: number | null): string {
  const conditionKey: string = resolveWeatherConditionKey(weatherCode);

  if (conditionKey === 'clear') {
    return 'pi pi-sun';
  }

  if (conditionKey === 'partlyCloudy') {
    return 'pi pi-cloud';
  }

  if (conditionKey === 'rain') {
    return 'pi pi-cloud';
  }

  if (conditionKey === 'snow') {
    return 'pi pi-asterisk';
  }

  if (conditionKey === 'thunderstorm') {
    return 'pi pi-bolt';
  }

  if (conditionKey === 'fog') {
    return 'pi pi-bars';
  }

  return 'pi pi-cloud';
}
