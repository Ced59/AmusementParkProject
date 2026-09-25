import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  MarkTripNotificationsReadRequest,
  SetTripNotificationsRequest,
  TripNotificationState
} from '@app/models/trips/trip-notification.models';
import { environment } from '../../../environments/environment';
import { TRIP_API_ENDPOINTS } from './trip-api-endpoints';

@Injectable({ providedIn: 'root' })
export class TripNotificationApiService {
  constructor(private readonly http: HttpClient) {
  }

  get(tripPlanId: string): Observable<TripNotificationState> {
    return this.http.get<TripNotificationState>(
      `${environment.apiBaseUrl}${TRIP_API_ENDPOINTS.notifications(tripPlanId)}`,
      { transferCache: false }
    );
  }

  setEnabled(
    tripPlanId: string,
    request: SetTripNotificationsRequest
  ): Observable<TripNotificationState> {
    return this.http.put<TripNotificationState>(
      `${environment.apiBaseUrl}${TRIP_API_ENDPOINTS.notifications(tripPlanId)}`,
      request
    );
  }

  markRead(
    tripPlanId: string,
    request: MarkTripNotificationsReadRequest
  ): Observable<TripNotificationState> {
    return this.http.post<TripNotificationState>(
      `${environment.apiBaseUrl}${TRIP_API_ENDPOINTS.notificationsRead(tripPlanId)}`,
      request
    );
  }
}
