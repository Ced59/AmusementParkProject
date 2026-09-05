import { inject, InjectionToken } from '@angular/core';

import { PassportStatisticsApiService } from '@data-access/passport/passport-statistics-api.service';

export interface PassportStatisticsApiPort extends Pick<
  PassportStatisticsApiService,
  'getGlobalStatistics' | 'getItemStatistics' | 'getParkStatistics' | 'getYearStatistics'
> {
}

export const PASSPORT_STATISTICS_API_PORT = new InjectionToken<PassportStatisticsApiPort>(
  'PASSPORT_STATISTICS_API_PORT',
  { providedIn: 'root', factory: () => inject(PassportStatisticsApiService) }
);
