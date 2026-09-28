import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  CreateLiveTargetMappingCandidateRequest,
  LiveTargetMapping,
  LiveTargetMappingPage,
  LiveTargetMappingQuery,
  ReviewLiveTargetMappingRequest
} from '@app/models/admin/live-data/live-target-mapping.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AdminLiveTargetMappingsApiService {
  private readonly endpoint: string = `${environment.apiBaseUrl}admin/live/mappings`;

  constructor(private readonly http: HttpClient) {
  }

  search(query: LiveTargetMappingQuery): Observable<LiveTargetMappingPage> {
    let params: HttpParams = new HttpParams()
      .set('page', query.page ?? 1)
      .set('pageSize', query.pageSize ?? 25);
    params = this.setOptional(params, 'sourceId', query.sourceId);
    params = this.setOptional(params, 'status', query.status);
    params = this.setOptional(params, 'targetType', query.targetType);
    params = this.setOptional(params, 'search', query.search);
    return this.http.get<LiveTargetMappingPage>(this.endpoint, { params });
  }

  createCandidate(
    request: CreateLiveTargetMappingCandidateRequest
  ): Observable<LiveTargetMapping> {
    return this.http.post<LiveTargetMapping>(`${this.endpoint}/candidates`, request);
  }

  review(
    mappingId: string,
    request: ReviewLiveTargetMappingRequest
  ): Observable<LiveTargetMapping> {
    return this.http.post<LiveTargetMapping>(
      `${this.endpoint}/${encodeURIComponent(mappingId)}/review`,
      request
    );
  }

  private setOptional(
    params: HttpParams,
    key: string,
    value: string | null | undefined
  ): HttpParams {
    return value?.trim() ? params.set(key, value.trim()) : params;
  }
}
