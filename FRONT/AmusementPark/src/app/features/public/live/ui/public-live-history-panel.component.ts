import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { PublicLiveHistory, PublicLiveHistoryHour } from '@app/models/live-data/public-live.models';
import { UiButtonDirective, UiKickerComponent } from '@ui/primitives';
import { PublicLiveHistoryViewState } from '../models/public-live-history-view-state.model';

@Component({
  selector: 'app-public-live-history-panel',
  templateUrl: './public-live-history-panel.component.html',
  styleUrls: ['./public-live-history-panel.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslateModule, UiButtonDirective, UiKickerComponent]
})
export class PublicLiveHistoryPanelComponent {
  @Input({ required: true }) state!: PublicLiveHistoryViewState;
  @Input() currentLanguage: string = 'en';
  @Output() retryClicked: EventEmitter<void> = new EventEmitter<void>();

  constructor(private readonly translateService: TranslateService) {
  }

  protected retry(): void {
    this.retryClicked.emit();
  }

  protected statusLabelKey(history: PublicLiveHistory): string {
    return `liveData.history.status.${history.dataStatus.charAt(0).toLowerCase()}${history.dataStatus.slice(1)}`;
  }

  protected hourStatusLabelKey(hour: PublicLiveHistoryHour): string {
    return `liveData.history.status.${hour.dataStatus.charAt(0).toLowerCase()}${hour.dataStatus.slice(1)}`;
  }

  protected hasStatistics(hour: PublicLiveHistoryHour): boolean {
    return hour.medianMinutes !== null
      && hour.robustMinimumMinutes !== null
      && hour.firstQuartileMinutes !== null
      && hour.thirdQuartileMinutes !== null
      && hour.robustMaximumMinutes !== null;
  }

  protected hourLabel(hour: number): string {
    try {
      return new Intl.DateTimeFormat(this.currentLanguage, {
        hour: '2-digit',
        minute: '2-digit',
        timeZone: 'UTC'
      }).format(new Date(Date.UTC(2026, 0, 1, hour)));
    } catch {
      return `${hour.toString().padStart(2, '0')}:00`;
    }
  }

  protected periodLabel(history: PublicLiveHistory): string {
    try {
      const formatter: Intl.DateTimeFormat = new Intl.DateTimeFormat(this.currentLanguage, {
        dateStyle: 'medium',
        timeZone: history.timeZoneId
      });
      return `${formatter.format(new Date(history.fromUtc))} – ${formatter.format(new Date(history.toUtc))}`;
    } catch {
      return `${history.fromUtc.slice(0, 10)} – ${history.toUtc.slice(0, 10)}`;
    }
  }

  protected metricPosition(history: PublicLiveHistory, value: number | null): number {
    if (value === null) {
      return 0;
    }

    const maximum: number = Math.max(
      1,
      ...history.hours.map((hour: PublicLiveHistoryHour) => hour.robustMaximumMinutes ?? 0)
    );
    return Math.min(100, Math.max(0, value * 100 / maximum));
  }

  protected rangeWidth(history: PublicLiveHistory, from: number | null, to: number | null): number {
    return Math.max(1.5, this.metricPosition(history, to) - this.metricPosition(history, from));
  }

  protected chartAriaLabel(hour: PublicLiveHistoryHour): string {
    if (!this.hasStatistics(hour)) {
      return this.translateService.instant('liveData.history.chart.gapAria', {
        hour: this.hourLabel(hour.localHour)
      });
    }

    return this.translateService.instant('liveData.history.chart.valueAria', {
      hour: this.hourLabel(hour.localHour),
      median: hour.medianMinutes,
      firstQuartile: hour.firstQuartileMinutes,
      thirdQuartile: hour.thirdQuartileMinutes,
      observations: hour.usableWaitCount,
      days: hour.comparableDays
    });
  }
}
