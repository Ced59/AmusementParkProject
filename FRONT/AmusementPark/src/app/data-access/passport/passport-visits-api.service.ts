import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { CreatePassportVisitRequest, PassportVisit } from '@app/models/passport/passport-visit.models';
import { environment } from '../../../environments/environment';
import { PASSPORT_VISITS_API_ENDPOINTS } from './passport-visits-api-endpoints';

@Injectable({
  providedIn: 'root'
})
export class PassportVisitsApiService {
  constructor(private readonly http: HttpClient) {
  }

  createVisit(request: CreatePassportVisitRequest, idempotencyKey: string): Observable<PassportVisit> {
    const url: string = `${environment.apiBaseUrl}${PASSPORT_VISITS_API_ENDPOINTS.create}`;
    const headers: HttpHeaders = new HttpHeaders({
      'Content-Type': 'application/json',
      'Idempotency-Key': idempotencyKey
    });

    return this.http.post<PassportVisit>(url, request, { headers });
  }

  getVisit(visitId: string): Observable<PassportVisit> {
    const url: string = `${environment.apiBaseUrl}${PASSPORT_VISITS_API_ENDPOINTS.getById(visitId)}`;
    return this.http.get<PassportVisit>(url, { transferCache: false });
  }
}
