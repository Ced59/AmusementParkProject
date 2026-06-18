import { HttpClient, HttpHeaders, HttpResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ParkGraphUpsertHistoryEntry, ParkGraphUpsertRequest, ParkGraphUpsertResult } from '@app/models/admin/park-graph-upsert.models';

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

  getHistory(targetParkId: string | null, limit: number): Observable<ParkGraphUpsertHistoryEntry[]> {
    const params: string[] = [`limit=${encodeURIComponent(limit)}`];
    if (targetParkId) {
      params.push(`targetParkId=${encodeURIComponent(targetParkId)}`);
    }

    const url: string = `${environment.apiBaseUrl}admin/park-graph-upserts/history?${params.join('&')}`;
    return this.http.get<ParkGraphUpsertHistoryEntry[]>(url);
  }

  downloadParkExport(parkId: string): Observable<HttpResponse<Blob>> {
    const url: string = `${environment.apiBaseUrl}admin/park-graph-upserts/parks/${encodeURIComponent(parkId)}/export`;
    return this.http.get(url, {
      observe: 'response',
      responseType: 'blob'
    });
  }
}
