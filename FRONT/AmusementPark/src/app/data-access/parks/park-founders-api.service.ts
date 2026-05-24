import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { EMPTY, expand, map, Observable, reduce } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ParkFounder } from '@app/models/parks/park-founder';
import { PagedResult } from '@shared/models/contracts';
import { PagedCollectionResponse, unwrapCollection, unwrapPagedCollection } from '../shared/api-helpers';
import { PARK_FOUNDERS_API_ENDPOINTS } from './park-founders-api-endpoints';

@Injectable({
  providedIn: 'root'
})
export class ParkFoundersApiService {
  private readonly jsonHttpOptions = {
    headers: new HttpHeaders({
      'Content-Type': 'application/json'
    })
  };

  constructor(private readonly http: HttpClient) {
  }

  getParkFounders(): Observable<ParkFounder[]> {
    return this.getAllParkFounders();
  }

  getParkFoundersPage(page: number = 1, size: number = 100): Observable<PagedResult<ParkFounder>> {
    const url: string = `${environment.apiBaseUrl}${PARK_FOUNDERS_API_ENDPOINTS.getParkFounders}`;
    const params: HttpParams = new HttpParams()
      .set('page', page.toString())
      .set('size', size.toString());

    return this.http.get<ParkFounder[] | PagedCollectionResponse<ParkFounder>>(url, { params }).pipe(
      map((response: ParkFounder[] | PagedCollectionResponse<ParkFounder>) => unwrapPagedCollection<ParkFounder>(response))
    );
  }

  getAllParkFounders(): Observable<ParkFounder[]> {
    return this.getParkFoundersPage(1, 100).pipe(
      expand((result: PagedResult<ParkFounder>) => {
        const nextPage: number = result.pagination.currentPage + 1;
        if (nextPage > result.pagination.totalPages) {
          return EMPTY;
        }

        return this.getParkFoundersPage(nextPage, result.pagination.itemsPerPage || 100);
      }),
      reduce((items: ParkFounder[], result: PagedResult<ParkFounder>) => [...items, ...unwrapCollection<ParkFounder>({ data: result.items })], [] as ParkFounder[])
    );
  }

  getParkFounderById(id: string): Observable<ParkFounder> {
    const url: string = `${environment.apiBaseUrl}${PARK_FOUNDERS_API_ENDPOINTS.getParkFounderById(id)}`;
    return this.http.get<ParkFounder>(url);
  }

  createParkFounder(founder: ParkFounder): Observable<ParkFounder> {
    const url: string = `${environment.apiBaseUrl}${PARK_FOUNDERS_API_ENDPOINTS.createParkFounder}`;
    return this.http.post<ParkFounder>(url, JSON.stringify(founder), this.jsonHttpOptions);
  }

  updateParkFounder(id: string, founder: ParkFounder): Observable<ParkFounder> {
    const url: string = `${environment.apiBaseUrl}${PARK_FOUNDERS_API_ENDPOINTS.updateParkFounder(id)}`;
    return this.http.put<ParkFounder>(url, JSON.stringify(founder), this.jsonHttpOptions);
  }
}
