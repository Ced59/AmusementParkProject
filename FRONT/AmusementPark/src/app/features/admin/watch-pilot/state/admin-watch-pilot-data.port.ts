import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import {
  WatchPilotMetricsQuery,
  WatchPilotMetricsResult
} from '@app/models/admin/watch-pilot/watch-pilot-metrics.models';
import { AdminWatchPilotApiService } from '@data-access/admin/admin-watch-pilot-api.service';

export interface AdminWatchPilotDataPort {
  getMetrics(query: WatchPilotMetricsQuery): Observable<WatchPilotMetricsResult>;
}

export const ADMIN_WATCH_PILOT_DATA_PORT = new InjectionToken<AdminWatchPilotDataPort>(
  'ADMIN_WATCH_PILOT_DATA_PORT',
  {
    providedIn: 'root',
    factory: (): AdminWatchPilotDataPort => inject(AdminWatchPilotApiService)
  }
);
