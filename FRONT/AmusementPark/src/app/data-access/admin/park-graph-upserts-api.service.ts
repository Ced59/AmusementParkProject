import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ParkGraphUpsertRequest, ParkGraphUpsertResult } from '@app/models/admin/park-graph-upsert.models';

@Injectable({
  providedIn: 'root'
})
export class ParkGraphUpsertsApiService {
  private readonly jsonHttpOptions = {
    headers: new HttpHeaders({
      'Content-Type': 'application/json'
    })
  };

  constructor(private readonly http: HttpClient) {
  }

  preview(request: ParkGraphUpsertRequest): Observable<ParkGraphUpsertResult> {
    const url: string = `${environment.apiBaseUrl}admin/park-graph-upserts/preview`;
    return this.http.post<ParkGraphUpsertResult>(url, request, this.jsonHttpOptions);
  }

  apply(request: ParkGraphUpsertRequest): Observable<ParkGraphUpsertResult> {
    const url: string = `${environment.apiBaseUrl}admin/park-graph-upserts/apply`;
    return this.http.post<ParkGraphUpsertResult>(url, request, this.jsonHttpOptions);
  }
}
