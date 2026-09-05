import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { PassportGlobalBarChartRow } from '../../models/passport-global-chart.models';

@Component({
  selector: 'app-passport-global-bar-chart',
  templateUrl: './passport-global-bar-chart.component.html',
  styleUrl: './passport-global-bar-chart.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslateModule]
})
export class PassportGlobalBarChartComponent {
  @Input({ required: true }) titleKey!: string;
  @Input({ required: true }) descriptionKey!: string;
  @Input({ required: true }) primaryLegendKey!: string;
  @Input() secondaryLegendKey: string | null = null;
  @Input() denominatorKey: string | null = null;
  @Input() emptyKey: string = 'passport.globalStatistics.charts.empty';
  @Input({ required: true }) rows: PassportGlobalBarChartRow[] = [];

  protected width(value: number): number {
    const maximum: number = Math.max(
      1,
      ...this.rows.flatMap((row: PassportGlobalBarChartRow): number[] => [
        row.primaryValue,
        row.secondaryValue ?? 0
      ])
    );
    return Math.max(0, Math.min(100, (value / maximum) * 100));
  }
}
