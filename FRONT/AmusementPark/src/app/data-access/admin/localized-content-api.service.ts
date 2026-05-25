import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { PagedCollectionResponse } from '../shared/api-helpers';

export type LocalizedContentEntityType = 'park' | 'parkZone' | 'parkItem' | 'parkOperator' | 'parkFounder' | 'attractionManufacturer' | 'image' | 'imageTag';

export interface LocalizedContentTarget {
  readonly entityType: LocalizedContentEntityType;
  readonly entityId: string;
  readonly label: string;
  readonly context?: string | null;
  readonly supportedFields: readonly string[];
}

export interface LocalizedContentApplyResult {
  readonly entityType: LocalizedContentEntityType;
  readonly entityId: string;
  readonly updatedFields: readonly string[];
  readonly updatedLocalizedValueCount: number;
}

@Injectable({
  providedIn: 'root'
})
export class LocalizedContentApiService {
  private readonly apiBaseUrl: string = environment.apiBaseUrl.endsWith('/')
    ? environment.apiBaseUrl
    : `${environment.apiBaseUrl}/`;

  private readonly jsonHttpOptions = {
    headers: new HttpHeaders({
      'Content-Type': 'application/json'
    })
  };

  constructor(private readonly http: HttpClient) {
  }

  searchTargets(entityType: LocalizedContentEntityType, search: string, page: number = 1, size: number = 20): Observable<PagedCollectionResponse<LocalizedContentTarget>> {
    const url: string = `${this.apiBaseUrl}admin/localized-content/targets`;
    let params: HttpParams = new HttpParams()
      .set('entityType', entityType)
      .set('page', String(page))
      .set('size', String(size));

    const normalizedSearch: string = search.trim();
    if (normalizedSearch.length > 0) {
      params = params.set('search', normalizedSearch);
    }

    return this.http.get<PagedCollectionResponse<LocalizedContentTarget>>(url, { params });
  }

  applyJson(entityType: LocalizedContentEntityType, entityId: string, json: unknown): Observable<LocalizedContentApplyResult> {
    const url: string = `${this.apiBaseUrl}admin/localized-content/${encodeURIComponent(entityType)}/${encodeURIComponent(entityId)}`;
    return this.http.patch<LocalizedContentApplyResult>(url, { json }, this.jsonHttpOptions);
  }
}
