import { inject, InjectionToken } from '@angular/core';

import { TripExportApiService } from '@data-access/trips/trip-export-api.service';

export interface TripExportDataPort extends Pick<TripExportApiService, 'get'> {
}

export const TRIP_EXPORT_DATA_PORT = new InjectionToken<TripExportDataPort>(
  'TRIP_EXPORT_DATA_PORT',
  { providedIn: 'root', factory: (): TripExportDataPort => inject(TripExportApiService) }
);
