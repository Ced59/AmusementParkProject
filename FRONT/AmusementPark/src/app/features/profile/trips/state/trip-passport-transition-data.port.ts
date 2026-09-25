import { inject, InjectionToken } from '@angular/core';

import { TripPassportTransitionApiService } from '@data-access/trips/trip-passport-transition-api.service';

export interface TripPassportTransitionDataPort extends Pick<
  TripPassportTransitionApiService,
  'get' | 'confirm'
> {
}

export const TRIP_PASSPORT_TRANSITION_DATA_PORT =
  new InjectionToken<TripPassportTransitionDataPort>(
    'TRIP_PASSPORT_TRANSITION_DATA_PORT',
    {
      providedIn: 'root',
      factory: (): TripPassportTransitionDataPort => inject(TripPassportTransitionApiService)
    }
  );
