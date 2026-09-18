import { inject, InjectionToken } from '@angular/core';

import { TripPreferencesApiService } from '@data-access/trips/trip-preferences-api.service';

export interface TripPreferencesDataPort extends Pick<
  TripPreferencesApiService,
  'getMine' | 'set' | 'setBatch'
> {
}

export const TRIP_PREFERENCES_DATA_PORT = new InjectionToken<TripPreferencesDataPort>(
  'TRIP_PREFERENCES_DATA_PORT',
  { providedIn: 'root', factory: (): TripPreferencesDataPort => inject(TripPreferencesApiService) }
);
