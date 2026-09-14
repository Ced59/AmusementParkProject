import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  ParkFitPilotMetricsQuery,
  ParkFitPilotMetricsResult
} from '@app/models/admin/park-fit/park-fit-pilot-metrics.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AdminParkFitPilotApiService {
  private readonly baseUrl: string = `${environment.apiBaseUrl}admin/park-fit/pilot/metrics`;

  constructor(private readonly http: HttpClient) {
  }

  getMetrics(query: ParkFitPilotMetricsQuery = {}): Observable<ParkFitPilotMetricsResult> {
    let params: HttpParams = new HttpParams();
    params = this.setOptionalParam(params, 'fromUtc', query.fromUtc);
    params = this.setOptionalParam(params, 'toUtc', query.toUtc);
    return this.http.get<ParkFitPilotMetricsResult>(this.baseUrl, { params });
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
