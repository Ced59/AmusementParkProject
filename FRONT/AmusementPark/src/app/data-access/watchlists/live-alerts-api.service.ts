import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  CreateLiveAlertRequest,
  LiveAlertDashboard,
  LiveAlertSubscription
} from '@app/models/watchlists/live-alert.model';
import { environment } from '../../../environments/environment';
import { LIVE_ALERTS_API_ENDPOINTS } from './live-alerts-api-endpoints';

@Injectable({ providedIn: 'root' })
export class LiveAlertsApiService {
  constructor(private readonly http: HttpClient) {
  }

  getDashboard(targetId?: string): Observable<LiveAlertDashboard> {
    const params: HttpParams = targetId
      ? new HttpParams().set('targetId', targetId)
      : new HttpParams();
    return this.http.get<LiveAlertDashboard>(
      `${environment.apiBaseUrl}${LIVE_ALERTS_API_ENDPOINTS.collection}`,
      { params }
    );
  }

  create(request: CreateLiveAlertRequest): Observable<LiveAlertSubscription> {
    return this.http.post<LiveAlertSubscription>(
      `${environment.apiBaseUrl}${LIVE_ALERTS_API_ENDPOINTS.collection}`,
      request
    );
  }

  delete(subscriptionId: string, expectedVersion: number): Observable<void> {
    const params: HttpParams = new HttpParams().set('expectedVersion', expectedVersion);
    return this.http.delete<void>(
      `${environment.apiBaseUrl}${LIVE_ALERTS_API_ENDPOINTS.entry(subscriptionId)}`,
      { params }
    );
  }

  markRead(notificationId: string, expectedVersion: number): Observable<void> {
    return this.http.post<void>(
      `${environment.apiBaseUrl}${LIVE_ALERTS_API_ENDPOINTS.read(notificationId)}`,
      { expectedVersion }
    );
  }

  dismiss(notificationId: string, expectedVersion: number): Observable<void> {
    return this.http.post<void>(
      `${environment.apiBaseUrl}${LIVE_ALERTS_API_ENDPOINTS.dismiss(notificationId)}`,
      { expectedVersion }
    );
  }
}
