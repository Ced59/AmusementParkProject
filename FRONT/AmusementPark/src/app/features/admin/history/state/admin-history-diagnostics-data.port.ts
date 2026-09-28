import { InjectionToken, inject } from '@angular/core';

import { HistoryApiService } from '@data-access/history/history-api.service';

export interface AdminHistoryDiagnosticsDataPort extends Pick<HistoryApiService, 'getAdminParkDiagnostics'> {
}

export const ADMIN_HISTORY_DIAGNOSTICS_DATA_PORT = new InjectionToken<AdminHistoryDiagnosticsDataPort>(
  'ADMIN_HISTORY_DIAGNOSTICS_DATA_PORT',
  {
    providedIn: 'root',
    factory: () => inject(HistoryApiService)
  }
);
