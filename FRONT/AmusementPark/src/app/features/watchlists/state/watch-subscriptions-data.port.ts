import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import { UserCollectionTargetType } from '@app/models/watchlists/user-collection-entry.model';
import {
  WatchSubscription,
  WatchSubscriptionUpdateRequest,
  WatchSubscriptionWriteRequest
} from '@app/models/watchlists/watch-subscription.model';
import { WatchSubscriptionsApiService } from '@data-access/watchlists/watch-subscriptions-api.service';

export interface WatchSubscriptionsDataPort {
  listMine(targetType: UserCollectionTargetType, targetId: string): Observable<WatchSubscription[]>;
  create(request: WatchSubscriptionWriteRequest): Observable<WatchSubscription>;
  update(subscriptionId: string, request: WatchSubscriptionUpdateRequest): Observable<WatchSubscription>;
  setPaused(subscriptionId: string, expectedVersion: number, paused: boolean): Observable<WatchSubscription>;
  delete(subscriptionId: string, expectedVersion: number): Observable<void>;
}

export const WATCH_SUBSCRIPTIONS_DATA_PORT = new InjectionToken<WatchSubscriptionsDataPort>(
  'WATCH_SUBSCRIPTIONS_DATA_PORT',
  {
    providedIn: 'root',
    factory: (): WatchSubscriptionsDataPort => inject(WatchSubscriptionsApiService)
  }
);
