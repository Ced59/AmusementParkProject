import { InjectionToken, inject } from '@angular/core';

import { ParksApiService } from '@data-access/parks/parks-api.service';

export interface AdminHistoryDiagnosticsParksPort extends Pick<ParksApiService, 'searchParks'> {
}

export const ADMIN_HISTORY_DIAGNOSTICS_PARKS_PORT = new InjectionToken<AdminHistoryDiagnosticsParksPort>(
  'ADMIN_HISTORY_DIAGNOSTICS_PARKS_PORT',
  {
    providedIn: 'root',
    factory: () => inject(ParksApiService)
  }
);
