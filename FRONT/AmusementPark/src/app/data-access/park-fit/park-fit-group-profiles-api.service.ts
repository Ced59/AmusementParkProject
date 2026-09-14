import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ParkFitGroupProfileDraft } from '@app/models/park-fit/park-fit-group-profile-draft.model';
import { ParkFitGroupProfileExport } from '@app/models/park-fit/park-fit-group-profile-export.model';
import { ParkFitGroupProfile } from '@app/models/park-fit/park-fit-group-profile.model';
import { environment } from '../../../environments/environment';
import { PARK_FIT_GROUP_PROFILES_API_ENDPOINTS } from './park-fit-group-profiles-api-endpoints';

@Injectable({ providedIn: 'root' })
export class ParkFitGroupProfilesApiService {
  constructor(private readonly http: HttpClient) {
  }

  listMine(): Observable<ParkFitGroupProfile[]> {
    return this.http.get<ParkFitGroupProfile[]>(
      `${environment.apiBaseUrl}${PARK_FIT_GROUP_PROFILES_API_ENDPOINTS.collection}`
    );
  }

  create(draft: ParkFitGroupProfileDraft): Observable<ParkFitGroupProfile> {
    return this.http.post<ParkFitGroupProfile>(
      `${environment.apiBaseUrl}${PARK_FIT_GROUP_PROFILES_API_ENDPOINTS.collection}`,
      draft
    );
  }

  update(
    profileId: string,
    expectedVersion: number,
    draft: ParkFitGroupProfileDraft
  ): Observable<ParkFitGroupProfile> {
    return this.http.put<ParkFitGroupProfile>(
      `${environment.apiBaseUrl}${PARK_FIT_GROUP_PROFILES_API_ENDPOINTS.profile(profileId)}`,
      { ...draft, expectedVersion }
    );
  }

  delete(profileId: string, expectedVersion: number): Observable<void> {
    const params: HttpParams = new HttpParams().set('expectedVersion', expectedVersion);
    return this.http.delete<void>(
      `${environment.apiBaseUrl}${PARK_FIT_GROUP_PROFILES_API_ENDPOINTS.profile(profileId)}`,
      { params }
    );
  }

  exportMine(): Observable<ParkFitGroupProfileExport> {
    return this.http.get<ParkFitGroupProfileExport>(
      `${environment.apiBaseUrl}${PARK_FIT_GROUP_PROFILES_API_ENDPOINTS.export}`
    );
  }
}
