import { inject, InjectionToken } from '@angular/core';

import { ParkFitApiService } from '@data-access/park-fit/park-fit-api.service';

export interface ParkFitSearchDataPort extends Pick<ParkFitApiService, 'search'> {
}

export const PARK_FIT_SEARCH_DATA_PORT = new InjectionToken<ParkFitSearchDataPort>(
  'PARK_FIT_SEARCH_DATA_PORT',
  {
    providedIn: 'root',
    factory: () => inject(ParkFitApiService)
  }
);
