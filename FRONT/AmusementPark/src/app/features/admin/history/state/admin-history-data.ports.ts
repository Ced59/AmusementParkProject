import { InjectionToken, inject } from '@angular/core';

import { HistoryApiService } from '@data-access/history/history-api.service';

export interface AdminHistoryDataPort extends Pick<HistoryApiService, 'createAdminEvent' | 'deleteAdminEvent' | 'getAdminEvents' | 'updateAdminEvent'> {
}

export const ADMIN_HISTORY_DATA_PORT = new InjectionToken<AdminHistoryDataPort>('ADMIN_HISTORY_DATA_PORT', {
  providedIn: 'root',
  factory: () => inject(HistoryApiService)
});
