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

  getParkTimeline(parkId: string, includeParkItems: boolean = false, parkItemIds: readonly string[] = [], options: HistoryHttpOptions = {}): Observable<HistoryTimeline> {
    const url: string = `${environment.apiBaseUrl}${HISTORY_API_ENDPOINTS.getParkTimeline(parkId, includeParkItems, parkItemIds)}`;
    return this.http.get<HistoryTimeline>(url, options);
  }

  getParkItemTimeline(parkItemId: string, options: HistoryHttpOptions = {}): Observable<HistoryTimeline> {
    const url: string = `${environment.apiBaseUrl}${HISTORY_API_ENDPOINTS.getParkItemTimeline(parkItemId)}`;
    return this.http.get<HistoryTimeline>(url, options);
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
}
