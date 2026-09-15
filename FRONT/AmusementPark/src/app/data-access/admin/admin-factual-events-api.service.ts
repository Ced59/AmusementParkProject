import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';

import {
  FactualChangeEventAdmin,
  FactualChangeEventMutationRequest,
  FactualChangeEventQuery,
} from '@app/models/admin/factual-events/factual-event-administration.models';
import { PagedResult } from '@shared/models/contracts';
import { PagedCollectionResponse, unwrapPagedCollection } from '@data-access/shared/api-helpers';
import { environment } from '../../../environments/environment';
import { ADMIN_FACTUAL_EVENTS_API_ENDPOINTS } from './admin-factual-events-api-endpoints';

@Injectable({ providedIn: 'root' })
export class AdminFactualEventsApiService {
  public constructor(private readonly http: HttpClient) {}

  public search(query: FactualChangeEventQuery): Observable<PagedResult<FactualChangeEventAdmin>> {
    let params: HttpParams = new HttpParams()
      .set('page', query.page)
      .set('size', query.size);
    if (query.status) {
      params = params.set('status', query.status);
    }
    if (query.targetType) {
      params = params.set('targetType', query.targetType);
    }
    if (query.eventType) {
      params = params.set('eventType', query.eventType);
    }
    if (query.confidence) {
      params = params.set('confidence', query.confidence);
    }

    return this.http.get<PagedCollectionResponse<FactualChangeEventAdmin>>(
      `${environment.apiBaseUrl}${ADMIN_FACTUAL_EVENTS_API_ENDPOINTS.search}`,
      { params },
    ).pipe(map((response: PagedCollectionResponse<FactualChangeEventAdmin>): PagedResult<FactualChangeEventAdmin> =>
      unwrapPagedCollection<FactualChangeEventAdmin>(response)));
  }

  public verify(eventId: string, request: FactualChangeEventMutationRequest): Observable<void> {
    return this.http.post<void>(
      `${environment.apiBaseUrl}${ADMIN_FACTUAL_EVENTS_API_ENDPOINTS.verify(eventId)}`,
      request,
    );
  }

  public publish(eventId: string, request: FactualChangeEventMutationRequest): Observable<void> {
    return this.http.post<void>(
      `${environment.apiBaseUrl}${ADMIN_FACTUAL_EVENTS_API_ENDPOINTS.publish(eventId)}`,
      request,
    );
  }
}
