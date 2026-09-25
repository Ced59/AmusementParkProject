import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { TripPilotMetricsResult } from '@app/models/admin/trip-pilot/trip-pilot-metrics.models';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { UiTemplate } from '@shared/ui/primitives/api';
import { ButtonDirective } from '@shared/ui/primitives/button';
import { Card } from '@shared/ui/primitives/card';
import { AdminTripPilotFacade } from '../../state/admin-trip-pilot.facade';

interface TripPilotActivityItem {
  readonly key: string;
  readonly count: number;
  readonly widthPercent: number;
}

@Component({
  selector: 'app-admin-trip-pilot',
  templateUrl: './admin-trip-pilot.component.html',
  styleUrl: './admin-trip-pilot.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AdminTripPilotFacade],
  imports: [CommonModule, TranslateModule, PageStateComponent, UiTemplate, ButtonDirective, Card]
})
export class AdminTripPilotComponent implements OnInit {
  protected readonly state = this.facade.state;
  protected readonly metrics = this.facade.metrics;
  protected readonly activity = computed<readonly TripPilotActivityItem[]>(
    (): readonly TripPilotActivityItem[] => this.toActivity(this.metrics())
  );

  constructor(protected readonly facade: AdminTripPilotFacade) {
  }

  ngOnInit(): void {
    this.facade.load();
  }

  private toActivity(metrics: TripPilotMetricsResult | null): readonly TripPilotActivityItem[] {
    const entries: [string, number][] = Object.entries(metrics?.activityCounts ?? {})
      .filter((entry: [string, number]): boolean => entry[1] > 0)
      .sort((left: [string, number], right: [string, number]): number => right[1] - left[1]);
    const maximum: number = Math.max(1, ...entries.map((entry: [string, number]): number => entry[1]));
    return entries.map((entry: [string, number]): TripPilotActivityItem => ({
      key: entry[0],
      count: entry[1],
      widthPercent: Math.max(4, Math.round(entry[1] * 100 / maximum))
    }));
  }
}
