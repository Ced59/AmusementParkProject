import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import {
  CaptainCoasterComparisonPagedResponse,
  CaptainCoasterSessionResponse,
  CaptainCoasterStatusResponse,
  ComparisonFilters
} from '../../../models/admin/data/data-management.models';

@Injectable({
  providedIn: 'root'
})
export class CaptainCoasterDataService {

  private readonly baseUrl: string = `${environment.apiBaseUrl}admin/data/captain-coaster`;

  constructor(private readonly http: HttpClient) {}

  getStatus(): Observable<CaptainCoasterStatusResponse> {
    return this.http.get<CaptainCoasterStatusResponse>(`${this.baseUrl}/status`);
  }

  getLatestSession(): Observable<CaptainCoasterSessionResponse> {
    return this.http.get<CaptainCoasterSessionResponse>(`${this.baseUrl}/sessions/latest`);
  }

  importFromFiles(parksFile: File, coastersFile: File): Observable<CaptainCoasterSessionResponse> {
    const formData: FormData = new FormData();
    formData.append('parksFile', parksFile, parksFile.name);
    formData.append('coastersFile', coastersFile, coastersFile.name);
    return this.http.post<CaptainCoasterSessionResponse>(`${this.baseUrl}/import`, formData);
  }

  getComparisonResults(
    sessionId: string | null | undefined,
    filters: ComparisonFilters,
    page: number,
    pageSize: number
  ): Observable<CaptainCoasterComparisonPagedResponse> {
    let params: HttpParams = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (sessionId) {
      params = params.set('sessionId', sessionId);
    }
    if (filters.entityType) {
      params = params.set('entityType', filters.entityType);
    }
    if (filters.changeType) {
      params = params.set('changeType', filters.changeType);
    }
    if (filters.isApplied !== null && filters.isApplied !== undefined) {
      params = params.set('isApplied', filters.isApplied.toString());
    }

    return this.http.get<CaptainCoasterComparisonPagedResponse>(
      `${this.baseUrl}/comparison-results`,
      { params }
    );
  }

  applySelectedIds(ids: string[]): Observable<{ appliedCount: number }> {
    return this.http.post<{ appliedCount: number }>(`${this.baseUrl}/apply`, {
      comparisonResultIds: ids,
      applyAll: false
    });
  }

  applyAll(sessionId: string | null, entityTypeFilter: string | null, changeTypeFilter: string | null): Observable<{ appliedCount: number }> {
    return this.http.post<{ appliedCount: number }>(`${this.baseUrl}/apply`, {
      comparisonResultIds: [],
      applyAll: true,
      sessionId,
      entityTypeFilter,
      changeTypeFilter
    });
  }
}
