import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { EMPTY, expand, map, Observable, reduce } from 'rxjs';

import { TechnicalPage, TechnicalPagesJsonUpsert, TechnicalPagesJsonUpsertResult } from '@app/models/technical-pages/technical-page';
import { PagedResult } from '@shared/models/contracts';
import { PagedCollectionResponse, unwrapCollection, unwrapPagedCollection } from '@data-access/shared/api-helpers';
import { environment } from '../../../environments/environment';
import { TECHNICAL_PAGES_API_ENDPOINTS } from './technical-pages-api-endpoints';

@Injectable({
  providedIn: 'root'
})
export class TechnicalPagesApiService {
  private readonly jsonHttpOptions = {
    headers: new HttpHeaders({
      'Content-Type': 'application/json'
    })
  };

  constructor(private readonly http: HttpClient) {
  }

  getPublicPagesPage(page: number = 1, size: number = 100): Observable<PagedResult<TechnicalPage>> {
    const url: string = `${environment.apiBaseUrl}${TECHNICAL_PAGES_API_ENDPOINTS.getPublicPages}`;
    const params: HttpParams = new HttpParams()
      .set('page', String(page))
      .set('size', String(size));

    return this.http.get<TechnicalPage[] | PagedCollectionResponse<TechnicalPage>>(url, { params }).pipe(
      map((response: TechnicalPage[] | PagedCollectionResponse<TechnicalPage>) => unwrapPagedCollection<TechnicalPage>(response))
    );
  }

  getAllPublicPages(): Observable<TechnicalPage[]> {
    return this.getPublicPagesPage(1, 100).pipe(
      expand((result: PagedResult<TechnicalPage>) => {
        const nextPage: number = result.pagination.currentPage + 1;
        if (nextPage > result.pagination.totalPages) {
          return EMPTY;
        }

        return this.getPublicPagesPage(nextPage, result.pagination.itemsPerPage || 100);
      }),
      reduce((items: TechnicalPage[], result: PagedResult<TechnicalPage>) => [...items, ...unwrapCollection<TechnicalPage>({ data: result.items })], [] as TechnicalPage[])
    );
  }

  getPublicLinkIndex(): Observable<TechnicalPage[]> {
    const url: string = `${environment.apiBaseUrl}${TECHNICAL_PAGES_API_ENDPOINTS.getPublicLinkIndex}`;
    return this.http.get<TechnicalPage[]>(url).pipe(
      map((response: TechnicalPage[]) => unwrapCollection<TechnicalPage>(response))
    );
  }

  getAdminPagesPage(page: number = 1, size: number = 100): Observable<PagedResult<TechnicalPage>> {
    const url: string = `${environment.apiBaseUrl}${TECHNICAL_PAGES_API_ENDPOINTS.getAdminPages}`;
    const params: HttpParams = new HttpParams()
      .set('page', String(page))
      .set('size', String(size));

    return this.http.get<TechnicalPage[] | PagedCollectionResponse<TechnicalPage>>(url, { params }).pipe(
      map((response: TechnicalPage[] | PagedCollectionResponse<TechnicalPage>) => unwrapPagedCollection<TechnicalPage>(response))
    );
  }

  getAllAdminPages(): Observable<TechnicalPage[]> {
    return this.getAdminPagesPage(1, 100).pipe(
      expand((result: PagedResult<TechnicalPage>) => {
        const nextPage: number = result.pagination.currentPage + 1;
        if (nextPage > result.pagination.totalPages) {
          return EMPTY;
        }

        return this.getAdminPagesPage(nextPage, result.pagination.itemsPerPage || 100);
      }),
      reduce((items: TechnicalPage[], result: PagedResult<TechnicalPage>) => [...items, ...unwrapCollection<TechnicalPage>({ data: result.items })], [] as TechnicalPage[])
    );
  }

  getById(id: string): Observable<TechnicalPage> {
    const url: string = `${environment.apiBaseUrl}${TECHNICAL_PAGES_API_ENDPOINTS.getById(id)}`;
    return this.http.get<TechnicalPage>(url);
  }

  getBySlug(slug: string): Observable<TechnicalPage> {
    const url: string = `${environment.apiBaseUrl}${TECHNICAL_PAGES_API_ENDPOINTS.getBySlug(slug)}`;
    return this.http.get<TechnicalPage>(url);
  }

  create(page: TechnicalPage): Observable<TechnicalPage> {
    const url: string = `${environment.apiBaseUrl}${TECHNICAL_PAGES_API_ENDPOINTS.create}`;
    return this.http.post<TechnicalPage>(url, JSON.stringify(page), this.jsonHttpOptions);
  }

  update(id: string, page: TechnicalPage): Observable<TechnicalPage> {
    const url: string = `${environment.apiBaseUrl}${TECHNICAL_PAGES_API_ENDPOINTS.update(id)}`;
    return this.http.put<TechnicalPage>(url, JSON.stringify(page), this.jsonHttpOptions);
  }

  upsertJson(request: TechnicalPagesJsonUpsert): Observable<TechnicalPagesJsonUpsertResult> {
    const url: string = `${environment.apiBaseUrl}${TECHNICAL_PAGES_API_ENDPOINTS.upsertJson}`;
    return this.http.post<TechnicalPagesJsonUpsertResult>(url, JSON.stringify(request), this.jsonHttpOptions);
  }
}
