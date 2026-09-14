import { inject, InjectionToken } from '@angular/core';

import { AdminParkFitDataQualityApiService } from '@app/data-access/admin/admin-park-fit-data-quality-api.service';

export interface AdminParkFitDataQualityStatePort extends Pick<
  AdminParkFitDataQualityApiService,
  'getPage' | 'getPendingReports' | 'reviewReport' | 'changeOperationalStatus'> {
}

export const ADMIN_PARK_FIT_DATA_QUALITY_STATE_PORT =
  new InjectionToken<AdminParkFitDataQualityStatePort>('ADMIN_PARK_FIT_DATA_QUALITY_STATE_PORT', {
    providedIn: 'root',
    factory: () => inject(AdminParkFitDataQualityApiService)
  });
