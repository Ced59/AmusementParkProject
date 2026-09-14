import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import { AdminParkFitPilotApiService } from '@data-access/admin/admin-park-fit-pilot-api.service';
import {
  ParkFitPilotMetricsQuery,
  ParkFitPilotMetricsResult
} from '@app/models/admin/park-fit/park-fit-pilot-metrics.models';

export interface AdminParkFitPilotStateDataPort {
  getMetrics(query: ParkFitPilotMetricsQuery): Observable<ParkFitPilotMetricsResult>;
}

export const ADMIN_PARK_FIT_PILOT_STATE_DATA_PORT =
  new InjectionToken<AdminParkFitPilotStateDataPort>(
    'ADMIN_PARK_FIT_PILOT_STATE_DATA_PORT',
    {
      providedIn: 'root',
      factory: () => inject(AdminParkFitPilotApiService)
    }
  );
