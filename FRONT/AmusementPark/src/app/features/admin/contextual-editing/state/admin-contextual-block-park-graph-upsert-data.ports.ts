import { inject, InjectionToken } from '@angular/core';

import { ParkGraphUpsertsApiService } from '@data-access/admin/park-graph-upserts-api.service';

export interface AdminContextualBlockParkGraphUpsertDataPort extends Pick<ParkGraphUpsertsApiService, 'apply'> {
}

export const ADMIN_CONTEXTUAL_BLOCK_PARK_GRAPH_UPSERT_DATA_PORT = new InjectionToken<AdminContextualBlockParkGraphUpsertDataPort>(
  'ADMIN_CONTEXTUAL_BLOCK_PARK_GRAPH_UPSERT_DATA_PORT',
  {
    providedIn: 'root',
    factory: () => inject(ParkGraphUpsertsApiService)
  }
);
