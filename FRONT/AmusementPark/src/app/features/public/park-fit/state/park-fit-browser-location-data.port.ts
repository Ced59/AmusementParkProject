import { InjectionToken, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ParkFitBrowserLocationService } from '@data-access/park-fit/park-fit-browser-location.service';
import { ParkFitSearchOrigin } from '../models/park-fit-search-form.models';

export interface ParkFitBrowserLocationDataPort {
  requestCurrentPosition(): Observable<ParkFitSearchOrigin>;
}

export const PARK_FIT_BROWSER_LOCATION_DATA_PORT =
  new InjectionToken<ParkFitBrowserLocationDataPort>(
    'PARK_FIT_BROWSER_LOCATION_DATA_PORT',
    {
      providedIn: 'root',
      factory: () => inject(ParkFitBrowserLocationService)
    }
  );
