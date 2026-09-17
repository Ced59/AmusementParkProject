import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import {
  NotificationEmailPreference,
  NotificationEmailPreferenceUpdate
} from '@app/models/watchlists/notification-email-preference.model';
import { NotificationEmailPreferencesApiService } from '@data-access/watchlists/notification-email-preferences-api.service';

export interface NotificationEmailPreferencesDataPort {
  get(): Observable<NotificationEmailPreference>;
  update(input: NotificationEmailPreferenceUpdate): Observable<NotificationEmailPreference>;
}

export const NOTIFICATION_EMAIL_PREFERENCES_DATA_PORT =
  new InjectionToken<NotificationEmailPreferencesDataPort>(
    'NOTIFICATION_EMAIL_PREFERENCES_DATA_PORT',
    {
      providedIn: 'root',
      factory: (): NotificationEmailPreferencesDataPort =>
        inject(NotificationEmailPreferencesApiService)
    }
  );
