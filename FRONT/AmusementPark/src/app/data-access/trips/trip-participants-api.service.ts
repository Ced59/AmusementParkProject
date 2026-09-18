import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  TripDelegatedRole,
  TripParticipantList
} from '@app/models/trips/trip-participant.models';
import { environment } from '../../../environments/environment';
import { TRIP_API_ENDPOINTS } from './trip-api-endpoints';

@Injectable({ providedIn: 'root' })
export class TripParticipantsApiService {
  constructor(private readonly http: HttpClient) {
  }

  list(tripPlanId: string): Observable<TripParticipantList> {
    return this.http.get<TripParticipantList>(
      `${environment.apiBaseUrl}${TRIP_API_ENDPOINTS.participants(tripPlanId)}`,
      { transferCache: false }
    );
  }

  changeRole(
    tripPlanId: string,
    memberId: string,
    role: TripDelegatedRole,
    expectedVersion: number
  ): Observable<TripParticipantList> {
    return this.http.patch<TripParticipantList>(
      `${environment.apiBaseUrl}${TRIP_API_ENDPOINTS.participantRole(tripPlanId, memberId)}`,
      { role, expectedVersion }
    );
  }

  transferOwnership(
    tripPlanId: string,
    memberId: string,
    previousOwnerRole: TripDelegatedRole,
    expectedVersion: number
  ): Observable<TripParticipantList> {
    return this.http.post<TripParticipantList>(
      `${environment.apiBaseUrl}${TRIP_API_ENDPOINTS.transferOwnership(tripPlanId, memberId)}`,
      { previousOwnerRole, expectedVersion }
    );
  }

  leave(tripPlanId: string, expectedVersion: number): Observable<void> {
    const params: HttpParams = new HttpParams().set('expectedVersion', expectedVersion);
    return this.http.delete<void>(
      `${environment.apiBaseUrl}${TRIP_API_ENDPOINTS.leaveTrip(tripPlanId)}`,
      { params }
    );
  }
}
