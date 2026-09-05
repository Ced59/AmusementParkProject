import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  PassportBetaMetricsQuery,
  PassportBetaMetricsResult
} from '@app/models/passport/passport-beta-metrics.models';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AdminPassportBetaApiService {
  private readonly baseUrl: string = `${environment.apiBaseUrl}admin/passport-beta/metrics`;

  constructor(private readonly http: HttpClient) {
  }

  getMetrics(query: PassportBetaMetricsQuery = {}): Observable<PassportBetaMetricsResult> {
    let params: HttpParams = new HttpParams();
    params = this.setOptionalParam(params, 'fromUtc', query.fromUtc);
    params = this.setOptionalParam(params, 'toUtc', query.toUtc);

    return this.http.get<PassportBetaMetricsResult>(this.baseUrl, { params });
  }

  private setOptionalParam(
    params: HttpParams,
    key: string,
    value: string | null | undefined
  ): HttpParams {
    const normalizedValue: string = value?.trim() ?? '';
    return normalizedValue.length > 0 ? params.set(key, normalizedValue) : params;
  }
}
