import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { TripPilotMetricsResult } from '@app/models/admin/trip-pilot/trip-pilot-metrics.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AdminTripPilotApiService {
  private readonly endpoint: string = `${environment.apiBaseUrl}admin/trip-pilot/metrics`;

  constructor(private readonly http: HttpClient) {
  }

  getMetrics(): Observable<TripPilotMetricsResult> {
    return this.http.get<TripPilotMetricsResult>(this.endpoint);
  }
}
