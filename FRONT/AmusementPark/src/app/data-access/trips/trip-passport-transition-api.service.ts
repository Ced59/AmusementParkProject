import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  ConfirmTripPassportTransitionRequest,
  ConfirmTripPassportTransitionResult,
  TripPassportTransition
} from '@app/models/trips/trip-passport-transition.models';
import { environment } from '../../../environments/environment';
import { TRIP_API_ENDPOINTS } from './trip-api-endpoints';

@Injectable({ providedIn: 'root' })
export class TripPassportTransitionApiService {
  constructor(private readonly http: HttpClient) {
  }

  get(tripPlanId: string): Observable<TripPassportTransition> {
    return this.http.get<TripPassportTransition>(
      `${environment.apiBaseUrl}${TRIP_API_ENDPOINTS.passportTransition(tripPlanId)}`,
      { transferCache: false }
    );
  }

  confirm(
    tripPlanId: string,
    localDate: string,
    request: ConfirmTripPassportTransitionRequest
  ): Observable<ConfirmTripPassportTransitionResult> {
    return this.http.post<ConfirmTripPassportTransitionResult>(
      `${environment.apiBaseUrl}${TRIP_API_ENDPOINTS.confirmPassportTransition(tripPlanId, localDate)}`,
      request
    );
  }
}
