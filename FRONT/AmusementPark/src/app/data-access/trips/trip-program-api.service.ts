import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  AddTripParkCandidateRequest,
  ChangeTripParkCandidateStateRequest,
  MoveTripParkCandidateRequest,
  PutTripDayPlanRequest,
  TripDayPlan,
  TripParkCandidate,
  TripProgram
} from '@app/models/trips/trip.models';
import { environment } from '../../../environments/environment';
import { TRIP_API_ENDPOINTS } from './trip-api-endpoints';

@Injectable({ providedIn: 'root' })
export class TripProgramApiService {
  constructor(private readonly http: HttpClient) {
  }

  get(tripPlanId: string): Observable<TripProgram> {
    return this.http.get<TripProgram>(
      `${environment.apiBaseUrl}${TRIP_API_ENDPOINTS.program(tripPlanId)}`,
      { transferCache: false }
    );
  }

  addPark(
    tripPlanId: string,
    request: AddTripParkCandidateRequest,
    idempotencyKey: string
  ): Observable<TripParkCandidate> {
    const headers: HttpHeaders = new HttpHeaders({ 'Idempotency-Key': idempotencyKey });
    return this.http.post<TripParkCandidate>(
      `${environment.apiBaseUrl}${TRIP_API_ENDPOINTS.parks(tripPlanId)}`,
      request,
      { headers }
    );
  }

  changeParkState(
    tripPlanId: string,
    candidateId: string,
    request: ChangeTripParkCandidateStateRequest
  ): Observable<TripParkCandidate> {
    return this.http.post<TripParkCandidate>(
      `${environment.apiBaseUrl}${TRIP_API_ENDPOINTS.parkState(tripPlanId, candidateId)}`,
      request
    );
  }

  movePark(
    tripPlanId: string,
    candidateId: string,
    request: MoveTripParkCandidateRequest
  ): Observable<TripProgram> {
    return this.http.post<TripProgram>(
      `${environment.apiBaseUrl}${TRIP_API_ENDPOINTS.parkMove(tripPlanId, candidateId)}`,
      request
    );
  }

  putDay(tripPlanId: string, localDate: string, request: PutTripDayPlanRequest): Observable<TripDayPlan> {
    return this.http.put<TripDayPlan>(
      `${environment.apiBaseUrl}${TRIP_API_ENDPOINTS.day(tripPlanId, localDate)}`,
      request
    );
  }
}
