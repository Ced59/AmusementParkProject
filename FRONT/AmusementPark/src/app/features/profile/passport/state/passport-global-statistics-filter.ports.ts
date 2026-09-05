import { inject, InjectionToken } from '@angular/core';

import { PassportGlobalStatisticsFilterStoreService } from './passport-global-statistics-filter-store.service';

export interface PassportGlobalStatisticsFilter {
  year: number | null;
  parkId: string | null;
}

export interface PassportGlobalStatisticsFilterStorePort {
  read(): PassportGlobalStatisticsFilter;
  write(filter: PassportGlobalStatisticsFilter): void;
}

export const PASSPORT_GLOBAL_STATISTICS_FILTER_STORE =
  new InjectionToken<PassportGlobalStatisticsFilterStorePort>(
    'PASSPORT_GLOBAL_STATISTICS_FILTER_STORE',
    { providedIn: 'root', factory: () => inject(PassportGlobalStatisticsFilterStoreService) }
  );
