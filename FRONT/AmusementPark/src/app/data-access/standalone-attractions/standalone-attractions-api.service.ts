import { HttpClient, HttpContext, HttpParams, HttpResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { BulkAdministrationUpdateRequest, BulkAdministrationUpdateResult } from '@app/models/admin/admin-review-status';
import { StandaloneAttraction, StandaloneAttractionMigrationRequest } from '@app/models/standalone-attractions/standalone-attraction';
import { ParkItemType } from '@app/models/parks/park-item-type';
import { PagedResult } from '@shared/models/contracts';
import { PagedCollectionResponse, unwrapPagedCollection } from '@data-access/shared/api-helpers';

export interface StandaloneAttractionListFilters {
  search?: string | null;
  isVisible?: boolean | null;
  adminReviewStatus?: string | null;
  type?: ParkItemType | null;
  countryCode?: string | null;
  manufacturerId?: string | null;
  sortBy?: string | null;
  sortDirection?: 'asc' | 'desc' | null;
}

interface StandaloneAttractionsHttpOptions {
  context?: HttpContext;
}

@Injectable({
  providedIn: 'root'
})
export class StandaloneAttractionsApiService {
  private readonly baseUrl: string = `${environment.apiBaseUrl}standalone-attractions`;
  private readonly adminBaseUrl: string = `${environment.apiBaseUrl}admin/standalone-attractions`;
  private readonly upsertBaseUrl: string = `${environment.apiBaseUrl}admin/park-graph-upserts`;

  constructor(private readonly http: HttpClient) {
  }

  getPage(page: number, size: number, filters: StandaloneAttractionListFilters = {}): Observable<PagedResult<StandaloneAttraction>> {
    const params: HttpParams = this.buildListParams(page, size, filters);
    return this.http.get<PagedCollectionResponse<StandaloneAttraction>>(this.baseUrl, { params }).pipe(
      map((response: PagedCollectionResponse<StandaloneAttraction>) => unwrapPagedCollection<StandaloneAttraction>(response))
    );
  }

  getAdminPage(page: number, size: number, filters: StandaloneAttractionListFilters = {}): Observable<PagedResult<StandaloneAttraction>> {
    const params: HttpParams = this.buildListParams(page, size, filters);
    return this.http.get<PagedCollectionResponse<StandaloneAttraction>>(this.adminBaseUrl, { params }).pipe(
      map((response: PagedCollectionResponse<StandaloneAttraction>) => unwrapPagedCollection<StandaloneAttraction>(response))
    );
  }

  getById(id: string, options: StandaloneAttractionsHttpOptions = {}): Observable<StandaloneAttraction> {
    return this.http.get<StandaloneAttraction>(`${this.baseUrl}/${encodeURIComponent(id)}`, options);
  }

  getAdminById(id: string, options: StandaloneAttractionsHttpOptions = {}): Observable<StandaloneAttraction> {
    return this.http.get<StandaloneAttraction>(`${this.adminBaseUrl}/${encodeURIComponent(id)}`, options);
  }

  create(attraction: StandaloneAttraction): Observable<StandaloneAttraction> {
    return this.http.post<StandaloneAttraction>(this.baseUrl, this.toWriteRequest(attraction));
  }

  update(id: string, attraction: StandaloneAttraction): Observable<StandaloneAttraction> {
    return this.http.put<StandaloneAttraction>(`${this.baseUrl}/${encodeURIComponent(id)}`, this.toWriteRequest(attraction));
  }

  updateBulkAdministration(request: BulkAdministrationUpdateRequest): Observable<BulkAdministrationUpdateResult> {
    return this.http.patch<BulkAdministrationUpdateResult>(`${this.baseUrl}/bulk-administration`, request);
  }

  migrateFromPark(request: StandaloneAttractionMigrationRequest): Observable<StandaloneAttraction> {
    return this.http.post<StandaloneAttraction>(`${this.baseUrl}/migrate-from-park`, request);
  }

  downloadExport(id: string): Observable<HttpResponse<Blob>> {
    const url: string = `${this.upsertBaseUrl}/standalone-attractions/${encodeURIComponent(id)}/export`;
    return this.http.get(url, {
      observe: 'response',
      responseType: 'blob'
    });
  }

  private buildListParams(page: number, size: number, filters: StandaloneAttractionListFilters): HttpParams {
    let params: HttpParams = new HttpParams()
      .set('page', String(page))
      .set('size', String(size));

    params = this.appendString(params, 'search', filters.search);
    params = this.appendBoolean(params, 'isVisible', filters.isVisible);
    params = this.appendString(params, 'adminReviewStatus', filters.adminReviewStatus);
    params = this.appendString(params, 'type', filters.type);
    params = this.appendString(params, 'countryCode', filters.countryCode);
    params = this.appendString(params, 'manufacturerId', filters.manufacturerId);
    params = this.appendString(params, 'sortBy', filters.sortBy);
    params = this.appendString(params, 'sortDirection', filters.sortDirection);
    return params;
  }

  private appendString(params: HttpParams, key: string, value: string | null | undefined): HttpParams {
    const normalized: string | null = value?.trim() || null;
    return normalized ? params.set(key, normalized) : params;
  }

  private appendBoolean(params: HttpParams, key: string, value: boolean | null | undefined): HttpParams {
    return value === null || value === undefined ? params : params.set(key, String(value));
  }

  private toWriteRequest(attraction: StandaloneAttraction): StandaloneAttraction {
    return {
      ...attraction,
      name: attraction.name.trim(),
      countryCode: attraction.countryCode?.trim().toUpperCase() || null,
      subtype: attraction.subtype?.trim() || null,
      operatorId: attraction.operatorId?.trim() || null,
      websiteUrl: attraction.websiteUrl?.trim() || null,
      street: attraction.street?.trim() || null,
      city: attraction.city?.trim() || null,
      postalCode: attraction.postalCode?.trim() || null,
      descriptions: attraction.descriptions ?? [],
      legacyParkId: attraction.legacyParkId?.trim() || null,
      legacyParkItemId: attraction.legacyParkItemId?.trim() || null
    };
  }
}
