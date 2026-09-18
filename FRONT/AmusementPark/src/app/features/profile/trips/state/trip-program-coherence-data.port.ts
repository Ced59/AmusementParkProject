import { inject, InjectionToken } from '@angular/core';

import { TripProgramCoherenceApiService } from '@data-access/trips/trip-program-coherence-api.service';

export interface TripProgramCoherenceDataPort extends Pick<TripProgramCoherenceApiService, 'get'> {
}

export const TRIP_PROGRAM_COHERENCE_DATA_PORT = new InjectionToken<TripProgramCoherenceDataPort>(
  'TRIP_PROGRAM_COHERENCE_DATA_PORT',
  { providedIn: 'root', factory: (): TripProgramCoherenceDataPort => inject(TripProgramCoherenceApiService) }
);
