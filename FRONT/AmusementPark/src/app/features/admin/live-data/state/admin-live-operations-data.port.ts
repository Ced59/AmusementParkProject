import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import {
  LiveOperationalScope,
  LiveOperationsDashboard,
  LiveQualityReplay,
  LiveWaitForecastBacktest,
  UpdateLiveOperationalControlRequest
} from '@app/models/admin/live-data/live-operations.models';
import { AdminLiveOperationsApiService } from '@data-access/admin/admin-live-operations-api.service';

export interface AdminLiveOperationsDataPort {
  getDashboard(): Observable<LiveOperationsDashboard>;
  updateControl(request: UpdateLiveOperationalControlRequest): Observable<LiveOperationalScope>;
  replayQuarantine(maximumCount: number): Observable<LiveQualityReplay>;
  getForecastBacktest(parkItemId: string): Observable<LiveWaitForecastBacktest>;
}

export const ADMIN_LIVE_OPERATIONS_DATA_PORT = new InjectionToken<AdminLiveOperationsDataPort>(
  'ADMIN_LIVE_OPERATIONS_DATA_PORT',
  {
    providedIn: 'root',
    factory: (): AdminLiveOperationsDataPort => inject(AdminLiveOperationsApiService)
  }
);
