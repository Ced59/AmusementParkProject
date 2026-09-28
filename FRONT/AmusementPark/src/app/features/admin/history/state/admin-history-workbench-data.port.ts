import { InjectionToken, inject } from '@angular/core';

import { HistoryApiService } from '@data-access/history/history-api.service';

export interface AdminHistoryWorkbenchDataPort extends Pick<HistoryApiService,
  'getAdminParkWorkbench'
  | 'previewAdminHistoricalImpact'
  | 'saveAdminHistoricalSource'
  | 'saveAdminHistoricalFact'
  | 'saveAdminHistoricalRelation'
  | 'advanceAdminHistoricalResource'
  | 'retractAdminHistoricalResource'> {
}

export const ADMIN_HISTORY_WORKBENCH_DATA_PORT = new InjectionToken<AdminHistoryWorkbenchDataPort>(
  'ADMIN_HISTORY_WORKBENCH_DATA_PORT',
  {
    providedIn: 'root',
    factory: () => inject(HistoryApiService)
  }
);
