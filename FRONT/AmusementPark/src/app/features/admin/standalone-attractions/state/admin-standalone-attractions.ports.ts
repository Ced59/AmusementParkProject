import { inject, InjectionToken } from '@angular/core';

import { ParkAdminListFilters } from '@data-access/parks/parks-api-endpoints';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { StandaloneAttractionListFilters, StandaloneAttractionsApiService } from '@data-access/standalone-attractions/standalone-attractions-api.service';

export type AdminStandaloneAttractionListFilters = StandaloneAttractionListFilters;
export type AdminStandaloneLegacyParkFilters = ParkAdminListFilters;

export interface AdminStandaloneAttractionsDataPort extends Pick<StandaloneAttractionsApiService,
  'getAdminPage'
  | 'getAdminById'
  | 'create'
  | 'update'
  | 'updateBulkAdministration'
  | 'migrateFromPark'
  | 'downloadExport'> {
}

export interface AdminStandaloneAttractionsParksPort extends Pick<ParksApiService,
  'getParkById' | 'searchParks'> {
}

export const ADMIN_STANDALONE_ATTRACTIONS_DATA_PORT =
  new InjectionToken<AdminStandaloneAttractionsDataPort>('AdminStandaloneAttractionsDataPort', {
    providedIn: 'root',
    factory: () => inject(StandaloneAttractionsApiService)
  });

export const ADMIN_STANDALONE_ATTRACTIONS_PARKS_PORT =
  new InjectionToken<AdminStandaloneAttractionsParksPort>('AdminStandaloneAttractionsParksPort', {
    providedIn: 'root',
    factory: () => inject(ParksApiService)
  });
