import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { PublicLiveForecast } from '@app/models/live-data/public-live.models';
import { UiKickerComponent } from '@ui/primitives';
import { PublicLiveForecastViewState } from '../models/public-live-forecast-view-state.model';

@Component({
  selector: 'app-public-live-forecast-card',
  templateUrl: './public-live-forecast-card.component.html',
  styleUrls: ['./public-live-forecast-card.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslateModule, UiKickerComponent]
})
export class PublicLiveForecastCardComponent {
  @Input({ required: true }) state!: PublicLiveForecastViewState;
  @Input() currentLanguage: string = 'en';

  protected timeRange(forecast: PublicLiveForecast): string {
    return `${this.time(forecast.forecastFromUtc, forecast.timeZoneId)}–${this.time(forecast.forecastToUtc, forecast.timeZoneId)}`;
  }

  protected calculationDate(forecast: PublicLiveForecast): string {
    try {
      return new Intl.DateTimeFormat(this.currentLanguage, {
        dateStyle: 'medium',
        timeStyle: 'short',
        timeZone: forecast.timeZoneId
      }).format(new Date(forecast.calculatedAtUtc));
    } catch {
      return forecast.calculatedAtUtc;
    }
  }

  protected minutes(value: number): string {
    return new Intl.NumberFormat(this.currentLanguage, {
      maximumFractionDigits: 1
    }).format(value);
  }

  protected methodLabelKey(method: string): string {
    return method === 'rolling-weekday-hour-median-v1'
      ? 'liveData.forecast.method.weekdayHourMedian'
      : 'liveData.forecast.method.verifiedHistorical';
  }

  private time(value: string, timeZoneId: string): string {
    try {
      return new Intl.DateTimeFormat(this.currentLanguage, {
        hour: '2-digit',
        minute: '2-digit',
        timeZone: timeZoneId
      }).format(new Date(value));
    } catch {
      return value.slice(11, 16);
    }
  }
}
