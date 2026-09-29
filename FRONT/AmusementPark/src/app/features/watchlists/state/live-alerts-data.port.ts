import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import {
  CreateLiveAlertRequest,
  LiveAlertDashboard,
  LiveAlertSubscription
} from '@app/models/watchlists/live-alert.model';
import { LiveAlertsApiService } from '@data-access/watchlists/live-alerts-api.service';

export interface LiveAlertsDataPort {
  getDashboard(targetId?: string): Observable<LiveAlertDashboard>;
  create(request: CreateLiveAlertRequest): Observable<LiveAlertSubscription>;
  delete(subscriptionId: string, expectedVersion: number): Observable<void>;
  markRead(notificationId: string, expectedVersion: number): Observable<void>;
  dismiss(notificationId: string, expectedVersion: number): Observable<void>;
}

export const LIVE_ALERTS_DATA_PORT = new InjectionToken<LiveAlertsDataPort>('LIVE_ALERTS_DATA_PORT', {
  providedIn: 'root',
  factory: (): LiveAlertsDataPort => inject(LiveAlertsApiService)
});
