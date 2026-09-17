import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import {
  WatchPilotDailyMetrics,
  WatchPilotMetricsQuery,
  WatchPilotSignal
} from '@app/models/admin/watch-pilot/watch-pilot-metrics.models';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { UiTemplate } from '@shared/ui/primitives/api';
import { ButtonDirective } from '@shared/ui/primitives/button';
import { Card } from '@shared/ui/primitives/card';
import { InputText } from '@shared/ui/primitives/inputtext';
import { Tag } from '@shared/ui/primitives/tag';
import { AdminWatchPilotFacade } from '../../state/admin-watch-pilot.facade';

interface WatchPilotFiltersForm {
  readonly fromUtc: FormControl<string>;
  readonly toUtc: FormControl<string>;
}

interface WatchPilotChartPoint extends WatchPilotDailyMetrics {
  readonly heightPercent: number;
}

interface WatchPilotBreakdownItem {
  readonly key: string;
  readonly count: number;
  readonly widthPercent: number;
}

@Component({
  selector: 'app-admin-watch-pilot',
  templateUrl: './admin-watch-pilot.component.html',
  styleUrl: './admin-watch-pilot.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AdminWatchPilotFacade],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    TranslateModule,
    PageStateComponent,
    UiTemplate,
    ButtonDirective,
    Card,
    InputText,
    Tag
  ]
})
export class AdminWatchPilotComponent implements OnInit {
  protected readonly state = this.facade.state;
  protected readonly metrics = this.facade.metrics;
  protected readonly filtersForm = new FormGroup<WatchPilotFiltersForm>({
    fromUtc: new FormControl<string>('', { nonNullable: true }),
    toUtc: new FormControl<string>('', { nonNullable: true })
  });
  protected readonly chartPoints = computed<readonly WatchPilotChartPoint[]>((): readonly WatchPilotChartPoint[] => {
    const daily: readonly WatchPilotDailyMetrics[] = this.metrics()?.daily ?? [];
    const maximum: number = Math.max(1, ...daily.map((point: WatchPilotDailyMetrics): number => point.notificationsDelivered));
    return daily.map((point: WatchPilotDailyMetrics): WatchPilotChartPoint => ({
      ...point,
      heightPercent: point.notificationsDelivered === 0
        ? 0
        : Math.max(5, Math.round(point.notificationsDelivered * 100 / maximum))
    }));
  });
  protected readonly subscriptionBreakdown = computed<readonly WatchPilotBreakdownItem[]>(
    (): readonly WatchPilotBreakdownItem[] => this.toBreakdown(
      this.metrics()?.activeSubscriptionsByEventType ?? {}
    )
  );
  protected readonly queueBreakdown = computed<readonly WatchPilotBreakdownItem[]>(
    (): readonly WatchPilotBreakdownItem[] => this.toBreakdown(
      this.metrics()?.queueCountsByStatus ?? {}
    )
  );

  constructor(private readonly facade: AdminWatchPilotFacade) {
  }

  ngOnInit(): void {
    this.facade.load();
  }

  protected applyFilters(): void {
    this.facade.load(this.toQuery());
  }

  protected resetFilters(): void {
    this.filtersForm.reset();
    this.facade.load();
  }

  protected signalLabel(signal: WatchPilotSignal): string {
    return `admin.watchPilot.signal.${signal}`;
  }

  protected signalSeverity(signal: WatchPilotSignal): 'secondary' | 'warn' | 'success' {
    if (signal === 'ReadyToExtend') {
      return 'success';
    }
    return signal === 'NeedsAttention' ? 'warn' : 'secondary';
  }

  private toQuery(): WatchPilotMetricsQuery {
    const values = this.filtersForm.getRawValue();
    return {
      fromUtc: this.toUtc(values.fromUtc, false),
      toUtc: this.toUtc(values.toUtc, true)
    };
  }

  private toUtc(value: string, endOfDay: boolean): string | null {
    const normalized: string = value.trim();
    if (!normalized) {
      return null;
    }
    const date = new Date(`${normalized}${endOfDay ? 'T23:59:59.999Z' : 'T00:00:00.000Z'}`);
    return Number.isNaN(date.getTime()) ? null : date.toISOString();
  }

  private toBreakdown(counts: Readonly<Record<string, number>>): readonly WatchPilotBreakdownItem[] {
    const entries: [string, number][] = Object.entries(counts)
      .filter((entry: [string, number]): boolean => entry[1] > 0)
      .sort((left: [string, number], right: [string, number]): number => right[1] - left[1]);
    const maximum: number = Math.max(1, ...entries.map((entry: [string, number]): number => entry[1]));
    return entries.map((entry: [string, number]): WatchPilotBreakdownItem => ({
      key: entry[0],
      count: entry[1],
      widthPercent: Math.max(4, Math.round(entry[1] * 100 / maximum))
    }));
  }
}
