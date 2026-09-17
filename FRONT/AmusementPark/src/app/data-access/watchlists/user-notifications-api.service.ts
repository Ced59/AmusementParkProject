import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  UserNotificationPage,
  UserNotificationSearch
} from '@app/models/watchlists/user-notification.model';
import { environment } from '../../../environments/environment';
import { USER_NOTIFICATIONS_API_ENDPOINTS } from './user-notifications-api-endpoints';

@Injectable({ providedIn: 'root' })
export class UserNotificationsApiService {
  constructor(private readonly http: HttpClient) {
  }

  search(criteria: UserNotificationSearch): Observable<UserNotificationPage> {
    let params: HttpParams = new HttpParams()
      .set('page', criteria.page)
      .set('size', criteria.size)
      .set('unreadOnly', criteria.unreadOnly);
    if (criteria.parkId) {
      params = params.set('parkId', criteria.parkId);
    }
    if (criteria.eventType) {
      params = params.set('eventType', criteria.eventType);
    }
    return this.http.get<UserNotificationPage>(
      `${environment.apiBaseUrl}${USER_NOTIFICATIONS_API_ENDPOINTS.collection}`,
      { params }
    );
  }

  markRead(notificationId: string, expectedVersion: number): Observable<void> {
    return this.http.post<void>(
      `${environment.apiBaseUrl}${USER_NOTIFICATIONS_API_ENDPOINTS.read(notificationId)}`,
      { expectedVersion }
    );
  }

  dismiss(notificationId: string, expectedVersion: number): Observable<void> {
    return this.http.post<void>(
      `${environment.apiBaseUrl}${USER_NOTIFICATIONS_API_ENDPOINTS.dismiss(notificationId)}`,
      { expectedVersion }
    );
  }

  markAllRead(): Observable<void> {
    return this.http.post<void>(
      `${environment.apiBaseUrl}${USER_NOTIFICATIONS_API_ENDPOINTS.readAll}`,
      null
    );
  }

  deleteSourceSubscription(notificationId: string, expectedVersion: number): Observable<void> {
    const params: HttpParams = new HttpParams().set('expectedVersion', expectedVersion);
    return this.http.delete<void>(
      `${environment.apiBaseUrl}${USER_NOTIFICATIONS_API_ENDPOINTS.subscription(notificationId)}`,
      { params }
    );
  }

  capturePilotInteraction(
    interactionKind: 'NotificationCenterOpened' | 'SourceOpened' | 'MisleadingAlertReported',
    notificationId: string | null = null
  ): Observable<void> {
    return this.http.post<void>(
      `${environment.apiBaseUrl}${USER_NOTIFICATIONS_API_ENDPOINTS.pilotInteractions}`,
      { interactionKind, notificationId }
    );
  }
}
