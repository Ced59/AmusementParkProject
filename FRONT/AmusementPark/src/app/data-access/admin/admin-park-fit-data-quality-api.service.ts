import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';

import {
  ParkFitDataQuality,
  ParkFitDataQualityPage,
  ParkFitOperationalStatusRequest,
  ParkFitSourceReport,
  ParkFitSourceReportPage,
  ParkFitSourceReportReviewRequest
} from '@app/models/admin/park-fit/park-fit-data-quality.models';
import {
  PagedCollectionResponse,
  unwrapPagedCollection
} from '@app/data-access/shared/api-helpers';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AdminParkFitDataQualityApiService {
  private readonly baseUrl: string = `${environment.apiBaseUrl}admin/park-fit/data-quality`;

  constructor(private readonly http: HttpClient) {
  }

  getPage(page: number, size: number): Observable<ParkFitDataQualityPage> {
    const params: HttpParams = new HttpParams()
      .set('page', page)
      .set('size', size);

    return this.http.get<PagedCollectionResponse<ParkFitDataQuality>>(this.baseUrl, { params })
      .pipe(map((response: PagedCollectionResponse<ParkFitDataQuality>) =>
        unwrapPagedCollection(response)));
  }

  getPendingReports(page: number, size: number): Observable<ParkFitSourceReportPage> {
    const params: HttpParams = new HttpParams()
      .set('page', page)
      .set('size', size)
      .set('status', 'Pending');
    const url: string = `${environment.apiBaseUrl}admin/park-fit/reports`;
    return this.http.get<PagedCollectionResponse<ParkFitSourceReport>>(url, { params })
      .pipe(map((response: PagedCollectionResponse<ParkFitSourceReport>) =>
        unwrapPagedCollection(response)));
  }

  reviewReport(reportId: string, request: ParkFitSourceReportReviewRequest): Observable<void> {
    const url: string = `${environment.apiBaseUrl}admin/park-fit/reports/${encodeURIComponent(reportId)}`;
    return this.http.put<void>(url, request);
  }

  changeOperationalStatus(parkId: string, request: ParkFitOperationalStatusRequest): Observable<void> {
    const url: string = `${environment.apiBaseUrl}admin/park-fit/parks/${encodeURIComponent(parkId)}/operational-status`;
    return this.http.put<void>(url, request);
  }
}
