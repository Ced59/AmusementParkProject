import { HttpClient, HttpHeaders, HttpResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import {
  CreatePassportRideOccurrencesBatchRequest,
  PassportRideOccurrence,
  PassportRideOccurrenceMutationResult,
  PassportRideOccurrencePage,
  ReorderPassportRideOccurrenceRequest,
  UpsertPassportRideAssessmentRequest,
  UpdatePassportRideOccurrenceRequest
} from '@app/models/passport/passport-ride-occurrence.models';
import { environment } from '../../../environments/environment';
import { PASSPORT_RIDE_OCCURRENCES_API_ENDPOINTS } from './passport-ride-occurrences-api-endpoints';

@Injectable({
  providedIn: 'root'
})
export class PassportRideOccurrencesApiService {
  constructor(private readonly http: HttpClient) {
  }

  validateTargets(parkId: string, parkItemIds: string[]): Observable<void> {
    const url: string = `${environment.apiBaseUrl}${PASSPORT_RIDE_OCCURRENCES_API_ENDPOINTS.validateTargets}`;
    return this.http.post<void>(url, { parkId, parkItemIds });
  }

  list(visitId: string, cursor: string | null = null, limit: number = 50): Observable<PassportRideOccurrencePage> {
    const url: string = `${environment.apiBaseUrl}${PASSPORT_RIDE_OCCURRENCES_API_ENDPOINTS.list(visitId, limit, cursor)}`;
    return this.http.get<PassportRideOccurrencePage>(url, { transferCache: false });
  }

  get(visitId: string, occurrenceId: string): Observable<PassportRideOccurrence> {
    const url: string = `${environment.apiBaseUrl}${PASSPORT_RIDE_OCCURRENCES_API_ENDPOINTS.get(visitId, occurrenceId)}`;
    return this.http.get<PassportRideOccurrence>(url, { transferCache: false });
  }

  addBatch(
    visitId: string,
    request: CreatePassportRideOccurrencesBatchRequest,
    idempotencyKey: string
  ): Observable<PassportRideOccurrenceMutationResult> {
    const url: string = `${environment.apiBaseUrl}${PASSPORT_RIDE_OCCURRENCES_API_ENDPOINTS.addBatch(visitId)}`;
    return this.http.post<PassportRideOccurrence[]>(url, request, {
      headers: this.idempotencyHeaders(idempotencyKey),
      observe: 'response'
    }).pipe(map((response: HttpResponse<PassportRideOccurrence[]>): PassportRideOccurrenceMutationResult =>
      this.toMutationResult(response, response.body ?? [])));
  }

  importBatch(
    visitId: string,
    request: CreatePassportRideOccurrencesBatchRequest,
    idempotencyKey: string
  ): Observable<PassportRideOccurrenceMutationResult> {
    const url: string = `${environment.apiBaseUrl}${PASSPORT_RIDE_OCCURRENCES_API_ENDPOINTS.importBatch(visitId)}`;
    return this.http.post<PassportRideOccurrence[]>(url, request, {
      headers: this.idempotencyHeaders(idempotencyKey),
      observe: 'response'
    }).pipe(map((response: HttpResponse<PassportRideOccurrence[]>): PassportRideOccurrenceMutationResult =>
      this.toMutationResult(response, response.body ?? [])));
  }

  update(
    visitId: string,
    occurrenceId: string,
    request: UpdatePassportRideOccurrenceRequest
  ): Observable<PassportRideOccurrence> {
    const url: string = `${environment.apiBaseUrl}${PASSPORT_RIDE_OCCURRENCES_API_ENDPOINTS.update(visitId, occurrenceId)}`;
    return this.http.patch<PassportRideOccurrence>(url, request);
  }

  delete(visitId: string, occurrenceId: string, expectedVersion: number): Observable<void> {
    const url: string = `${environment.apiBaseUrl}${PASSPORT_RIDE_OCCURRENCES_API_ENDPOINTS.delete(visitId, occurrenceId, expectedVersion)}`;
    return this.http.delete<void>(url);
  }

  reorder(
    visitId: string,
    request: ReorderPassportRideOccurrenceRequest,
    idempotencyKey: string
  ): Observable<PassportRideOccurrenceMutationResult> {
    const url: string = `${environment.apiBaseUrl}${PASSPORT_RIDE_OCCURRENCES_API_ENDPOINTS.reorder(visitId)}`;
    return this.http.post<PassportRideOccurrence>(url, request, {
      headers: this.idempotencyHeaders(idempotencyKey),
      observe: 'response'
    }).pipe(map((response: HttpResponse<PassportRideOccurrence>): PassportRideOccurrenceMutationResult =>
      this.toMutationResult(response, response.body ? [response.body] : [])));
  }

  upsertAssessment(
    occurrenceId: string,
    request: UpsertPassportRideAssessmentRequest
  ): Observable<PassportRideOccurrence> {
    const url: string = `${environment.apiBaseUrl}${PASSPORT_RIDE_OCCURRENCES_API_ENDPOINTS.assessment(occurrenceId)}`;
    return this.http.put<PassportRideOccurrence>(url, request);
  }

  deleteAssessment(occurrenceId: string, expectedVersion: number): Observable<PassportRideOccurrence> {
    const url: string = `${environment.apiBaseUrl}${PASSPORT_RIDE_OCCURRENCES_API_ENDPOINTS.deleteAssessment(occurrenceId, expectedVersion)}`;
    return this.http.delete<PassportRideOccurrence>(url);
  }

  private idempotencyHeaders(idempotencyKey: string): HttpHeaders {
    return new HttpHeaders({
      'Content-Type': 'application/json',
      'Idempotency-Key': idempotencyKey
    });
  }

  private toMutationResult(
    response: HttpResponse<unknown>,
    occurrences: PassportRideOccurrence[]
  ): PassportRideOccurrenceMutationResult {
    return {
      occurrences,
      wasReplayed: response.headers.get('Idempotency-Replayed') === 'true',
      wasOrderNormalized: response.headers.get('Ride-Order-Normalized') === 'true'
    };
  }
}
