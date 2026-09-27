import { inject, InjectionToken } from '@angular/core';

import { PassportHistoricalExistenceReportsApiService } from '@data-access/passport/passport-historical-existence-reports-api.service';

export interface PassportHistoricalExistenceReportDataPort extends Pick<
  PassportHistoricalExistenceReportsApiService,
  'list' | 'submit'
> {
}

export const PASSPORT_HISTORICAL_EXISTENCE_REPORT_DATA_PORT =
  new InjectionToken<PassportHistoricalExistenceReportDataPort>(
    'PASSPORT_HISTORICAL_EXISTENCE_REPORT_DATA_PORT',
    {
      providedIn: 'root',
      factory: (): PassportHistoricalExistenceReportDataPort =>
        inject(PassportHistoricalExistenceReportsApiService)
    }
  );
