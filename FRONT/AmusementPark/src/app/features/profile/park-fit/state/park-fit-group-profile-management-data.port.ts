import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import { ParkFitGroupProfileDraft } from '@app/models/park-fit/park-fit-group-profile-draft.model';
import { ParkFitGroupProfileExport } from '@app/models/park-fit/park-fit-group-profile-export.model';
import { ParkFitGroupProfile } from '@app/models/park-fit/park-fit-group-profile.model';
import { ParkFitGroupProfilesApiService } from '@data-access/park-fit/park-fit-group-profiles-api.service';

export interface ParkFitGroupProfileManagementDataPort {
  listMine(): Observable<ParkFitGroupProfile[]>;
  create(draft: ParkFitGroupProfileDraft): Observable<ParkFitGroupProfile>;
  update(
    profileId: string,
    expectedVersion: number,
    draft: ParkFitGroupProfileDraft
  ): Observable<ParkFitGroupProfile>;
  delete(profileId: string, expectedVersion: number): Observable<void>;
  exportMine(): Observable<ParkFitGroupProfileExport>;
}

export const PARK_FIT_GROUP_PROFILE_MANAGEMENT_DATA_PORT =
  new InjectionToken<ParkFitGroupProfileManagementDataPort>(
    'PARK_FIT_GROUP_PROFILE_MANAGEMENT_DATA_PORT',
    {
      providedIn: 'root',
      factory: (): ParkFitGroupProfileManagementDataPort => inject(ParkFitGroupProfilesApiService)
    }
  );
