import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import {
  PassportBetaDailyMetrics,
  PassportBetaMetricsQuery,
  PassportBetaRepeatUsageSignal
} from '@app/models/passport/passport-beta-metrics.models';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { UiTemplate } from '@shared/ui/primitives/api';
import { ButtonDirective } from '@shared/ui/primitives/button';
import { Card } from '@shared/ui/primitives/card';
import { InputText } from '@shared/ui/primitives/inputtext';
import { Tag } from '@shared/ui/primitives/tag';
import { AdminPassportBetaFacade } from '../../state/admin-passport-beta.facade';

interface AdminPassportBetaFiltersForm {
  readonly fromUtc: FormControl<string>;
  readonly toUtc: FormControl<string>;
}

interface AdminPassportBetaChartPoint extends PassportBetaDailyMetrics {
  readonly completedHeightPercent: number;
  readonly secondHeightPercent: number;
}

@Component({
  selector: 'app-admin-passport-beta',
  templateUrl: './admin-passport-beta.component.html',
  styleUrl: './admin-passport-beta.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AdminPassportBetaFacade],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    TranslateModule,
    ButtonDirective,
    Card,
    InputText,
    UiTemplate,
    Tag,
    PageStateComponent
  ]
})
export class AdminPassportBetaComponent implements OnInit {
  protected readonly state = this.facade.state;
  protected readonly loading = this.facade.loading;
  protected readonly metrics = this.facade.metrics;
  protected readonly createdVisits = this.facade.createdVisits;
  protected readonly completedVisits = this.facade.completedVisits;
  protected readonly usersWithCompletedVisit = this.facade.usersWithCompletedVisit;
  protected readonly usersWithSecondCompletedVisit = this.facade.usersWithSecondCompletedVisit;
  protected readonly repeatUsageRatePercent = this.facade.repeatUsageRatePercent;
  protected readonly repeatUsageSignal = this.facade.repeatUsageSignal;
  protected readonly daily = this.facade.daily;
  protected readonly chartPoints = computed<readonly AdminPassportBetaChartPoint[]>(
    () => this.buildChartPoints(this.daily())
  );

  protected readonly filtersForm = new FormGroup<AdminPassportBetaFiltersForm>({
    fromUtc: new FormControl<string>('', { nonNullable: true }),
    toUtc: new FormControl<string>('', { nonNullable: true })
  });

  constructor(private readonly facade: AdminPassportBetaFacade) {
  }

  ngOnInit(): void {
    this.facade.load();
  }

  protected applyFilters(): void {
    this.facade.load(this.toQueryFilters());
  }

  protected resetFilters(): void {
    this.filtersForm.reset();
    this.facade.load({ fromUtc: null, toUtc: null });
  }

  protected signalLabel(signal: PassportBetaRepeatUsageSignal): string {
    return `admin.passportBeta.signal.${signal}`;
  }

  protected signalSeverity(signal: PassportBetaRepeatUsageSignal): 'secondary' | 'warn' | 'success' {
    if (signal === 'Candidate') {
      return 'success';
    }

    return signal === 'Emerging' ? 'warn' : 'secondary';
  }

  private toQueryFilters(): PassportBetaMetricsQuery {
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
    points: readonly PassportBetaDailyMetrics[]
  ): readonly AdminPassportBetaChartPoint[] {
    const maxCount: number = Math.max(
      1,
      ...points.map((point: PassportBetaDailyMetrics) => point.completedVisits)
    );

    return points.map((point: PassportBetaDailyMetrics) => ({
      ...point,
      completedHeightPercent: point.completedVisits === 0
        ? 0
        : Math.max(5, Math.round((point.completedVisits / maxCount) * 100)),
      secondHeightPercent: point.secondVisits === 0
        ? 0
        : Math.max(5, Math.round((point.secondVisits / maxCount) * 100))
    }));
  }
}
