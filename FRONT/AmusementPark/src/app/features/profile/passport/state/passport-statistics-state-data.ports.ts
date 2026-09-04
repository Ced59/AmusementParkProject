import { inject, InjectionToken } from '@angular/core';

import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { PassportStatisticsApiService } from '@data-access/passport/passport-statistics-api.service';

export interface PassportStatisticsApiPort extends Pick<
  PassportStatisticsApiService,
  'getItemStatistics' | 'getParkStatistics' | 'getYearStatistics'
> {
}

export interface PassportStatisticsParksPort extends Pick<ParksApiService, 'getParkById'> {
}

export interface PassportStatisticsItemsPort extends Pick<ParkItemsApiService, 'getParkItemById'> {
}

export const PASSPORT_STATISTICS_API_PORT = new InjectionToken<PassportStatisticsApiPort>(
  'PASSPORT_STATISTICS_API_PORT',
  { providedIn: 'root', factory: () => inject(PassportStatisticsApiService) }
);

export const PASSPORT_STATISTICS_PARKS_PORT = new InjectionToken<PassportStatisticsParksPort>(
  'PASSPORT_STATISTICS_PARKS_PORT',
  { providedIn: 'root', factory: () => inject(ParksApiService) }
);

export const PASSPORT_STATISTICS_ITEMS_PORT = new InjectionToken<PassportStatisticsItemsPort>(
  'PASSPORT_STATISTICS_ITEMS_PORT',
  { providedIn: 'root', factory: () => inject(ParkItemsApiService) }
);
