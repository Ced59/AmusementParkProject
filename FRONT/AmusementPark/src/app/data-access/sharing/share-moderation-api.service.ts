import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';

import { environment } from '../../../environments/environment';
import { PagedResult } from '@app/shared/models/contracts/paged-result.model';
import { PagedCollectionResponse, unwrapPagedCollection } from '@data-access/shared/api-helpers';
import {
  ReviewShareModerationReportRequest,
  ShareModerationReport,
  ShareModerationReportQuery,
  SubmitShareModerationReportRequest,
} from '@app/models/sharing/share-moderation.models';
import { SHARE_MODERATION_API_ENDPOINTS } from './share-moderation-api-endpoints';

@Injectable({ providedIn: 'root' })
export class ShareModerationApiService {
  public constructor(private readonly http: HttpClient) {}

  public submit(request: SubmitShareModerationReportRequest): Observable<void> {
    return this.http.post<void>(
      `${environment.apiBaseUrl}${SHARE_MODERATION_API_ENDPOINTS.submit}`,
      request,
    );
  }

  public search(
    query: ShareModerationReportQuery,
  ): Observable<PagedResult<ShareModerationReport>> {
    let params: HttpParams = new HttpParams()
      .set('page', query.page)
      .set('size', query.size);
    if (query.status) {
      params = params.set('status', query.status);
    }
    if (query.targetType) {
      params = params.set('targetType', query.targetType);
    }
    if (query.reason) {
      params = params.set('reason', query.reason);
    }
    return this.http.get<PagedCollectionResponse<ShareModerationReport>>(
      `${environment.apiBaseUrl}${SHARE_MODERATION_API_ENDPOINTS.adminReports}`,
      { params },
    ).pipe(map((response: PagedCollectionResponse<ShareModerationReport>): PagedResult<ShareModerationReport> =>
      unwrapPagedCollection<ShareModerationReport>(response)));
  }

  public review(
    reportId: string,
    request: ReviewShareModerationReportRequest,
  ): Observable<void> {
    return this.http.put<void>(
      `${environment.apiBaseUrl}${SHARE_MODERATION_API_ENDPOINTS.adminReview(reportId)}`,
      request,
    );
  }
}
