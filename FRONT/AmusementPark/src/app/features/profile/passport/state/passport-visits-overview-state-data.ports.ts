import { inject, InjectionToken } from '@angular/core';
import { PassportVisitsApiService } from '@data-access/passport/passport-visits-api.service';

export interface PassportVisitsOverviewApiPort extends Pick<PassportVisitsApiService, 'listVisits'> {
}

export const PASSPORT_VISITS_OVERVIEW_API_PORT = new InjectionToken<PassportVisitsOverviewApiPort>(
  'PASSPORT_VISITS_OVERVIEW_API_PORT',
  {
    providedIn: 'root',
    factory: () => inject(PassportVisitsApiService)
  }
);
