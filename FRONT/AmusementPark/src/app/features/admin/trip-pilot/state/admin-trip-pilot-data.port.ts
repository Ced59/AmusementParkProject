import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import { TripPilotMetricsResult } from '@app/models/admin/trip-pilot/trip-pilot-metrics.models';
import { AdminTripPilotApiService } from '@data-access/admin/admin-trip-pilot-api.service';

export interface AdminTripPilotDataPort {
  getMetrics(): Observable<TripPilotMetricsResult>;
}

export const ADMIN_TRIP_PILOT_DATA_PORT = new InjectionToken<AdminTripPilotDataPort>(
  'ADMIN_TRIP_PILOT_DATA_PORT',
  {
    providedIn: 'root',
    factory: (): AdminTripPilotDataPort => inject(AdminTripPilotApiService)
  }
);
