import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  SetTripPlanDatesRequest,
  TripPlan,
  TripPlanWriteRequest
} from '@app/models/trips/trip.models';
import { environment } from '../../../environments/environment';
import { TRIP_API_ENDPOINTS } from './trip-api-endpoints';

@Injectable({ providedIn: 'root' })
export class TripPlansApiService {
  constructor(private readonly http: HttpClient) {
  }

  listMine(): Observable<TripPlan[]> {
    return this.http.get<TripPlan[]>(
      `${environment.apiBaseUrl}${TRIP_API_ENDPOINTS.plans}`,
      { transferCache: false }
    );
  }

  getMine(tripPlanId: string): Observable<TripPlan> {
    return this.http.get<TripPlan>(
      `${environment.apiBaseUrl}${TRIP_API_ENDPOINTS.plan(tripPlanId)}`,
      { transferCache: false }
    );
  }

  create(request: TripPlanWriteRequest, idempotencyKey: string): Observable<TripPlan> {
    const headers: HttpHeaders = new HttpHeaders({ 'Idempotency-Key': idempotencyKey });
    return this.http.post<TripPlan>(
      `${environment.apiBaseUrl}${TRIP_API_ENDPOINTS.plans}`,
      request,
      { headers }
    );
  }

  setDates(tripPlanId: string, request: SetTripPlanDatesRequest): Observable<TripPlan> {
    return this.http.post<TripPlan>(
      `${environment.apiBaseUrl}${TRIP_API_ENDPOINTS.dates(tripPlanId)}`,
      request
    );
  }

  delete(tripPlanId: string, expectedVersion: number): Observable<void> {
    const params: HttpParams = new HttpParams().set('expectedVersion', expectedVersion);
    return this.http.delete<void>(
      `${environment.apiBaseUrl}${TRIP_API_ENDPOINTS.plan(tripPlanId)}`,
      { params }
    );
  }
}
