import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  PassportHistoricalExistenceReport,
  SubmitPassportHistoricalExistenceReportRequest
} from '@app/models/passport/passport-historical-existence-report.models';
import { environment } from '../../../environments/environment';
import { PASSPORT_HISTORICAL_EXISTENCE_REPORTS_API_ENDPOINTS } from './passport-historical-existence-reports-api-endpoints';

@Injectable({ providedIn: 'root' })
export class PassportHistoricalExistenceReportsApiService {
  constructor(private readonly http: HttpClient) {
  }

  list(visitId: string): Observable<PassportHistoricalExistenceReport[]> {
    const url: string = `${environment.apiBaseUrl}${PASSPORT_HISTORICAL_EXISTENCE_REPORTS_API_ENDPOINTS.forVisit(visitId)}`;
    return this.http.get<PassportHistoricalExistenceReport[]>(url, { transferCache: false });
  }

  submit(
    visitId: string,
    request: SubmitPassportHistoricalExistenceReportRequest
  ): Observable<PassportHistoricalExistenceReport> {
    const url: string = `${environment.apiBaseUrl}${PASSPORT_HISTORICAL_EXISTENCE_REPORTS_API_ENDPOINTS.forVisit(visitId)}`;
    return this.http.post<PassportHistoricalExistenceReport>(url, request);
  }
}
