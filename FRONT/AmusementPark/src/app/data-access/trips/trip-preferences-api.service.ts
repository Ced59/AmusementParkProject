import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  BulkSetTripItemPreferencesRequest,
  SetTripItemPreferenceRequest,
  TripPreferenceBoard
} from '@app/models/trips/trip.models';
import { environment } from '../../../environments/environment';
import { TRIP_API_ENDPOINTS } from './trip-api-endpoints';

@Injectable({ providedIn: 'root' })
export class TripPreferencesApiService {
  constructor(private readonly http: HttpClient) {
  }

  getMine(tripPlanId: string): Observable<TripPreferenceBoard> {
    return this.http.get<TripPreferenceBoard>(
      `${environment.apiBaseUrl}${TRIP_API_ENDPOINTS.preferences(tripPlanId)}`,
      { transferCache: false }
    );
  }

  set(
    tripPlanId: string,
    parkItemId: string,
    request: SetTripItemPreferenceRequest
  ): Observable<TripPreferenceBoard> {
    return this.http.put<TripPreferenceBoard>(
      `${environment.apiBaseUrl}${TRIP_API_ENDPOINTS.preference(tripPlanId, parkItemId)}`,
      request
    );
  }

  setBatch(
    tripPlanId: string,
    request: BulkSetTripItemPreferencesRequest
  ): Observable<TripPreferenceBoard> {
    return this.http.post<TripPreferenceBoard>(
      `${environment.apiBaseUrl}${TRIP_API_ENDPOINTS.preferenceBatch(tripPlanId)}`,
      request
    );
  }
}
