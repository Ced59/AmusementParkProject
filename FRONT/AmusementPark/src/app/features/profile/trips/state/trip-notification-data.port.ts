import { inject, InjectionToken } from '@angular/core';

import { TripNotificationApiService } from '@data-access/trips/trip-notification-api.service';

export interface TripNotificationDataPort extends Pick<
  TripNotificationApiService,
  'get' | 'setEnabled' | 'markRead'
> {
}

export const TRIP_NOTIFICATION_DATA_PORT = new InjectionToken<TripNotificationDataPort>(
  'TRIP_NOTIFICATION_DATA_PORT',
  { providedIn: 'root', factory: (): TripNotificationDataPort => inject(TripNotificationApiService) }
);
