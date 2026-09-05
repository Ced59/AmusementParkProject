import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { PassportGlobalRatingEvolution } from '@app/models/passport/passport-statistics.models';

@Component({
  selector: 'app-passport-rating-evolution-chart',
  templateUrl: './passport-rating-evolution-chart.component.html',
  styleUrl: './passport-rating-evolution-chart.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe, TranslateModule]
})
export class PassportRatingEvolutionChartComponent {
  @Input({ required: true }) points: PassportGlobalRatingEvolution[] = [];

  protected width(value: number | null): number {
    return value === null ? 0 : Math.max(0, Math.min(100, (value / 5) * 100));
  }

  protected hasRatings(): boolean {
    return this.points.some((point: PassportGlobalRatingEvolution): boolean =>
      point.parkAverage !== null || point.rideAverage !== null);
  }
}
