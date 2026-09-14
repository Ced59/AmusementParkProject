import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import {
  ParkFitPilotDailyMetrics,
  ParkFitPilotMetricsQuery,
  ParkFitPilotSignal
} from '@app/models/admin/park-fit/park-fit-pilot-metrics.models';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { ButtonDirective } from '@shared/ui/primitives/button';
import { Card } from '@shared/ui/primitives/card';
import { InputText } from '@shared/ui/primitives/inputtext';
import { Tag } from '@shared/ui/primitives/tag';
import { UiTemplate } from '@shared/ui/primitives/api';
import { AdminParkFitPilotFacade } from '../../state/admin-park-fit-pilot.facade';

interface AdminParkFitPilotFiltersForm {
  readonly fromUtc: FormControl<string>;
  readonly toUtc: FormControl<string>;
}

interface AdminParkFitPilotChartPoint extends ParkFitPilotDailyMetrics {
  readonly completed: number;
  readonly completedHeightPercent: number;
  readonly reportsHeightPercent: number;
}

interface AdminParkFitPilotBreakdownItem {
  readonly key: string;
  readonly count: number;
  readonly widthPercent: number;
}

@Component({
  selector: 'app-admin-park-fit-pilot',
  templateUrl: './admin-park-fit-pilot.component.html',
  styleUrl: './admin-park-fit-pilot.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AdminParkFitPilotFacade],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    TranslateModule,
    ButtonDirective,
    Card,
    InputText,
    Tag,
    UiTemplate,
    PageStateComponent
  ]
})
export class AdminParkFitPilotComponent implements OnInit {
  protected readonly state = this.facade.state;
  protected readonly metrics = this.facade.metrics;
  protected readonly signal = this.facade.signal;
  protected readonly chartPoints = computed<readonly AdminParkFitPilotChartPoint[]>(
    (): readonly AdminParkFitPilotChartPoint[] => this.buildChartPoints(this.facade.daily())
  );
  protected readonly resultBreakdown = computed<readonly AdminParkFitPilotBreakdownItem[]>(
    (): readonly AdminParkFitPilotBreakdownItem[] =>
      this.buildBreakdown(this.metrics()?.resultBandCounts ?? {})
  );
  protected readonly unknownBreakdown = computed<readonly AdminParkFitPilotBreakdownItem[]>(
    (): readonly AdminParkFitPilotBreakdownItem[] =>
      this.buildBreakdown(this.metrics()?.unknownLevelCounts ?? {})
  );
  protected readonly issueBreakdown = computed<readonly AdminParkFitPilotBreakdownItem[]>(
    (): readonly AdminParkFitPilotBreakdownItem[] =>
      this.buildBreakdown(this.metrics()?.zeroResultQualityIssueCounts ?? {})
  );
  protected readonly filtersForm = new FormGroup<AdminParkFitPilotFiltersForm>({
    fromUtc: new FormControl<string>('', { nonNullable: true }),
    toUtc: new FormControl<string>('', { nonNullable: true })
  });

  constructor(private readonly facade: AdminParkFitPilotFacade) {
  }

  ngOnInit(): void {
    this.facade.load();
  }

  protected applyFilters(): void {
    this.facade.load(this.toQuery());
  }

  protected resetFilters(): void {
    this.filtersForm.reset();
    this.facade.load({ fromUtc: null, toUtc: null });
  }

  protected signalLabel(signal: ParkFitPilotSignal): string {
    return `admin.parkFitPilot.signal.${signal}`;
  }

  protected signalSeverity(signal: ParkFitPilotSignal): 'secondary' | 'warn' | 'success' {
    if (signal === 'Encouraging') {
      return 'success';
    }

    return signal === 'NeedsAttention' ? 'warn' : 'secondary';
  }

  protected resultLabel(key: string): string {
    return `admin.parkFitPilot.resultBand.${key}`;
  }

  protected unknownLabel(key: string): string {
    return `admin.parkFitPilot.unknownLevel.${key}`;
  }

  protected issueLabel(key: string): string {
    return `admin.parkFitDataQuality.issue.${key}`;
  }

  private toQuery(): ParkFitPilotMetricsQuery {
    const value = this.filtersForm.getRawValue();
    return {
      fromUtc: this.toUtcIso(value.fromUtc, false),
      toUtc: this.toUtcIso(value.toUtc, true)
    };
  }

  private toUtcIso(value: string, endOfDay: boolean): string | null {
    const normalizedValue: string = value.trim();
    if (normalizedValue.length === 0) {
      return null;
    }

    const suffix: string = endOfDay ? 'T23:59:59.999Z' : 'T00:00:00.000Z';
    const date: Date = new Date(`${normalizedValue}${suffix}`);
    return Number.isNaN(date.getTime()) ? null : date.toISOString();
  }

  private buildChartPoints(
    points: readonly ParkFitPilotDailyMetrics[]
  ): readonly AdminParkFitPilotChartPoint[] {
    const maxCount: number = Math.max(
      1,
      ...points.map((point: ParkFitPilotDailyMetrics): number => Math.max(
        point.eventCounts['SearchCompleted'] ?? 0,
        point.sourceReports
      ))
    );
    return points.map((point: ParkFitPilotDailyMetrics): AdminParkFitPilotChartPoint => {
      const completed: number = point.eventCounts['SearchCompleted'] ?? 0;
      return {
        ...point,
        completed,
        completedHeightPercent: this.heightPercent(completed, maxCount),
        reportsHeightPercent: this.heightPercent(point.sourceReports, maxCount)
      };
    });
  }

  private buildBreakdown(
    counts: Readonly<Record<string, number>>
  ): readonly AdminParkFitPilotBreakdownItem[] {
    const entries: [string, number][] = Object.entries(counts)
      .filter((entry: [string, number]): boolean => entry[1] > 0)
      .sort((left: [string, number], right: [string, number]): number => right[1] - left[1]);
    const maximum: number = Math.max(1, ...entries.map((entry: [string, number]): number => entry[1]));
    return entries.map((entry: [string, number]): AdminParkFitPilotBreakdownItem => ({
      key: entry[0],
      count: entry[1],
      widthPercent: Math.max(4, Math.round(entry[1] * 100 / maximum))
    }));
  }

  private heightPercent(value: number, maximum: number): number {
    return value === 0 ? 0 : Math.max(5, Math.round(value * 100 / maximum));
  }
}
