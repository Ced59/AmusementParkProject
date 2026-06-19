import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Card } from 'primeng/card';
import { PrimeTemplate } from 'primeng/api';
import { TableModule } from 'primeng/table';
import { Tag } from 'primeng/tag';

import { ParkWeatherRun, ParkWeatherRunItem } from '@app/models/admin/park-weather/park-weather-admin.models';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { AdminParkWeatherStateFacade } from '../../state/admin-park-weather-state.facade';

@Component({
  selector: 'app-admin-park-weather',
  templateUrl: './admin-park-weather.component.html',
  styleUrl: './admin-park-weather.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AdminParkWeatherStateFacade],
  imports: [
    CommonModule,
    TranslateModule,
    ButtonDirective,
    Card,
    PrimeTemplate,
    TableModule,
    Tag,
    PageStateComponent
  ]
})
export class AdminParkWeatherComponent implements OnInit {
  protected readonly state = this.facade.state;
  protected readonly loading = this.facade.loading;
  protected readonly latestRun = this.facade.latestRun;
  protected readonly failedItems = this.facade.failedItems;
  protected readonly startingManualRefresh = this.facade.startingManualRefresh;
  protected readonly retryingFailed = this.facade.retryingFailed;
  protected readonly refreshingParkIds = this.facade.refreshingParkIds;
  protected readonly isRunActive = this.facade.isRunActive;

  constructor(private readonly facade: AdminParkWeatherStateFacade) {
  }

  ngOnInit(): void {
    this.facade.load();
  }

  protected refresh(): void {
    this.facade.load();
  }

  protected startManualRefresh(): void {
    this.facade.startManualRefresh();
  }

  protected retryFailed(): void {
    this.facade.retryFailed();
  }

  protected refreshPark(item: ParkWeatherRunItem): void {
    this.facade.refreshPark(item.parkId);
  }

  protected isRefreshingPark(parkId: string): boolean {
    return this.refreshingParkIds().has(parkId);
  }

  protected statusLabelKey(status: string | null | undefined): string {
    switch (status) {
      case 'Queued':
        return 'admin.parkWeather.statuses.queued';
      case 'Running':
        return 'admin.parkWeather.statuses.running';
      case 'Completed':
        return 'admin.parkWeather.statuses.completed';
      case 'CompletedWithFailures':
        return 'admin.parkWeather.statuses.completedWithFailures';
      case 'Failed':
        return 'admin.parkWeather.statuses.failed';
      case 'Skipped':
        return 'admin.parkWeather.statuses.skipped';
      default:
        return status?.trim() || 'admin.parkWeather.values.empty';
    }
  }

  protected messageLabelKey(message: string | null | undefined): string {
    switch (message?.trim()) {
      case 'Weather refresh queued.':
        return 'admin.parkWeather.messages.queued';
      case 'Weather refresh running.':
        return 'admin.parkWeather.messages.running';
      case 'Weather refresh completed.':
        return 'admin.parkWeather.messages.completed';
      case 'Weather refresh completed with failures.':
        return 'admin.parkWeather.messages.completedWithFailures';
      case 'Weather refresh canceled.':
        return 'admin.parkWeather.messages.canceled';
      case 'Automatic weather refresh skipped because a manual refresh already covers this cycle.':
        return 'admin.parkWeather.messages.skippedByManual';
      default:
        return message?.trim() || 'admin.parkWeather.values.empty';
    }
  }

  protected runProgressPercent(run: ParkWeatherRun | null): number {
    if (!run || run.totalParkCount <= 0) {
      return 0;
    }

    const doneCount: number = run.succeededParkCount + run.failedParkCount + run.skippedParkCount;
    return Math.min(100, Math.round((doneCount / run.totalParkCount) * 100));
  }

  protected statusSeverity(status: string | null | undefined): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' {
    switch (status) {
      case 'Completed':
        return 'success';
      case 'Queued':
      case 'Running':
        return 'info';
      case 'CompletedWithFailures':
      case 'Skipped':
        return 'warn';
      case 'Failed':
        return 'danger';
      default:
        return 'secondary';
    }
  }
}
