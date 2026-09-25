import { inject, InjectionToken } from '@angular/core';

import { TripActivityApiService } from '@data-access/trips/trip-activity-api.service';

export interface TripActivityDataPort extends Pick<TripActivityApiService, 'get'> {
}

export const TRIP_ACTIVITY_DATA_PORT = new InjectionToken<TripActivityDataPort>(
  'TRIP_ACTIVITY_DATA_PORT',
  { providedIn: 'root', factory: (): TripActivityDataPort => inject(TripActivityApiService) }
);
