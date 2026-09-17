import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  NotificationEmailPreference,
  NotificationEmailPreferenceUpdate
} from '@app/models/watchlists/notification-email-preference.model';
import { environment } from '../../../environments/environment';
import { NOTIFICATION_EMAIL_PREFERENCES_API_ENDPOINTS } from './notification-email-preferences-api-endpoints';

@Injectable({ providedIn: 'root' })
export class NotificationEmailPreferencesApiService {
  constructor(private readonly http: HttpClient) {
  }

  get(): Observable<NotificationEmailPreference> {
    return this.http.get<NotificationEmailPreference>(
      `${environment.apiBaseUrl}${NOTIFICATION_EMAIL_PREFERENCES_API_ENDPOINTS.preference}`
    );
  }

  update(input: NotificationEmailPreferenceUpdate): Observable<NotificationEmailPreference> {
    return this.http.put<NotificationEmailPreference>(
      `${environment.apiBaseUrl}${NOTIFICATION_EMAIL_PREFERENCES_API_ENDPOINTS.preference}`,
      input
    );
  }
}
