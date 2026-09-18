import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  CreateTripInvitationRequest,
  TripInvitationCreation,
  TripInvitationPreview,
  TripInvitationSummary
} from '@app/models/trips/trip-invitation.models';
import { environment } from '../../../environments/environment';
import { TRIP_API_ENDPOINTS } from './trip-api-endpoints';

@Injectable({ providedIn: 'root' })
export class TripInvitationsApiService {
  constructor(private readonly http: HttpClient) {
  }

  list(tripPlanId: string): Observable<TripInvitationSummary[]> {
    return this.http.get<TripInvitationSummary[]>(
      `${environment.apiBaseUrl}${TRIP_API_ENDPOINTS.invitations(tripPlanId)}`,
      { transferCache: false }
    );
  }

  create(
    tripPlanId: string,
    request: CreateTripInvitationRequest,
    idempotencyKey: string
  ): Observable<TripInvitationCreation> {
    const headers: HttpHeaders = new HttpHeaders({ 'Idempotency-Key': idempotencyKey });
    return this.http.post<TripInvitationCreation>(
      `${environment.apiBaseUrl}${TRIP_API_ENDPOINTS.invitations(tripPlanId)}`,
      request,
      { headers }
    );
  }

  revoke(
    tripPlanId: string,
    invitationId: string,
    expectedVersion: number,
    idempotencyKey: string
  ): Observable<void> {
    const headers: HttpHeaders = new HttpHeaders({ 'Idempotency-Key': idempotencyKey });
    const params: HttpParams = new HttpParams().set('expectedVersion', expectedVersion);
    return this.http.delete<void>(
      `${environment.apiBaseUrl}${TRIP_API_ENDPOINTS.invitation(tripPlanId, invitationId)}`,
      { headers, params }
    );
  }

  preview(token: string): Observable<TripInvitationPreview> {
    return this.http.get<TripInvitationPreview>(
      `${environment.apiBaseUrl}${TRIP_API_ENDPOINTS.invitationPreview(token)}`,
      { transferCache: false }
    );
  }
}
