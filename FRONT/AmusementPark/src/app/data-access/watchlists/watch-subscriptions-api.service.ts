import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { UserCollectionTargetType } from '@app/models/watchlists/user-collection-entry.model';
import {
  WatchSubscription,
  WatchSubscriptionUpdateRequest,
  WatchSubscriptionWriteRequest
} from '@app/models/watchlists/watch-subscription.model';
import { environment } from '../../../environments/environment';
import { WATCH_SUBSCRIPTIONS_API_ENDPOINTS } from './watch-subscriptions-api-endpoints';

@Injectable({ providedIn: 'root' })
export class WatchSubscriptionsApiService {
  constructor(private readonly http: HttpClient) {
  }

  listMine(targetType: UserCollectionTargetType, targetId: string): Observable<WatchSubscription[]> {
    const params: HttpParams = new HttpParams()
      .set('targetType', targetType)
      .set('targetId', targetId);
    return this.http.get<WatchSubscription[]>(
      `${environment.apiBaseUrl}${WATCH_SUBSCRIPTIONS_API_ENDPOINTS.collection}`,
      { params }
    );
  }

  create(request: WatchSubscriptionWriteRequest): Observable<WatchSubscription> {
    return this.http.post<WatchSubscription>(
      `${environment.apiBaseUrl}${WATCH_SUBSCRIPTIONS_API_ENDPOINTS.collection}`,
      request
    );
  }

  update(subscriptionId: string, request: WatchSubscriptionUpdateRequest): Observable<WatchSubscription> {
    return this.http.patch<WatchSubscription>(
      `${environment.apiBaseUrl}${WATCH_SUBSCRIPTIONS_API_ENDPOINTS.entry(subscriptionId)}`,
      request
    );
  }

  setPaused(subscriptionId: string, expectedVersion: number, paused: boolean): Observable<WatchSubscription> {
    const endpoint: string = paused
      ? WATCH_SUBSCRIPTIONS_API_ENDPOINTS.pause(subscriptionId)
      : WATCH_SUBSCRIPTIONS_API_ENDPOINTS.resume(subscriptionId);
    return this.http.post<WatchSubscription>(
      `${environment.apiBaseUrl}${endpoint}`,
      { expectedVersion }
    );
  }

  delete(subscriptionId: string, expectedVersion: number): Observable<void> {
    const params: HttpParams = new HttpParams().set('expectedVersion', expectedVersion);
    return this.http.delete<void>(
      `${environment.apiBaseUrl}${WATCH_SUBSCRIPTIONS_API_ENDPOINTS.entry(subscriptionId)}`,
      { params }
    );
  }
}
