import { inject, InjectionToken } from '@angular/core';

import { ParkGraphUpsertsApiService } from '@data-access/admin/park-graph-upserts-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';

export interface AdminParkGraphUpsertParksPort extends Pick<ParksApiService,
  'searchParks' | 'getParkDataCompletenessScore'> {
}

export interface AdminParkGraphUpsertGraphPort extends Pick<ParkGraphUpsertsApiService,
  'downloadParkExport' | 'preview' | 'apply'> {
}

export const ADMIN_PARK_GRAPH_UPSERT_PARKS_PORT =
  new InjectionToken<AdminParkGraphUpsertParksPort>('AdminParkGraphUpsertParksPort', {
    providedIn: 'root',
    factory: () => inject(ParksApiService)
  });

export const ADMIN_PARK_GRAPH_UPSERT_GRAPH_PORT =
  new InjectionToken<AdminParkGraphUpsertGraphPort>('AdminParkGraphUpsertGraphPort', {
    providedIn: 'root',
    factory: () => inject(ParkGraphUpsertsApiService)
  });
