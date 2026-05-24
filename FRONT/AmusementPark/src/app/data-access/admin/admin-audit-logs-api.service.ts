import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AdminAuditLogQuery, AdminAuditLogResponse } from '@app/models/admin/audit/admin-audit-log.models';
import { ADMIN_AUDIT_LOGS_API_ENDPOINTS } from './admin-audit-logs-api-endpoints';

@Injectable({
  providedIn: 'root'
})
export class AdminAuditLogsApiService {
  constructor(private readonly http: HttpClient) {
  }

  search(query: AdminAuditLogQuery): Observable<AdminAuditLogResponse> {
    const url: string = `${environment.apiBaseUrl}${ADMIN_AUDIT_LOGS_API_ENDPOINTS.search}`;
    const params: HttpParams = this.buildParams(query);
    return this.http.get<AdminAuditLogResponse>(url, { params });
  }

  private buildParams(query: AdminAuditLogQuery): HttpParams {
    let params: HttpParams = new HttpParams()
      .set('page', query.page)
      .set('size', query.size);

    params = this.setOptionalParam(params, 'fromUtc', query.fromUtc);
    params = this.setOptionalParam(params, 'toUtc', query.toUtc);
    params = this.setOptionalParam(params, 'actorUserId', query.actorUserId);
    params = this.setOptionalParam(params, 'actorEmail', query.actorEmail);
    params = this.setOptionalParam(params, 'action', query.action);
    params = this.setOptionalParam(params, 'entityType', query.entityType);
    params = this.setOptionalParam(params, 'entityId', query.entityId);
    params = this.setOptionalParam(params, 'traceId', query.traceId);

    return params;
  }

  private setOptionalParam(params: HttpParams, key: string, value: string | null | undefined): HttpParams {
    if (!value || value.trim().length === 0) {
      return params;
    }

    return params.set(key, value.trim());
  }
}
