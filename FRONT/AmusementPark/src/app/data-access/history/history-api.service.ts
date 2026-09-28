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
import { AdminHistoricalParkDiagnostics } from '@app/models/history/admin-historical-park-diagnostics.models';
import {
  AdminHistoricalParkWorkbench,
  HistoricalEditorialMutation,
  HistoricalEditorialResourceType,
  HistoricalPublicationImpactPreview,
  SaveHistoricalFactRequest,
  SaveHistoricalRelationRequest,
  SaveHistoricalSourceRequest
} from '@app/models/history/admin-historical-workbench.models';
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

  getAdminParkDiagnostics(parkId: string): Observable<AdminHistoricalParkDiagnostics> {
    const url: string = `${environment.apiBaseUrl}${HISTORY_API_ENDPOINTS.getAdminParkDiagnostics(parkId)}`;
    return this.http.get<AdminHistoricalParkDiagnostics>(url);
  }

  getAdminParkWorkbench(parkId: string): Observable<AdminHistoricalParkWorkbench> {
    const url: string = `${environment.apiBaseUrl}${HISTORY_API_ENDPOINTS.getAdminParkWorkbench(parkId)}`;
    return this.http.get<AdminHistoricalParkWorkbench>(url);
  }

  previewAdminHistoricalImpact(parkId: string, resourceType: HistoricalEditorialResourceType, resourceId: string, year: number | null): Observable<HistoricalPublicationImpactPreview> {
    const url: string = `${environment.apiBaseUrl}${HISTORY_API_ENDPOINTS.previewAdminHistoricalImpact(parkId)}`;
    return this.http.post<HistoricalPublicationImpactPreview>(url, { resourceType, resourceId, year }, this.jsonHttpOptions);
  }

  saveAdminHistoricalSource(sourceId: string | null, request: SaveHistoricalSourceRequest): Observable<HistoricalEditorialMutation> {
    const url: string = `${environment.apiBaseUrl}${sourceId
      ? HISTORY_API_ENDPOINTS.updateAdminHistoricalSource(sourceId)
      : HISTORY_API_ENDPOINTS.createAdminHistoricalSource}`;
    return sourceId
      ? this.http.patch<HistoricalEditorialMutation>(url, request, this.jsonHttpOptions)
      : this.http.post<HistoricalEditorialMutation>(url, request, this.jsonHttpOptions);
  }

  saveAdminHistoricalFact(parkId: string, factId: string | null, request: SaveHistoricalFactRequest): Observable<HistoricalEditorialMutation> {
    const url: string = `${environment.apiBaseUrl}${factId
      ? HISTORY_API_ENDPOINTS.updateAdminHistoricalFact(parkId, factId)
      : HISTORY_API_ENDPOINTS.createAdminHistoricalFact(parkId)}`;
    return factId
      ? this.http.patch<HistoricalEditorialMutation>(url, request, this.jsonHttpOptions)
      : this.http.post<HistoricalEditorialMutation>(url, request, this.jsonHttpOptions);
  }

  saveAdminHistoricalRelation(parkId: string, relationId: string | null, request: SaveHistoricalRelationRequest): Observable<HistoricalEditorialMutation> {
    const url: string = `${environment.apiBaseUrl}${relationId
      ? HISTORY_API_ENDPOINTS.updateAdminHistoricalRelation(parkId, relationId)
      : HISTORY_API_ENDPOINTS.createAdminHistoricalRelation(parkId)}`;
    return relationId
      ? this.http.patch<HistoricalEditorialMutation>(url, request, this.jsonHttpOptions)
      : this.http.post<HistoricalEditorialMutation>(url, request, this.jsonHttpOptions);
  }

  advanceAdminHistoricalResource(resourceType: HistoricalEditorialResourceType, resourceId: string, expectedRevision: number, reviewNote: string | null): Observable<HistoricalEditorialMutation> {
    const endpoint: string = resourceType === 'Fact'
      ? HISTORY_API_ENDPOINTS.reviewAdminHistoricalFact(resourceId)
      : resourceType === 'Relation'
        ? HISTORY_API_ENDPOINTS.reviewAdminHistoricalRelation(resourceId)
        : HISTORY_API_ENDPOINTS.reviewAdminHistoricalSource(resourceId);
    return this.http.post<HistoricalEditorialMutation>(
      `${environment.apiBaseUrl}${endpoint}`,
      { expectedRevision, reviewNote },
      this.jsonHttpOptions
    );
  }

  retractAdminHistoricalResource(resourceType: HistoricalEditorialResourceType, resourceId: string, expectedRevision: number, reviewNote: string | null): Observable<HistoricalEditorialMutation> {
    const endpoint: string = resourceType === 'Fact'
      ? HISTORY_API_ENDPOINTS.retractAdminHistoricalFact(resourceId)
      : resourceType === 'Relation'
        ? HISTORY_API_ENDPOINTS.retractAdminHistoricalRelation(resourceId)
        : HISTORY_API_ENDPOINTS.retractAdminHistoricalSource(resourceId);
    return this.http.post<HistoricalEditorialMutation>(
      `${environment.apiBaseUrl}${endpoint}`,
      { expectedRevision, reviewNote },
      this.jsonHttpOptions
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
