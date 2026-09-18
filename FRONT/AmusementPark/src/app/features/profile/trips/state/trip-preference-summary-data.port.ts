import { inject, InjectionToken } from '@angular/core';

import { TripPreferenceSummaryApiService } from '@data-access/trips/trip-preference-summary-api.service';

export interface TripPreferenceSummaryDataPort extends Pick<
  TripPreferenceSummaryApiService,
  'get' | 'setDecision'
> {
}

export const TRIP_PREFERENCE_SUMMARY_DATA_PORT = new InjectionToken<TripPreferenceSummaryDataPort>(
  'TRIP_PREFERENCE_SUMMARY_DATA_PORT',
  { providedIn: 'root', factory: (): TripPreferenceSummaryDataPort => inject(TripPreferenceSummaryApiService) }
);
