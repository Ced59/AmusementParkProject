import { InjectionToken, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { AdminPassportBetaApiService } from '@data-access/passport/admin-passport-beta-api.service';
import {
  PassportBetaMetricsQuery,
  PassportBetaMetricsResult
} from '@app/models/passport/passport-beta-metrics.models';

export interface AdminPassportBetaDataPort {
  getMetrics(query: PassportBetaMetricsQuery): Observable<PassportBetaMetricsResult>;
}

export const ADMIN_PASSPORT_BETA_DATA_PORT = new InjectionToken<AdminPassportBetaDataPort>(
  'ADMIN_PASSPORT_BETA_DATA_PORT',
  {
    providedIn: 'root',
    factory: () => inject(AdminPassportBetaApiService)
  }
);
