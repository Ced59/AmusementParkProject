import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { EMPTY, expand, map, Observable, reduce } from 'rxjs';

import { environment } from '../../../environments/environment';
import { BulkAdministrationUpdateResult, BulkReviewStatusUpdateRequest } from '@app/models/admin/admin-review-status';
import { ParkOperator } from '@app/models/parks/park-operator';
import { PagedResult } from '@shared/models/contracts';
import { PagedCollectionResponse, unwrapCollection, unwrapPagedCollection } from '../shared/api-helpers';
import { PARK_OPERATORS_API_ENDPOINTS } from './park-operators-api-endpoints';

@Injectable({
  providedIn: 'root'
})
export class ParkOperatorsApiService {
  private readonly jsonHttpOptions = {
    headers: new HttpHeaders({
      'Content-Type': 'application/json'
    })
  };

  constructor(private readonly http: HttpClient) {
  }

  getParkOperators(): Observable<ParkOperator[]> {
    return this.getAllParkOperators();
  }

  getParkOperatorsPage(page: number = 1, size: number = 100): Observable<PagedResult<ParkOperator>> {
    const url: string = `${environment.apiBaseUrl}${PARK_OPERATORS_API_ENDPOINTS.getParkOperators}`;
    const params: HttpParams = new HttpParams()
      .set('page', page.toString())
      .set('size', size.toString());

    return this.http.get<ParkOperator[] | PagedCollectionResponse<ParkOperator>>(url, { params }).pipe(
      map((response: ParkOperator[] | PagedCollectionResponse<ParkOperator>) => unwrapPagedCollection<ParkOperator>(response))
    );
  }

  getAllParkOperators(): Observable<ParkOperator[]> {
    return this.getParkOperatorsPage(1, 100).pipe(
      expand((result: PagedResult<ParkOperator>) => {
        const nextPage: number = result.pagination.currentPage + 1;
        if (nextPage > result.pagination.totalPages) {
          return EMPTY;
        }

        return this.getParkOperatorsPage(nextPage, result.pagination.itemsPerPage || 100);
      }),
      reduce((items: ParkOperator[], result: PagedResult<ParkOperator>) => [...items, ...unwrapCollection<ParkOperator>({ data: result.items })], [] as ParkOperator[])
    );
  }

  getParkOperatorById(id: string): Observable<ParkOperator> {
    const url: string = `${environment.apiBaseUrl}${PARK_OPERATORS_API_ENDPOINTS.getParkOperatorById(id)}`;
    return this.http.get<ParkOperator>(url);
  }

  createParkOperator(parkOperator: ParkOperator): Observable<ParkOperator> {
    const url: string = `${environment.apiBaseUrl}${PARK_OPERATORS_API_ENDPOINTS.createParkOperator}`;
    return this.http.post<ParkOperator>(url, JSON.stringify(parkOperator), this.jsonHttpOptions);
  }

  updateParkOperator(id: string, parkOperator: ParkOperator): Observable<ParkOperator> {
    const url: string = `${environment.apiBaseUrl}${PARK_OPERATORS_API_ENDPOINTS.updateParkOperator(id)}`;
    return this.http.put<ParkOperator>(url, JSON.stringify(parkOperator), this.jsonHttpOptions);
  }

  updateParkOperatorsBulkReviewStatus(request: BulkReviewStatusUpdateRequest): Observable<BulkAdministrationUpdateResult> {
    const url: string = `${environment.apiBaseUrl}${PARK_OPERATORS_API_ENDPOINTS.updateParkOperatorsBulkReviewStatus}`;
    return this.http.patch<BulkAdministrationUpdateResult>(url, JSON.stringify(request), this.jsonHttpOptions);
  }
}
