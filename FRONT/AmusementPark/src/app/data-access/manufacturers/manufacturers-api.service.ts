import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { EMPTY, expand, map, Observable, reduce } from 'rxjs';

import { environment } from '../../../environments/environment';
import { BulkAdministrationUpdateResult, BulkReviewStatusUpdateRequest } from '@app/models/admin/admin-review-status';
import { AttractionManufacturer } from '@app/models/parks/attraction-manufacturer';
import { PagedResult } from '@shared/models/contracts';
import { PagedCollectionResponse, unwrapCollection, unwrapPagedCollection } from '../shared/api-helpers';
import { MANUFACTURERS_API_ENDPOINTS } from './manufacturers-api-endpoints';

@Injectable({
  providedIn: 'root'
})
export class ManufacturersApiService {
  private readonly jsonHttpOptions = {
    headers: new HttpHeaders({
      'Content-Type': 'application/json'
    })
  };

  constructor(private readonly http: HttpClient) {
  }

  getAttractionManufacturers(): Observable<AttractionManufacturer[]> {
    return this.getAllAttractionManufacturers();
  }

  getAttractionManufacturersPage(page: number = 1, size: number = 100, search: string | null = null): Observable<PagedResult<AttractionManufacturer>> {
    const url: string = `${environment.apiBaseUrl}${MANUFACTURERS_API_ENDPOINTS.getAttractionManufacturers}`;
    let params: HttpParams = new HttpParams()
      .set('page', page.toString())
      .set('size', size.toString());

    if (search?.trim()) {
      params = params.set('search', search.trim());
    }

    return this.http.get<AttractionManufacturer[] | PagedCollectionResponse<AttractionManufacturer>>(url, { params }).pipe(
      map((response: AttractionManufacturer[] | PagedCollectionResponse<AttractionManufacturer>) => unwrapPagedCollection<AttractionManufacturer>(response))
    );
  }

  getAllAttractionManufacturers(): Observable<AttractionManufacturer[]> {
    return this.getAttractionManufacturersPage(1, 100).pipe(
      expand((result: PagedResult<AttractionManufacturer>) => {
        const nextPage: number = result.pagination.currentPage + 1;
        if (nextPage > result.pagination.totalPages) {
          return EMPTY;
        }

        return this.getAttractionManufacturersPage(nextPage, result.pagination.itemsPerPage || 100);
      }),
      reduce((items: AttractionManufacturer[], result: PagedResult<AttractionManufacturer>) => [...items, ...unwrapCollection<AttractionManufacturer>({ data: result.items })], [] as AttractionManufacturer[])
    );
  }

  getAttractionManufacturerById(id: string): Observable<AttractionManufacturer> {
    const url: string = `${environment.apiBaseUrl}${MANUFACTURERS_API_ENDPOINTS.getAttractionManufacturerById(id)}`;
    return this.http.get<AttractionManufacturer>(url);
  }

  createAttractionManufacturer(manufacturer: AttractionManufacturer): Observable<AttractionManufacturer> {
    const url: string = `${environment.apiBaseUrl}${MANUFACTURERS_API_ENDPOINTS.createAttractionManufacturer}`;
    return this.http.post<AttractionManufacturer>(url, JSON.stringify(manufacturer), this.jsonHttpOptions);
  }

  updateAttractionManufacturer(id: string, manufacturer: AttractionManufacturer): Observable<AttractionManufacturer> {
    const url: string = `${environment.apiBaseUrl}${MANUFACTURERS_API_ENDPOINTS.updateAttractionManufacturer(id)}`;
    return this.http.put<AttractionManufacturer>(url, JSON.stringify(manufacturer), this.jsonHttpOptions);
  }

  updateAttractionManufacturersBulkReviewStatus(request: BulkReviewStatusUpdateRequest): Observable<BulkAdministrationUpdateResult> {
    const url: string = `${environment.apiBaseUrl}${MANUFACTURERS_API_ENDPOINTS.updateAttractionManufacturersBulkReviewStatus}`;
    return this.http.patch<BulkAdministrationUpdateResult>(url, JSON.stringify(request), this.jsonHttpOptions);
  }
}
