import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  SetTripItemDecisionRequest,
  TripPreferenceSummary
} from '@app/models/trips/trip.models';
import { environment } from '../../../environments/environment';
import { TRIP_API_ENDPOINTS } from './trip-api-endpoints';

@Injectable({ providedIn: 'root' })
export class TripPreferenceSummaryApiService {
  constructor(private readonly http: HttpClient) {
  }

  get(tripPlanId: string): Observable<TripPreferenceSummary> {
    return this.http.get<TripPreferenceSummary>(
      `${environment.apiBaseUrl}${TRIP_API_ENDPOINTS.preferenceSummary(tripPlanId)}`,
      { transferCache: false }
    );
  }

  setDecision(
    tripPlanId: string,
    parkItemId: string,
    request: SetTripItemDecisionRequest
  ): Observable<TripPreferenceSummary> {
    return this.http.put<TripPreferenceSummary>(
      `${environment.apiBaseUrl}${TRIP_API_ENDPOINTS.preferenceDecision(tripPlanId, parkItemId)}`,
      request
    );
  }
}
