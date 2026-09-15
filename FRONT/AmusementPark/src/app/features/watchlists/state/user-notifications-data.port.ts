import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import {
  UserNotificationPage,
  UserNotificationSearch
} from '@app/models/watchlists/user-notification.model';
import { UserNotificationsApiService } from '@data-access/watchlists/user-notifications-api.service';

export interface UserNotificationsDataPort {
  search(criteria: UserNotificationSearch): Observable<UserNotificationPage>;
  markRead(notificationId: string, expectedVersion: number): Observable<void>;
  dismiss(notificationId: string, expectedVersion: number): Observable<void>;
  markAllRead(): Observable<void>;
  deleteSourceSubscription(notificationId: string): Observable<void>;
}

export const USER_NOTIFICATIONS_DATA_PORT = new InjectionToken<UserNotificationsDataPort>(
  'USER_NOTIFICATIONS_DATA_PORT',
  {
    providedIn: 'root',
    factory: (): UserNotificationsDataPort => inject(UserNotificationsApiService)
  }
);
