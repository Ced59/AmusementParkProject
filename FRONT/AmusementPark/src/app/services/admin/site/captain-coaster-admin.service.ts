import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import {
  AdminDataSourceSummaryResponse,
  CaptainCoasterComparisonResultResponse,
  CaptainCoasterSettingsResponse,
  CaptainCoasterSyncSessionResponse,
  UpdateCaptainCoasterSettingsRequest
} from '../../../models/admin/site/captain-coaster-admin.models';

@Injectable({
  providedIn: 'root'
})
export class CaptainCoasterAdminService {
  constructor(private readonly http: HttpClient) {
  }

  getSources(): Observable<AdminDataSourceSummaryResponse[]> {
    return this.http.get<AdminDataSourceSummaryResponse[]>(`${environment.apiBaseUrl}admin/data/sources`);
  }

  getSettings(): Observable<CaptainCoasterSettingsResponse> {
    return this.http.get<CaptainCoasterSettingsResponse>(`${environment.apiBaseUrl}admin/data/captain-coaster/settings`);
  }

  updateSettings(payload: UpdateCaptainCoasterSettingsRequest): Observable<CaptainCoasterSettingsResponse> {
    return this.http.put<CaptainCoasterSettingsResponse>(`${environment.apiBaseUrl}admin/data/captain-coaster/settings`, payload);
  }

  getLatestSession(): Observable<CaptainCoasterSyncSessionResponse> {
    return this.http.get<CaptainCoasterSyncSessionResponse>(`${environment.apiBaseUrl}admin/data/captain-coaster/imports/latest`);
  }

  importJson(files: File[]): Observable<CaptainCoasterSyncSessionResponse> {
    const formData: FormData = new FormData();
    for (const file of files) {
      formData.append('files', file, file.name);
    }

    return this.http.post<CaptainCoasterSyncSessionResponse>(`${environment.apiBaseUrl}admin/data/captain-coaster/import-json`, formData);
  }

  getComparisonResults(sessionId?: string | null): Observable<CaptainCoasterComparisonResultResponse[]> {
    const suffix: string = sessionId ? `?sessionId=${encodeURIComponent(sessionId)}` : '';
    return this.http.get<CaptainCoasterComparisonResultResponse[]>(`${environment.apiBaseUrl}admin/data/captain-coaster/comparison-results${suffix}`);
  }

  applyComparisonResults(ids: string[]): Observable<{ appliedCount: number }> {
    return this.http.post<{ appliedCount: number }>(`${environment.apiBaseUrl}admin/data/captain-coaster/apply`, { comparisonResultIds: ids });
  }
}
