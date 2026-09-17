import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  WatchPilotMetricsQuery,
  WatchPilotMetricsResult
} from '@app/models/admin/watch-pilot/watch-pilot-metrics.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AdminWatchPilotApiService {
  private readonly endpoint: string = `${environment.apiBaseUrl}admin/watch-pilot/metrics`;

  constructor(private readonly http: HttpClient) {
  }

  getMetrics(query: WatchPilotMetricsQuery = {}): Observable<WatchPilotMetricsResult> {
    let params: HttpParams = new HttpParams();
    if (query.fromUtc) {
      params = params.set('fromUtc', query.fromUtc);
    }
    if (query.toUtc) {
      params = params.set('toUtc', query.toUtc);
    }
    return this.http.get<WatchPilotMetricsResult>(this.endpoint, { params });
  }
}
