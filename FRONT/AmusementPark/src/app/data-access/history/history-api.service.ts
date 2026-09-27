import { HttpClient, HttpContext, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';

import {
  HistoryArticle,
  HistoryEvent,
  HistoryEventAdminListResponse,
  HistoryEventWriteModel,
  HistoryTimeline
} from '@app/models/history/history.models';
import {
  PublicHistoricalLineage,
  PublicParkHistoricalComparison,
  PublicParkHistoricalSnapshot,
  PublicParkHistoricalTimeline
} from '@app/models/history/public-park-history.models';
import { PagedCollectionResponse, unwrapPagedCollection } from '@data-access/shared/api-helpers';
import { PagedResult } from '@shared/models/contracts';
import { environment } from '../../../environments/environment';
import { AdminHistoryEventListQuery, HISTORY_API_ENDPOINTS } from './history-api-endpoints';

interface HistoryHttpOptions {
  context?: HttpContext;
}

@Injectable({
  providedIn: 'root'
})
export class HistoryApiService {
  private readonly jsonHttpOptions = {
    headers: new HttpHeaders({
      'Content-Type': 'application/json'
    })
  };

  constructor(private readonly http: HttpClient) {
  }

  getPublicHistoricalLineage(subjectType: string, subjectId: string, contextParkId: string, options: HistoryHttpOptions = {}): Observable<PublicHistoricalLineage> {
    const url: string = `${environment.apiBaseUrl}${HISTORY_API_ENDPOINTS.getPublicHistoricalLineage(subjectType, subjectId, contextParkId)}`;
    return this.http.get<PublicHistoricalLineage>(url, options);
  }

  getPublicParkTimeline(parkId: string, options: HistoryHttpOptions = {}, page: number = 1, pageSize: number = 50): Observable<PublicParkHistoricalTimeline> {
    const url: string = `${environment.apiBaseUrl}${HISTORY_API_ENDPOINTS.getPublicParkTimeline(parkId, page, pageSize)}`;
    return this.http.get<PublicParkHistoricalTimeline>(url, options);
  }

  getPublicParkSnapshot(parkId: string, year: number, month: number | null = null, day: number | null = null, options: HistoryHttpOptions = {}): Observable<PublicParkHistoricalSnapshot> {
    const url: string = `${environment.apiBaseUrl}${HISTORY_API_ENDPOINTS.getPublicParkSnapshot(parkId, year, month, day)}`;
    return this.http.get<PublicParkHistoricalSnapshot>(url, options);
  }

  getPublicParkComparison(parkId: string, fromYear: number, toYear: number, options: HistoryHttpOptions = {}): Observable<PublicParkHistoricalComparison> {
    const url: string = `${environment.apiBaseUrl}${HISTORY_API_ENDPOINTS.getPublicParkComparison(parkId, fromYear, toYear)}`;
    return this.http.get<PublicParkHistoricalComparison>(url, options);
  }

  getParkTimeline(parkId: string, includeParkItems: boolean = false, parkItemIds: readonly string[] = [], options: HistoryHttpOptions = {}, page: number = 1): Observable<HistoryTimeline> {
    const url: string = `${environment.apiBaseUrl}${HISTORY_API_ENDPOINTS.getParkTimeline(parkId, includeParkItems, parkItemIds, page)}`;
    return this.getTimeline(url, options);
  }

  getParkItemTimeline(parkItemId: string, options: HistoryHttpOptions = {}, page: number = 1): Observable<HistoryTimeline> {
    const url: string = `${environment.apiBaseUrl}${HISTORY_API_ENDPOINTS.getParkItemTimeline(parkItemId, page)}`;
    return this.getTimeline(url, options);
  }

  getStandaloneAttractionTimeline(standaloneAttractionId: string, options: HistoryHttpOptions = {}, page: number = 1): Observable<HistoryTimeline> {
    const url: string = `${environment.apiBaseUrl}${HISTORY_API_ENDPOINTS.getStandaloneAttractionTimeline(standaloneAttractionId, page)}`;
    return this.getTimeline(url, options);
  }

  getArticle(eventId: string, options: HistoryHttpOptions = {}): Observable<HistoryArticle> {
    const url: string = `${environment.apiBaseUrl}${HISTORY_API_ENDPOINTS.getArticle(eventId)}`;
    return this.http.get<HistoryArticle>(url, options);
  }

  getAdminEvents(query: AdminHistoryEventListQuery): Observable<PagedResult<HistoryEvent>> {
    const url: string = `${environment.apiBaseUrl}${HISTORY_API_ENDPOINTS.getAdminEvents(query)}`;
    return this.http.get<HistoryEventAdminListResponse | PagedCollectionResponse<HistoryEvent>>(url).pipe(
      map((response: HistoryEventAdminListResponse | PagedCollectionResponse<HistoryEvent>) => {
        if ('items' in response && 'totalCount' in response) {
          const itemsPerPage: number = Math.max(1, (query.size ?? response.items.length) || 1);

          return {
            items: response.items,
            pagination: {
              currentPage: query.page ?? 1,
              itemsPerPage,
              totalItems: response.totalCount,
              totalPages: Math.max(1, Math.ceil(response.totalCount / itemsPerPage))
            }
          };
        }

        return unwrapPagedCollection<HistoryEvent>(response);
      })
    );
  }

  createAdminEvent(request: HistoryEventWriteModel): Observable<HistoryEvent> {
    const url: string = `${environment.apiBaseUrl}${HISTORY_API_ENDPOINTS.createAdminEvent}`;
    return this.http.post<HistoryEvent>(url, request, this.jsonHttpOptions);
  }

  updateAdminEvent(eventId: string, request: HistoryEventWriteModel): Observable<HistoryEvent> {
    const url: string = `${environment.apiBaseUrl}${HISTORY_API_ENDPOINTS.updateAdminEvent(eventId)}`;
    return this.http.put<HistoryEvent>(url, request, this.jsonHttpOptions);
  }

  deleteAdminEvent(eventId: string): Observable<boolean> {
    const url: string = `${environment.apiBaseUrl}${HISTORY_API_ENDPOINTS.deleteAdminEvent(eventId)}`;
    return this.http.delete<boolean>(url);
  }

  private getTimeline(url: string, options: HistoryHttpOptions): Observable<HistoryTimeline> {
    return this.http.get<HistoryTimeline>(url, options);
  }
}
