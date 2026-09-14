import { inject, InjectionToken } from '@angular/core';

import { ParkFitApiService } from '@data-access/park-fit/park-fit-api.service';

export interface ParkFitSourceReportDataPort extends Pick<ParkFitApiService, 'submitReport'> {
}

export const PARK_FIT_SOURCE_REPORT_DATA_PORT = new InjectionToken<ParkFitSourceReportDataPort>(
  'PARK_FIT_SOURCE_REPORT_DATA_PORT',
  {
    providedIn: 'root',
    factory: () => inject(ParkFitApiService)
  }
);
