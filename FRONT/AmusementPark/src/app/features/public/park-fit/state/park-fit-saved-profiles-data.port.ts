import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import { ParkFitGroupProfile } from '@app/models/park-fit/park-fit-group-profile.model';
import { ParkFitGroupProfilesApiService } from '@data-access/park-fit/park-fit-group-profiles-api.service';

export interface ParkFitSavedProfilesDataPort {
  listMine(): Observable<ParkFitGroupProfile[]>;
}

export const PARK_FIT_SAVED_PROFILES_DATA_PORT =
  new InjectionToken<ParkFitSavedProfilesDataPort>(
    'PARK_FIT_SAVED_PROFILES_DATA_PORT',
    {
      providedIn: 'root',
      factory: (): ParkFitSavedProfilesDataPort => inject(ParkFitGroupProfilesApiService)
    }
  );
